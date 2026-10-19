using Arcana.Core.Compression;
using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class FormatCompatibilityTests
{
    [Theory]
    [InlineData("backup.tar.gz", CompressionFormat.TarGz)]
    [InlineData("backup.tar.xz", CompressionFormat.TarXz)]
    [InlineData("backup.7z", CompressionFormat.SevenZip)]
    [InlineData("backup.tgz", CompressionFormat.TarGz)]
    public void OutputFormat_IsResolvedIncludingCompoundExtensions(string path, CompressionFormat expected)
        => ArchiveFormats.FromPath(path).Should().Be(expected);

    [Theory]
    [InlineData(CompressionFormat.GZip)]
    [InlineData(CompressionFormat.BZip2)]
    [InlineData(CompressionFormat.Brotli)]
    [InlineData(CompressionFormat.Xz)]
    [InlineData(CompressionFormat.Lzma)]
    public async Task SingleStreamEngines_ReturnReadableContent(CompressionFormat format)
    {
        var engine = ArchiveFactory.GetFormat(format);
        using var archive = new Archive { Vfs = new VirtualFileSystem(), FormatEngine = engine };
        archive.Vfs.AddFile("hello.txt", new MemoryStream("hello world"u8.ToArray()));
        using var output = new MemoryStream();
        await engine.SaveAsync(archive, output, new CompressionOptions { Format = format });
        if (format == CompressionFormat.Xz)
            output.ToArray().Take(6).Should().Equal(0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00);
        output.Position = 0;
        using var opened = await engine.OpenAsync("hello.txt" + engine.Extension, output, AccessMode.Read);
        using var content = opened.Vfs.Root.Children.Single().OpenRead();
        new StreamReader(content).ReadToEnd().Should().Be("hello world");
    }

    [Fact]
    public async Task MultiFileInputToSingleStream_IsRejectedBeforeReplacingDestination()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".zst");
        try
        {
            File.WriteAllText(path, "original");
            using var archive = new Archive();
            archive.Vfs.AddDirectory("folder");
            Func<Task> act = () => ArchiveWriter.SaveAsync(archive, path,
                new CompressionOptions { Format = CompressionFormat.Zstandard });
            await act.Should().ThrowAsync<InvalidOperationException>();
            File.ReadAllText(path).Should().Be("original");
        }
        finally { File.Delete(path); }
    }
}
