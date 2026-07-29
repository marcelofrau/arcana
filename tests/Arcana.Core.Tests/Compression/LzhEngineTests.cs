using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Compression.Registry;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class LzhEngineTests
{
    private readonly LzhEngine _sut = new();

    [Fact]
    public void Name_ShouldReturnLzh()
    {
        _sut.Name.Should().Be("LZH");
    }

    [Fact]
    public void Extension_ShouldReturnDotLzh()
    {
        _sut.Extension.Should().Be(".lzh");
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
    public void SupportsSolid_ShouldBeFalse()
    {
        _sut.SupportsSolid.Should().BeFalse();
    }

    [Fact]
    public void SupportsVolumes_ShouldBeFalse()
    {
        _sut.SupportsVolumes.Should().BeFalse();
    }

    [Fact]
    public void ArchiveFactory_GetFormat_WithLzh_ShouldReturnLzhEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.Lzh);
        format.Should().BeOfType<LzhEngine>();
    }

    [Fact]
    public void Save_ShouldThrowNotSupported()
    {
        var archive = new Archive
        {
            Format = CompressionFormat.Lzh,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new(),
        };
        using var stream = new MemoryStream();
        var act = () => _sut.Save(archive, stream, new CompressionOptions { Format = CompressionFormat.Lzh });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ArchiveFactory_GetFormatFromExtension_ShouldReturnLzhEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension(".lzh");
        format.Should().BeOfType<LzhEngine>();
    }

    [Fact]
    public void ArchiveFactory_GetFormatFromExtension_WithLha_ShouldReturnLzhEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension(".lha");
        format.Should().BeOfType<LzhEngine>();
    }

    [Fact]
    public void Open_WithSample_ShouldReadEntry()
    {
        using var sample = CreateSample();
        var archive = _sut.Open("test.lzh", sample, AccessMode.Read);

        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].Path.Should().Be("hello.txt");
        archive.Entries[0].Size.Should().Be(11);
        archive.Entries[0].IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void Open_WithSample_ShouldReturnContent()
    {
        using var sample = CreateSample();
        var archive = _sut.Open("test.lzh", sample, AccessMode.Read);

        var node = archive.Vfs.FindNode("/hello.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var reader = new StreamReader(content);
        reader.ReadToEnd().Should().Be("Hello World");
    }

    private static MemoryStream CreateSample()
    {
        HawkyntInit.Ensure();
        var ops = (IArchiveCreatable)FormatRegistry.GetArchiveOps("Lzh");
        var content = "Hello World"u8.ToArray();
        var inputs = new[] { new ArchiveInputInfo("/hello.txt", "hello.txt", false, content) };
        var ms = new MemoryStream();
        ops.Create(ms, inputs, new FormatCreateOptions());
        ms.Position = 0;
        return ms;
    }
}
