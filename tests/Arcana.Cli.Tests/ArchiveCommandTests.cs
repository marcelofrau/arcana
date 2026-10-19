using Arcana.Cli.Commands;
using Arcana.Core.Compression;
using FluentAssertions;

namespace Arcana.Cli.Tests;

public class ArchiveCommandTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "arcana-cli", Guid.NewGuid().ToString("N"));
    public ArchiveCommandTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("zip", CompressionFormat.Zip)]
    [InlineData("7z", CompressionFormat.SevenZip)]
    [InlineData("tar.gz", CompressionFormat.TarGz)]
    public async Task CompressProducesRequestedFormat(string format, CompressionFormat expected)
    {
        var source = Path.Combine(_directory, "input.txt");
        var output = Path.Combine(_directory, "output." + format);
        File.WriteAllText(source, "command integration");
        (await CompressCommand.Create().Parse([source, "-f", format, "-o", output]).InvokeAsync()).Should().Be(0);
        using var archive = await ArchiveFactory.OpenAsync(output);
        archive.Format.Should().Be(expected);
        using var content = archive.Vfs.FindNode("input.txt")!.OpenRead();
        new StreamReader(content).ReadToEnd().Should().Be("command integration");
    }

    [Fact]
    public async Task ExtractPassesPasswordAndRespectsOverwriteFlag()
    {
        var source = Path.Combine(_directory, "input.txt");
        var zip = Path.Combine(_directory, "protected.zip");
        var destination = Path.Combine(_directory, "out");
        File.WriteAllText(source, "archive content");
        (await CompressCommand.Create().Parse([source, "-o", zip, "-p", "test-only"]).InvokeAsync()).Should().Be(0);
        Directory.CreateDirectory(destination);
        var extracted = Path.Combine(destination, "input.txt");
        File.WriteAllText(extracted, "keep me");
        (await ExtractCommand.Create().Parse([zip, destination, "-p", "test-only"]).InvokeAsync()).Should().Be(0);
        File.ReadAllText(extracted).Should().Be("keep me");
        (await ExtractCommand.Create().Parse([zip, destination, "-p", "test-only", "--overwrite"]).InvokeAsync()).Should().Be(0);
        File.ReadAllText(extracted).Should().Be("archive content");
    }

    [Fact]
    public async Task ConvertUsesCompoundDestinationFormat()
    {
        var source = Path.Combine(_directory, "input.txt");
        var zip = Path.Combine(_directory, "input.zip");
        var output = Path.Combine(_directory, "output.tar.xz");
        File.WriteAllText(source, "convert me");
        (await CompressCommand.Create().Parse([source, "-o", zip]).InvokeAsync()).Should().Be(0);
        (await ConvertCommand.Create().Parse([zip, "-o", output]).InvokeAsync()).Should().Be(0);
        using var archive = await ArchiveFactory.OpenAsync(output);
        archive.Format.Should().Be(CompressionFormat.TarXz);
        using var content = archive.Vfs.FindNode("input.txt")!.OpenRead();
        new StreamReader(content).ReadToEnd().Should().Be("convert me");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
