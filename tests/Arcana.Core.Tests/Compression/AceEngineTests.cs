using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Compression.Registry;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class AceEngineTests
{
    private readonly AceEngine _sut = new();

    [Fact]
    public void Name_ShouldReturnAce()
    {
        _sut.Name.Should().Be("ACE");
    }

    [Fact]
    public void Extension_ShouldReturnDotAce()
    {
        _sut.Extension.Should().Be(".ace");
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
    public void ArchiveFactory_GetFormat_WithAce_ShouldReturnAceEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.Ace);
        format.Should().BeOfType<AceEngine>();
    }

    [Fact]
    public void Save_ShouldThrowNotSupported()
    {
        var archive = new Archive
        {
            Format = CompressionFormat.Ace,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new(),
        };
        using var stream = new MemoryStream();
        var act = () => _sut.Save(archive, stream, new CompressionOptions { Format = CompressionFormat.Ace });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ArchiveFactory_GetFormatFromExtension_ShouldReturnAceEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension(".ace");
        format.Should().BeOfType<AceEngine>();
    }

    [Fact]
    public void Open_WithSample_ShouldReadEntry()
    {
        using var sample = CreateSample();
        var archive = _sut.Open("test.ace", sample, AccessMode.Read);

        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].Path.Should().Be("hello.txt");
        archive.Entries[0].Size.Should().Be(11);
        archive.Entries[0].IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void Open_WithSample_ShouldReturnContent()
    {
        using var sample = CreateSample();
        var archive = _sut.Open("test.ace", sample, AccessMode.Read);

        var node = archive.Vfs.FindNode("/hello.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var reader = new StreamReader(content);
        reader.ReadToEnd().Should().Be("Hello World");
    }

    private static MemoryStream CreateSample()
    {
        HawkyntInit.Ensure();
        var ops = (IArchiveCreatable)FormatRegistry.GetArchiveOps("Ace");
        var content = "Hello World"u8.ToArray();
        var inputs = new[] { new ArchiveInputInfo("/hello.txt", "hello.txt", false, content) };
        var ms = new MemoryStream();
        ops.Create(ms, inputs, new FormatCreateOptions());
        ms.Position = 0;
        return ms;
    }
}
