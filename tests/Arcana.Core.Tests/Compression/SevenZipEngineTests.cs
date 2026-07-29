using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class SevenZipEngineTests
{
    private readonly SevenZipEngine _sut = new();

    [Fact]
    public void Name_ShouldReturn7z()
    {
        _sut.Name.Should().Be("7z");
    }

    [Fact]
    public void Extension_ShouldReturnDot7z()
    {
        _sut.Extension.Should().Be(".7z");
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
    public void SupportsSolid_ShouldBeTrue()
    {
        _sut.SupportsSolid.Should().BeTrue();
    }

    [Fact]
    public void ArchiveFactory_GetFormat_WithSevenZip_ShouldReturnSevenZipEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.SevenZip);
        format.Should().BeOfType<SevenZipEngine>();
    }

    [Fact]
    public void SaveAndOpen_WithSingleFile_ShouldRoundTrip()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["hello.txt"] = "Hello World"u8.ToArray(),
        });

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.SevenZip });

        output.Position = 0;
        var opened = _sut.Open("test.7z", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("hello.txt");
    }

    [Fact]
    public void SaveAndOpen_WithMultipleFiles_ShouldPreserveAllEntries()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["a.txt"] = "alpha"u8.ToArray(),
            ["b.txt"] = "beta"u8.ToArray(),
            ["c.txt"] = "gamma"u8.ToArray(),
        });

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.SevenZip });

        output.Position = 0;
        var opened = _sut.Open("test.7z", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(3);
        opened.Entries.Select(e => e.Path).Should().BeEquivalentTo("a.txt", "b.txt", "c.txt");
    }

    [Fact]
    public void SaveAndOpen_WithBinaryContent_ShouldPreserveBytes()
    {
        var binary = new byte[512];
        for (var i = 0; i < binary.Length; i++) binary[i] = (byte)(i % 256);

        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["data.bin"] = binary,
        });

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.SevenZip });

        output.Position = 0;
        var opened = _sut.Open("test.7z", output, AccessMode.Read);

        var node = opened.Vfs.FindNode("/data.bin");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        ms.ToArray().Should().BeEquivalentTo(binary);
    }

    [Fact]
    public void SaveAndOpen_WithEmptyArchive_ShouldThrow()
    {
        using var output = new MemoryStream();
        var archive = new Archive
        {
            Format = CompressionFormat.SevenZip,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new VirtualFileSystem(),
        };

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.SevenZip });

        output.Position = 0;
        var act = () => _sut.Open("empty.7z", output, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Open_OnInvalidStream_ShouldThrow()
    {
        using var stream = new MemoryStream("not a 7z file"u8.ToArray());
        var act = () => _sut.Open("bad.7z", stream, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    private static Archive CreateArchive(Dictionary<string, byte[]> files)
    {
        var vfs = new VirtualFileSystem();
        var entries = new List<ArchiveEntry>();

        foreach (var (path, content) in files)
        {
            vfs.AddFile(path, new MemoryStream(content));
            entries.Add(new ArchiveEntry
            {
                Path = path,
                Name = System.IO.Path.GetFileName(path),
                Size = content.Length,
                IsDirectory = false,
                LastModified = DateTime.UtcNow,
            });
        }

        return new Archive
        {
            Format = CompressionFormat.SevenZip,
            FormatEngine = new SevenZipEngine(),
            Entries = entries,
            Vfs = vfs,
        };
    }
}
