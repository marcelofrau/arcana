using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class ZipEngineTests
{
    private readonly ZipEngine _sut = new();

    [Fact]
    public void Name_ShouldReturnZip()
    {
        _sut.Name.Should().Be("ZIP");
    }

    [Fact]
    public void Extension_ShouldReturnDotZip()
    {
        _sut.Extension.Should().Be(".zip");
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

    [Fact]
    public void ArchiveFactory_GetFormat_WithZip_ShouldReturnZipEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.Zip);
        format.Should().BeOfType<ZipEngine>();
    }
}
