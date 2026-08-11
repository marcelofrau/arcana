using System.Text;
using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class IsoEngineTests
{
    private const int SectorSize = 2048;

    [Fact]
    public void Name_ShouldReturnIso()
    {
        new IsoEngine().Name.Should().Be("ISO");
    }

    [Fact]
    public void CanRead_ShouldBeTrue_AndCanWriteFalse()
    {
        var sut = new IsoEngine();
        sut.CanRead.Should().BeTrue();
        sut.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void ArchiveFactory_GetFormat_WithIso_ShouldReturnIsoEngine()
    {
        ArchiveFactory.GetFormat(CompressionFormat.Iso).Should().BeOfType<IsoEngine>();
    }

    [Fact]
    public void GetFormatFromFileHeader_ShouldDetectIso_FromSector16Pvd()
    {
        var image = BuildIso(joliet: false);
        using var stream = new MemoryStream(image);
        var engine = ArchiveFactory.GetFormatFromFileHeader(stream);
        engine.Should().BeOfType<IsoEngine>();
    }

    [Fact]
    public void Open_ShouldListRawIso9660Entries_WithRawNames()
    {
        using var opened = Open(BuildIso(joliet: false));
        opened.Entries.Select(e => e.Path)
            .Should().Contain(new[] { "/HELLO.TXT", "/SUB", "/SUB/NESTED.DAT" });
        opened.Entries.Single(e => e.Path == "/HELLO.TXT").Size.Should().Be(11);
    }

    [Fact]
    public void Open_ShouldUseJolietNames_WhenSvdPresent()
    {
        using var opened = Open(BuildIso(joliet: true));
        opened.Entries.Select(e => e.Path)
            .Should().Contain(new[] { "/hello.txt", "/sub", "/sub/nested.dat" });
    }

    [Fact]
    public void OpenRead_ShouldReturnFileBytes_FromDiskLazily()
    {
        using var opened = Open(BuildIso(joliet: false));
        var node = opened.Vfs.FindNode("/HELLO.TXT")!;
        using var stream = node.OpenRead();
        using var reader = new StreamReader(stream);
        reader.ReadToEnd().Should().Be("Hello World");
    }

    [Fact]
    public void OpenRead_ShouldReturnNestedFileBytes()
    {
        using var opened = Open(BuildIso(joliet: false));
        var node = opened.Vfs.FindNode("/SUB/NESTED.DAT")!;
        using var stream = node.OpenRead();
        var buffer = new byte[4];
        stream.Read(buffer, 0, 4).Should().Be(4);
        buffer.Should().BeEquivalentTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });
    }

    [Fact]
    public void GetFormatFromFileHeader_ShouldThrow_ForNonIsoData()
    {
        using var stream = new MemoryStream(new byte[0x9000]);
        var act = () => ArchiveFactory.GetFormatFromFileHeader(stream);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Open_ShouldIgnore_SpuriousSecondDescriptorSet()
    {
        // Mint-style hybrid images carry a second, spurious descriptor set whose
        // Joliet root does NOT match the PVD. The reader must stop at the first
        // Volume Descriptor Set Terminator and use the first Joliet SVD.
        using var opened = Open(BuildIso(joliet: true, spuriousSet: true));
        opened.Entries.Select(e => e.Path)
            .Should().Contain(new[] { "/hello.txt", "/sub", "/sub/nested.dat" });
        opened.Entries.Select(e => e.Path)
            .Should().NotContain("/decoy.bin");
    }

    private static TestArchive Open(byte[] image)
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"arcana-test-{Guid.NewGuid():N}.iso");
        File.WriteAllBytes(path, image);
        return new TestArchive(path);
    }

    private static byte[] BuildIso(bool joliet, bool spuriousSet = false)
    {
        const int sectors = 64;
        var iso = new byte[sectors * SectorSize];

        var rawRoot = WriteDirectoryTree(
            iso,
            rootSector: 32,
            dirSector: 40,
            helloSector: 48,
            nestedSector: 49,
            joliet: false);

        WriteVolumeDescriptor(iso, sector: 16, type: 1, rootRecord: rawRoot);

        if (joliet)
        {
            var jolietRoot = WriteDirectoryTree(
                iso,
                rootSector: 44,
                dirSector: 40,
                helloSector: 48,
                nestedSector: 49,
                joliet: true);
            WriteVolumeDescriptor(iso, sector: 17, type: 2, rootRecord: jolietRoot, joliet: true);
        }

        if (spuriousSet)
        {
            // end the real descriptor set, then write a decoy one: a Joliet SVD
            // (sector 33) whose root (sector 55) holds a decoy file. The reader
            // must stop at the terminator and ignore this second set.
            iso[18 * SectorSize] = 255;

            var decoy = new byte[SectorSize];
            var at = 0;
            at += WriteRecord(decoy, at, 55, SectorSize, 0x02, ".", joliet: true);
            at += WriteRecord(decoy, at, 32, SectorSize, 0x02, "..", joliet: true);
            at += WriteRecord(decoy, at, 56, 4, 0x00, "decoy.bin", joliet: true);
            Array.Copy(decoy, 0, iso, 55 * SectorSize, SectorSize);

            WriteVolumeDescriptor(iso, 33, 2, MakeRootRecord(55), joliet: true);
            iso[34 * SectorSize] = 255;
        }

        var hello = Encoding.ASCII.GetBytes("Hello World");
        Array.Copy(hello, 0, iso, 48 * SectorSize, hello.Length);
        iso[49 * SectorSize] = 0xAA;
        iso[49 * SectorSize + 1] = 0xBB;
        iso[49 * SectorSize + 2] = 0xCC;
        iso[49 * SectorSize + 3] = 0xDD;

        return iso;
    }

    /// <summary>Writes a root directory (rootSector) with ".", "..", a file and a
    /// subdir (dirSector), and the subdir with ".", ".." and a nested file.</summary>
    private static byte[] WriteDirectoryTree(
        byte[] iso, int rootSector, int dirSector, int helloSector, int nestedSector, bool joliet)
    {
        var root = new byte[SectorSize];
        var at = 0;
        at += WriteRecord(root, at, (uint)rootSector, SectorSize, 0x02, ".", joliet);
        at += WriteRecord(root, at, (uint)rootSector, SectorSize, 0x02, "..", joliet);
        at += WriteRecord(root, at, (uint)helloSector, 11, 0x00, joliet ? "hello.txt" : "HELLO.TXT", joliet);
        at += WriteRecord(root, at, (uint)dirSector, SectorSize, 0x02, joliet ? "sub" : "SUB", joliet);
        Array.Copy(root, 0, iso, rootSector * SectorSize, SectorSize);

        var sub = new byte[SectorSize];
        at = 0;
        at += WriteRecord(sub, at, (uint)dirSector, SectorSize, 0x02, ".", joliet);
        at += WriteRecord(sub, at, (uint)rootSector, SectorSize, 0x02, "..", joliet);
        at += WriteRecord(sub, at, (uint)nestedSector, 4, 0x00, joliet ? "nested.dat" : "NESTED.DAT", joliet);
        Array.Copy(sub, 0, iso, dirSector * SectorSize, SectorSize);

        return MakeRootRecord(rootSector);
    }

    private static byte[] MakeRootRecord(int rootSector)
    {
        var rootRecord = new byte[34];
        rootRecord[0] = 34;
        WriteBothEndian(rootRecord, 2, (uint)rootSector);
        WriteBothEndian(rootRecord, 10, SectorSize);
        rootRecord[25] = 0x02;
        rootRecord[32] = 1;
        rootRecord[33] = 0x00;
        return rootRecord;
    }

    private static void WriteVolumeDescriptor(byte[] iso, int sector, byte type, byte[] rootRecord, bool joliet = false)
    {
        var pvd = iso.AsSpan(sector * SectorSize, SectorSize);
        pvd[0] = type;
        "CD001"u8.CopyTo(pvd.Slice(1));
        pvd[6] = 1;
        WriteBothEndian(pvd, 80, 64); // volume space size
        if (joliet)
        {
            pvd[88] = 0x25; // '%'
            pvd[89] = 0x2F; // '/'
            pvd[90] = 0x40; // '@'
        }
        rootRecord.CopyTo(pvd.Slice(156, 34));
    }

    private static int WriteRecord(byte[] dir, int at, uint extent, uint length, byte flags, string id, bool joliet)
    {
        var idBytes = joliet ? Encoding.BigEndianUnicode.GetBytes(id) : Encoding.ASCII.GetBytes(id);
        var recLen = 33 + idBytes.Length + (idBytes.Length % 2 == 0 ? 1 : 0);
        dir[at] = (byte)recLen;
        WriteBothEndian(dir, at + 2, extent);
        WriteBothEndian(dir, at + 10, length);
        dir[at + 25] = flags;
        dir[at + 32] = (byte)idBytes.Length;
        Array.Copy(idBytes, 0, dir, at + 33, idBytes.Length);
        return recLen;
    }

    private static void WriteBothEndian(Span<byte> b, int off, uint value)
    {
        b[off] = (byte)value;
        b[off + 1] = (byte)(value >> 8);
        b[off + 2] = (byte)(value >> 16);
        b[off + 3] = (byte)(value >> 24);
        b[off + 4] = (byte)(value >> 24);
        b[off + 5] = (byte)(value >> 16);
        b[off + 6] = (byte)(value >> 8);
        b[off + 7] = (byte)value;
    }

    private sealed class TestArchive : IDisposable
    {
        private readonly string _path;
        private readonly Archive _inner;
        public TestArchive(string path)
        {
            _path = path;
            _inner = ArchiveFactory.Open(path);
        }
        public IReadOnlyList<ArchiveEntry> Entries => _inner.Entries;
        public Filesystem.VirtualFileSystem Vfs => _inner.Vfs;
        public void Dispose()
        {
            _inner.Dispose();
            try { File.Delete(_path); } catch { /* best effort */ }
        }
    }
}
