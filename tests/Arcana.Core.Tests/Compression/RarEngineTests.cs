using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class RarEngineTests
{
    private readonly RarEngine _sut = new();

    // corkami rar5.rar - minimal RAR 5.0 archive, contains rar5.txt (4 bytes, content "RAR5")
    private static readonly byte[] Rar5SampleBytes =
    [
        0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00, 0x33, 0x92, 0xB5, 0xE5, 0x0A, 0x01, 0x05, 0x06,
        0x00, 0x05, 0x01, 0x01, 0x80, 0x80, 0x00, 0xDE, 0x7F, 0x87, 0xFB, 0x24, 0x02, 0x03, 0x0B, 0x84,
        0x00, 0x04, 0x84, 0x00, 0x20, 0x37, 0x04, 0x26, 0xEF, 0x80, 0x00, 0x00, 0x08, 0x72, 0x61, 0x72,
        0x35, 0x2E, 0x74, 0x78, 0x74, 0x0A, 0x03, 0x02, 0xD9, 0x9C, 0x57, 0x3C, 0x2A, 0xCE, 0xD5, 0x01,
        0x52, 0x41, 0x52, 0x35, 0x1D, 0x77, 0x56, 0x51, 0x03, 0x05, 0x04, 0x00,
    ];

    // corkami rar4.rar - minimal RAR 4.x archive, contains rar4.txt (4 bytes, content "RAR4")
    private static readonly byte[] Rar4SampleBytes =
    [
        0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00, 0xCF, 0x90, 0x73, 0x00, 0x00, 0x0D, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0xB9, 0x9A, 0x74, 0x20, 0x90, 0x2D, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04,
        0x00, 0x00, 0x00, 0x02, 0xA1, 0x34, 0x21, 0x98, 0x14, 0x99, 0x32, 0x50, 0x1D, 0x30, 0x08, 0x00,
        0x20, 0x00, 0x00, 0x00, 0x72, 0x61, 0x72, 0x34, 0x2E, 0x74, 0x78, 0x74, 0x00, 0xB0, 0xBA, 0x5C,
        0x90, 0x52, 0x41, 0x52, 0x34, 0xC4, 0x3D, 0x7B, 0x00, 0x40, 0x07, 0x00,
    ];

    [Fact]
    public void Name_ShouldReturnRar()
    {
        _sut.Name.Should().Be("RAR");
    }

    [Fact]
    public void Extension_ShouldReturnDotRar()
    {
        _sut.Extension.Should().Be(".rar");
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
    public void CanEncrypt_ShouldBeFalse()
    {
        _sut.CanEncrypt.Should().BeFalse();
    }

    [Fact]
    public void SupportsSolid_ShouldBeTrue()
    {
        _sut.SupportsSolid.Should().BeTrue();
    }

    [Fact]
    public void SupportsVolumes_ShouldBeTrue()
    {
        _sut.SupportsVolumes.Should().BeTrue();
    }

    [Fact]
    public void ArchiveFactory_GetFormat_WithRar_ShouldReturnRarEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.Rar);
        format.Should().BeOfType<RarEngine>();
    }

    [Fact]
    public void Open_WithRar5Sample_ShouldReadEntry()
    {
        using var stream = new MemoryStream(Rar5SampleBytes);
        var archive = _sut.Open("test.rar", stream, AccessMode.Read);

        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].Path.Should().Be("rar5.txt");
        archive.Entries[0].Name.Should().Be("rar5.txt");
        archive.Entries[0].Size.Should().Be(4);
        archive.Entries[0].IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void Open_WithRar5Sample_ShouldReturnContent()
    {
        using var stream = new MemoryStream(Rar5SampleBytes);
        var archive = _sut.Open("test.rar", stream, AccessMode.Read);

        var node = archive.Vfs.FindNode("/rar5.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var reader = new StreamReader(content);
        reader.ReadToEnd().Should().Be("RAR5");
    }

    [Fact]
    public void Open_WithRar4Sample_ShouldReadEntry()
    {
        using var stream = new MemoryStream(Rar4SampleBytes);
        var archive = _sut.Open("test.rar", stream, AccessMode.Read);

        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].Path.Should().Be("rar4.txt");
        archive.Entries[0].Size.Should().Be(4);
    }

    [Fact]
    public void Open_WithRar4Sample_ShouldReturnContent()
    {
        using var stream = new MemoryStream(Rar4SampleBytes);
        var archive = _sut.Open("test.rar", stream, AccessMode.Read);

        var node = archive.Vfs.FindNode("/rar4.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var reader = new StreamReader(content);
        reader.ReadToEnd().Should().Be("RAR4");
    }

    [Fact]
    public void Open_OnInvalidStream_ShouldThrow()
    {
        using var stream = new MemoryStream("not a rar file"u8.ToArray());
        var act = () => _sut.Open("bad.rar", stream, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Save_ShouldThrowNotSupported()
    {
        var archive = new Archive
        {
            Format = CompressionFormat.Rar,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new(),
        };
        using var stream = new MemoryStream();
        var act = () => _sut.Save(archive, stream, new CompressionOptions { Format = CompressionFormat.Rar });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ArchiveFactory_DetectRar5FromHeader_ShouldReturnRarEngine()
    {
        using var stream = new MemoryStream(Rar5SampleBytes);
        var format = ArchiveFactory.GetFormatFromFileHeader(stream);
        format.Should().BeOfType<RarEngine>();
    }

    [Fact]
    public void ArchiveFactory_DetectRar4FromHeader_ShouldReturnRarEngine()
    {
        using var stream = new MemoryStream(Rar4SampleBytes);
        var format = ArchiveFactory.GetFormatFromFileHeader(stream);
        format.Should().BeOfType<RarEngine>();
    }

    [Fact]
    public void ArchiveFactory_GetFormatFromExtension_ShouldReturnRarEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension(".rar");
        format.Should().BeOfType<RarEngine>();
    }
}
