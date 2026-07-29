using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class ArjEngineTests
{
    private readonly ArjEngine _sut = new();

    private static readonly byte[] ArjSampleBytes =
    [
        0x60, 0xEA, 0x2B, 0x00, 0x22, 0x0B, 0x01, 0x0B, 0x10, 0x00, 0x02, 0x03, 0x03, 0x5D, 0xFB, 0x50,
        0x0C, 0x5D, 0xFB, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x61, 0x72, 0x6A, 0x2E, 0x61, 0x72, 0x6A, 0x00, 0x00, 0xF3,
        0xD2, 0xF8, 0x3B, 0x00, 0x00, 0x60, 0xEA, 0x37, 0x00, 0x2E, 0x0B, 0x01, 0x0B, 0x10, 0x00, 0x00,
        0x0C, 0x0C, 0x5D, 0xFB, 0x50, 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xBD, 0xA9, 0x9D,
        0x90, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x5D, 0xFB, 0x50, 0xFB,
        0x5C, 0xFB, 0x50, 0x00, 0x00, 0x00, 0x00, 0x61, 0x72, 0x6A, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00,
        0x01, 0xAE, 0x16, 0xD5, 0x00, 0x00, 0x41, 0x52, 0x4A, 0x60, 0xEA, 0x00, 0x00,
    ];

    [Fact]
    public void Name_ShouldReturnArj()
    {
        _sut.Name.Should().Be("ARJ");
    }

    [Fact]
    public void Extension_ShouldReturnDotArj()
    {
        _sut.Extension.Should().Be(".arj");
    }

    [Fact]
    public void CanRead_ShouldBeTrue()
    {
        _sut.CanRead.Should().BeTrue();
    }

    [Fact]
    public void CanWrite_ShouldBeFalse()
    {
        _sut.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void CanEncrypt_ShouldBeTrue()
    {
        _sut.CanEncrypt.Should().BeTrue();
    }

    [Fact]
    public void SupportsSolid_ShouldBeFalse()
    {
        _sut.SupportsSolid.Should().BeFalse();
    }

    [Fact]
    public void SupportsVolumes_ShouldBeTrue()
    {
        _sut.SupportsVolumes.Should().BeTrue();
    }

    [Fact]
    public void ArchiveFactory_GetFormat_WithArj_ShouldReturnArjEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.Arj);
        format.Should().BeOfType<ArjEngine>();
    }

    [Fact]
    public void Open_WithSample_ShouldReadEntry()
    {
        using var stream = new MemoryStream(ArjSampleBytes);
        var archive = _sut.Open("test.arj", stream, AccessMode.Read);

        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].Path.Should().Be("arj.txt");
        archive.Entries[0].Name.Should().Be("arj.txt");
        archive.Entries[0].Size.Should().Be(3);
        archive.Entries[0].IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void Open_WithSample_ShouldReturnContent()
    {
        using var stream = new MemoryStream(ArjSampleBytes);
        var archive = _sut.Open("test.arj", stream, AccessMode.Read);

        var node = archive.Vfs.FindNode("/arj.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var reader = new StreamReader(content);
        reader.ReadToEnd().Should().Be("ARJ");
    }

    [Fact]
    public void Open_OnInvalidStream_ShouldThrow()
    {
        using var stream = new MemoryStream("not an arj file"u8.ToArray());
        var act = () => _sut.Open("bad.arj", stream, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Save_ShouldThrowNotSupported()
    {
        var archive = new Archive
        {
            Format = CompressionFormat.Arj,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new(),
        };
        using var stream = new MemoryStream();
        var act = () => _sut.Save(archive, stream, new CompressionOptions { Format = CompressionFormat.Arj });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ArchiveFactory_DetectArjFromHeader_ShouldReturnArjEngine()
    {
        using var stream = new MemoryStream(ArjSampleBytes);
        var format = ArchiveFactory.GetFormatFromFileHeader(stream);
        format.Should().BeOfType<ArjEngine>();
    }

    [Fact]
    public void ArchiveFactory_GetFormatFromExtension_ShouldReturnArjEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension(".arj");
        format.Should().BeOfType<ArjEngine>();
    }
}
