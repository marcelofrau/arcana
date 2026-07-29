using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Compression.Registry;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class CabEngineTests
{
    private readonly CabEngine _sut = new();

    private static MemoryStream CreateSample()
    {
        HawkyntInit.Ensure();
        var ops = (IArchiveCreatable)FormatRegistry.GetArchiveOps("Cab");
        var content = "Hello World"u8.ToArray();
        var inputs = new[] { new ArchiveInputInfo("/hello.txt", "hello.txt", false, content) };
        var ms = new MemoryStream();
        ops.Create(ms, inputs, new FormatCreateOptions());
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Name_ShouldReturnCab()
    {
        _sut.Name.Should().Be("CAB");
    }

    [Fact]
    public void Extension_ShouldReturnDotCab()
    {
        _sut.Extension.Should().Be(".cab");
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
    public void ArchiveFactory_GetFormat_WithCab_ShouldReturnCabEngine()
    {
        var format = ArchiveFactory.GetFormat(CompressionFormat.Cab);
        format.Should().BeOfType<CabEngine>();
    }

    [Fact]
    public void Open_WithSample_ShouldReadEntry()
    {
        using var sample = CreateSample();
        var archive = _sut.Open("test.cab", sample, AccessMode.Read);

        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].Path.Should().Be("hello.txt");
        archive.Entries[0].Size.Should().Be(11);
        archive.Entries[0].IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void Open_WithSample_ShouldReturnContent()
    {
        using var sample = CreateSample();
        var archive = _sut.Open("test.cab", sample, AccessMode.Read);

        var node = archive.Vfs.FindNode("/hello.txt");
        node.Should().NotBeNull();
        using var content = node!.OpenRead();
        using var reader = new StreamReader(content);
        reader.ReadToEnd().Should().Be("Hello World");
    }

    [Fact]
    public void Open_OnInvalidStream_ShouldThrow()
    {
        using var stream = new MemoryStream("not a cab file"u8.ToArray());
        var act = () => _sut.Open("bad.cab", stream, AccessMode.Read);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Save_ShouldThrowNotSupported()
    {
        var archive = new Archive
        {
            Format = CompressionFormat.Cab,
            FormatEngine = _sut,
            Entries = Array.Empty<ArchiveEntry>(),
            Vfs = new(),
        };
        using var stream = new MemoryStream();
        var act = () => _sut.Save(archive, stream, new CompressionOptions { Format = CompressionFormat.Cab });
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ArchiveFactory_DetectCabFromHeader_ShouldReturnCabEngine()
    {
        using var sample = CreateSample();
        var format = ArchiveFactory.GetFormatFromFileHeader(sample);
        format.Should().BeOfType<CabEngine>();
    }

    [Fact]
    public void ArchiveFactory_GetFormatFromExtension_ShouldReturnCabEngine()
    {
        var format = ArchiveFactory.GetFormatFromExtension(".cab");
        format.Should().BeOfType<CabEngine>();
    }
}
