using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Filesystem;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class TarEngineTests
{
    private readonly TarEngine _sut = new();
    private static readonly byte[] Lorem = "Lorem ipsum dolor sit amet"u8.ToArray();

    [Fact]
    public void Name_ShouldReturnTar()
    {
        _sut.Name.Should().Be("Tar");
    }

    [Fact]
    public void Extension_ShouldReturnDotTar()
    {
        _sut.Extension.Should().Be(".tar");
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
    public void SupportsSolid_ShouldBeFalse()
    {
        _sut.SupportsSolid.Should().BeFalse();
    }

    [Theory]
    [InlineData(CompressionFormat.Tar, ".tar")]
    [InlineData(CompressionFormat.TarGz, ".tar.gz")]
    [InlineData(CompressionFormat.TarBz2, ".tar.bz2")]
    [InlineData(CompressionFormat.TarXz, ".tar.xz")]
    [InlineData(CompressionFormat.TarZstd, ".tar.zst")]
    public void ArchiveFactory_GetFormat_ShouldReturnTarEngine(CompressionFormat format, string extension)
    {
        var fromEnum = ArchiveFactory.GetFormat(format);
        fromEnum.Should().BeOfType<TarEngine>();

        var fromExt = ArchiveFactory.GetFormatFromExtension(extension);
        fromExt.Should().BeOfType<TarEngine>();
    }

    [Fact]
    public void SaveAndOpen_Tar_WithSingleFile_ShouldRoundTrip()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["hello.txt"] = "Hello World"u8.ToArray(),
        });

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Tar });

        output.Position = 0;
        var opened = _sut.Open("test.tar", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("hello.txt");
        opened.Entries[0].Size.Should().Be(11);
    }

    [Fact]
    public void SaveAndOpen_TarGz_WithSingleFile_ShouldRoundTrip()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["hello.txt"] = "Hello World"u8.ToArray(),
        });

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.TarGz });

        output.Position = 0;
        var opened = _sut.Open("test.tar.gz", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("hello.txt");
        opened.Entries[0].Size.Should().Be(11);
    }

    [Fact]
    public void SaveAndOpen_TarBz2_WithSingleFile_ShouldRoundTrip()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["hello.txt"] = "Hello World"u8.ToArray(),
        });

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.TarBz2 });

        output.Position = 0;
        var opened = _sut.Open("test.tar.bz2", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("hello.txt");
        opened.Entries[0].Size.Should().Be(11);
    }

    [Fact]
    public void Save_TarXz_ShouldThrowNotSupported()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["hello.txt"] = "Hello World"u8.ToArray(),
        });

        var act = () => _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.TarXz });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void SaveAndOpen_TarZstd_WithSingleFile_ShouldRoundTrip()
    {
        using var output = new MemoryStream();
        var archive = CreateArchive(new Dictionary<string, byte[]>
        {
            ["hello.txt"] = "Hello World"u8.ToArray(),
        });

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.TarZstd });

        output.Position = 0;
        var opened = _sut.Open("test.tar.zst", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(1);
        opened.Entries[0].Path.Should().Be("hello.txt");
        opened.Entries[0].Size.Should().Be(11);
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

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Tar });

        output.Position = 0;
        var opened = _sut.Open("test.tar", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(3);
        opened.Entries.Select(e => e.Path).Should().BeEquivalentTo("a.txt", "b.txt", "c.txt");
    }

    [Fact]
    public void SaveAndOpen_WithDirectories_ShouldPreserveStructure()
    {
        using var output = new MemoryStream();
        var archive = CreateArchiveWithDirs();

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Tar });

        output.Position = 0;
        var opened = _sut.Open("test.tar", output, AccessMode.Read);

        opened.Entries.Should().HaveCount(4);
        opened.Entries.Any(e => e.Path == "sub/nested.txt").Should().BeTrue();
        opened.Entries.Any(e => e.Path == "sub/dir/file.txt").Should().BeTrue();
        opened.Entries.Any(e => e.IsDirectory).Should().BeTrue();
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

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Tar });

        output.Position = 0;
        var opened = _sut.Open("test.tar", output, AccessMode.Read);

        var node = opened.Vfs.FindNode("/data.bin");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        ms.ToArray().Should().BeEquivalentTo(binary);
    }

    [Fact]
    public void SaveAndOpen_WithEmptyArchive_ShouldReturnNoEntries()
    {
        using var output = new MemoryStream();
        var archive = new Archive
        {
            Format = CompressionFormat.Tar,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new VirtualFileSystem(),
        };

        _sut.Save(archive, output, new CompressionOptions { Format = CompressionFormat.Tar });

        output.Position = 0;
        var opened = _sut.Open("empty.tar", output, AccessMode.Read);

        opened.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Open_OnInvalidStream_ShouldThrow()
    {
        using var stream = new MemoryStream("not a tar file"u8.ToArray());
        var act = () => _sut.Open("bad.tar", stream, AccessMode.Read);
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
            Format = CompressionFormat.Tar,
            FormatEngine = new TarEngine(),
            Entries = entries,
            Vfs = vfs,
        };
    }

    private static Archive CreateArchiveWithDirs()
    {
        var vfs = new VirtualFileSystem();
        vfs.AddDirectory("sub");
        vfs.AddFile("sub/nested.txt", new MemoryStream("nested"u8.ToArray()));
        vfs.AddDirectory("sub/dir");
        vfs.AddFile("sub/dir/file.txt", new MemoryStream("deep"u8.ToArray()));

        return new Archive
        {
            Format = CompressionFormat.Tar,
            FormatEngine = new TarEngine(),
            Entries = new List<ArchiveEntry>
            {
                new() { Path = "sub/nested.txt", Name = "nested.txt", Size = 6 },
                new() { Path = "sub/dir/file.txt", Name = "file.txt", Size = 4 },
                new() { Path = "sub", Name = "sub", IsDirectory = true },
                new() { Path = "sub/dir", Name = "dir", IsDirectory = true },
            },
            Vfs = vfs,
        };
    }
}
