using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class ZstdEngineTests
{
    private readonly ZstdEngine _sut = new();

    [Fact]
    public void Name_ShouldReturnZstandard()
    {
        _sut.Name.Should().Be("Zstandard");
    }

    [Fact]
    public void Extension_ShouldReturnDotZst()
    {
        _sut.Extension.Should().Be(".zst");
    }

    [Fact]
    public void CanRead_ShouldBeTrue()
    {
        _sut.CanRead.Should().BeTrue();
    }

    [Fact]
    public void CanWrite_ShouldBeTrue()
    {
        _sut.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void ArchiveFactory_GetFormat_WithZstandard_ShouldReturnZstdEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.Zstandard);
        format.Should().BeOfType<ZstdEngine>();
    }

    [Fact]
    public void SaveAndOpen_WithSingleFile_ShouldRoundTrip()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive("hello.txt", "Hello World"u8.ToArray());

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Zstandard });

        output.Position = 0;
        var opened = _sut.Open("test.zst", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("test");
    }

    [Fact]
    public void SaveAndOpen_WithBinaryContent_ShouldPreserveBytes()
    {
        var binary = new byte[512];
        for (var i = 0; i < binary.Length; i++) binary[i] = (byte)(i % 256);

        using var output = new MemoryStream();
        var archive = CreateArchive("data.bin", binary);

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Zstandard });

        output.Position = 0;
        var opened = _sut.Open("test.zst", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        var entry = opened.Entries[0];
        entry.Size.Should().Be(binary.Length);

        var node = opened.Vfs.FindNode($"/{entry.Path}");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        ms.ToArray().Should().BeEquivalentTo(binary);
    }

    [Fact]
    public void SaveAndOpen_WithEmptyFile_ShouldRoundTrip()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive("empty.bin", []);

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Zstandard });

        output.Position = 0;
        var opened = _sut.Open("empty.zst", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Size.Should().Be(0);
    }

    [Fact]
    public void Open_OnInvalidStream_ShouldThrow()
    {
        using var stream = new MemoryStream("not zstd data"u8.ToArray());
        var act = () => _sut.Open("bad.zst", stream, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    private static Archive CreateArchive(string fileName, byte[] content)
    {
        var vfs = new VirtualFileSystem();
        vfs.AddFile(fileName, new MemoryStream(content));

        return new Archive
        {
            Format = CompressionFormat.Zstandard,
            FormatEngine = new ZstdEngine(),
            Entries = new List<ArchiveEntry>
            {
                new()
                {
                    Path = fileName,
                    Name = fileName,
                    Size = content.Length,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow,
                }
            },
            Vfs = vfs,
        };
    }
}
