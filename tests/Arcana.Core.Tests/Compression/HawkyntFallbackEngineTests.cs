using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class HawkyntFallbackEngineTests
{
    private readonly HawkyntFallbackEngine _sut = new();

    [Fact]
    public void Name_ShouldReturnHawkynt()
    {
        _sut.Name.Should().Be("Hawkynt");
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
    public void Save_ShouldThrowNotSupported()
    {
        var archive = new Archive
        {
            Format = CompressionFormat.Hawkynt,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new(),
        };
        using var stream = new MemoryStream();
        var act = () => _sut.Save(archive, stream, new CompressionOptions { Format = CompressionFormat.Hawkynt });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void FindDescriptor_WithCabMagic_ShouldReturnCabDescriptor()
    {
        var bytes = new byte[] { 0x4D, 0x53, 0x43, 0x46, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(bytes);
        var desc = _sut.FindDescriptor("unknown.bin", stream);
        desc.Should().NotBeNull();
        desc!.Id.Should().Be("Cab");
    }

    [Fact]
    public void FindDescriptor_WithArjMagic_ShouldReturnArjDescriptor()
    {
        var bytes = new byte[] { 0x60, 0xEA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(bytes);
        var desc = _sut.FindDescriptor("unknown.bin", stream);
        desc.Should().NotBeNull();
        desc!.Id.Should().Be("Arj");
    }

    [Fact]
    public void FindDescriptor_WithUnknownData_ShouldReturnNull()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(bytes);
        var desc = _sut.FindDescriptor("random.bin", stream);
        desc.Should().BeNull();
    }

    [Fact]
    public void FindDescriptor_ByExtension_ShouldReturnDescriptor()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        using var stream = new MemoryStream(bytes);
        var desc = _sut.FindDescriptor("test.cab", stream);
        desc.Should().NotBeNull();
        desc!.Id.Should().Be("Cab");
    }

    [Fact]
    public void GetFormatFromExtension_WithUnknownExt_ShouldReturnHawkyntEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension(".xyz");
        format.Should().BeOfType<HawkyntFallbackEngine>();
    }

    [Fact]
    public void GetFormatFromExtension_WithNoExt_ShouldReturnHawkyntEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension("");
        format.Should().BeOfType<HawkyntFallbackEngine>();
    }

}
