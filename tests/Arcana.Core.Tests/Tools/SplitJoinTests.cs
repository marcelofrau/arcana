using Arcana.Core.Tools;
using FluentAssertions;

namespace Arcana.Core.Tests.Tools;

public class SplitJoinTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "arcana-parts", Guid.NewGuid().ToString("N"));
    public SplitJoinTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData(false, 11)]
    [InlineData(true, 11)]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    public async Task BothNamingModes_RoundTripWithAutomaticDiscovery(bool hjsplit, int size)
    {
        var source = Path.Combine(_directory, "source.bin");
        var bytes = Enumerable.Range(0, size).Select(i => (byte)i).ToArray();
        File.WriteAllBytes(source, bytes);
        var partsDir = Path.Combine(_directory, "parts");
        await new FileSplitter().SplitAsync(source, partsDir, 4, hjsplitMode: hjsplit);
        var parts = FileJoiner.AutoDiscoverParts(partsDir);
        var output = Path.Combine(_directory, "joined.bin");
        await new FileJoiner().JoinAsync(parts, output);
        File.ReadAllBytes(output).Should().Equal(bytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidPartSize_IsRejectedBeforeWriting(long size)
    {
        Func<Task> act = () => new FileSplitter().SplitAsync("unused", _directory, size);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        Directory.GetFiles(_directory).Should().BeEmpty();
    }

    [Fact]
    public void MissingMiddlePart_IsRejected()
    {
        File.WriteAllText(Path.Combine(_directory, "a.001"), "one");
        File.WriteAllText(Path.Combine(_directory, "a.003"), "three");
        Action act = () => FileJoiner.AutoDiscoverParts(Path.Combine(_directory, "a.001"));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DirectoryContainingDifferentSequences_IsRejected()
    {
        File.WriteAllText(Path.Combine(_directory, "a.001"), "a");
        File.WriteAllText(Path.Combine(_directory, "b.001"), "b");
        Action act = () => FileJoiner.AutoDiscoverParts(_directory);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task JoiningIntoAnInputPart_IsRejectedWithoutDataLoss()
    {
        var part = Path.Combine(_directory, "a.001");
        File.WriteAllText(part, "original");
        Func<Task> act = () => new FileJoiner().JoinAsync([part], part);
        await act.Should().ThrowAsync<ArgumentException>();
        File.ReadAllText(part).Should().Be("original");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
