using Arcana.Core.Compression.Formats;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class RealIsoDebugTests
{
    private const string RealIso = @"D:\tests\arcana\raw\linuxmint-22.3-cinnamon-64bit.iso";
    private const int Sector = 2048;

    [Fact]
    public void Dump_RootDirectoryRecords()
    {
        if (!File.Exists(RealIso)) return;

        using var fs = new FileStream(RealIso, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var pvd = ReadSector(fs, 16);
        var volumeBytes = ReadLE32(pvd, 80) * (long)Sector;
        var root = pvd.AsSpan(156, 34);
        var extent = ReadLE32(root, 2);
        var length = ReadLE32(root, 10);
        var jolietEscape = new byte[] { pvd[88], pvd[89], pvd[90] };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"volumeBytes={volumeBytes} rootExtent={extent} rootLen={length} pvd[88..91]={BitConverter.ToString(jolietEscape)}");

        // Locate ALL Joliet SVDs in sectors 17..63 (reader's scan window), dump each root extent
        for (var s = 17; s < 64; s++)
        {
            var svd = ReadSector(fs, s);
            if (svd[0] == 2 && svd[1] == (byte)'C' && svd[2] == (byte)'D' && svd[5] == (byte)'1'
                && svd[88] == 0x25 && svd[89] == 0x2F && (svd[90] == 0x40 || svd[90] == 0x43 || svd[90] == 0x45))
            {
                var root2 = svd.AsSpan(156, 34);
                sb.AppendLine($"Joliet SVD at sector {s}, escape={svd[88]:X2} {svd[89]:X2} {svd[90]:X2}, rootExtent={ReadLE32(root2, 2)} rootLen={ReadLE32(root2, 10)}");
            }
            else if (svd[0] == 2 && svd[1] == (byte)'C' && svd[2] == (byte)'D' && svd[5] == (byte)'1')
            {
                sb.AppendLine($"Non-Joliet SVD at sector {s}, escape={svd[88]:X2} {svd[89]:X2} {svd[90]:X2}");
            }
            else if (svd[0] == 255)
            {
                sb.AppendLine($"Volume Descriptor Set Terminator at sector {s}");
            }
        }

        // Dump PVD root directory records
        sb.AppendLine("--- PVD root records ---");
        DumpDirectory(fs, extent, length, sb);

        var output = sb.ToString();
        // Write dump to temp so it can be inspected even when truncated
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "arcana-iso-dump.txt"), output);
    }

    [Fact]
    public void Dump_WalkEntries()
    {
        if (!File.Exists(RealIso)) return;

        using var fs = new FileStream(RealIso, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var t = typeof(IsoEngine).Assembly.GetType("Arcana.Core.Compression.Formats.Iso9660Reader")!;
        var tryCreate = t.GetMethod("TryCreate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        var parameters = new object?[] { fs, null };
        var ok = (bool)tryCreate.Invoke(null, parameters)!;
        var reader = parameters[1];

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"TryCreate={ok}");
        if (reader != null)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var volumeBytes = (long)t.GetField("_volumeBytes", flags)!.GetValue(reader)!;
            var joliet = (bool)t.GetField("_joliet", flags)!.GetValue(reader)!;
            var rootRecord = (byte[])t.GetField("_rootRecord", flags)!.GetValue(reader)!;
            sb.AppendLine($"volumeBytes={volumeBytes} joliet={joliet} rootExtent={ReadLE32(rootRecord, 2)} rootLen={ReadLE32(rootRecord, 10)}");
            sb.AppendLine($"rootRecord idLen={rootRecord[32]} idHex={BitConverter.ToString(rootRecord.AsSpan(33, rootRecord[32]).ToArray())}");

            // dump the root directory the reader actually reads
            DumpDirectory(fs, ReadLE32(rootRecord, 2), ReadLE32(rootRecord, 10), sb);
        }

        var output = sb.ToString();
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "arcana-iso-walk.txt"), output);
    }

    private static void DumpDirectory(FileStream fs, uint extent, uint length, System.Text.StringBuilder sb)
    {
        var buffer = new byte[length];
        fs.Position = extent * Sector;
        fs.Read(buffer, 0, buffer.Length);

        var i = 0;
        var count = 0;
        while (i + 1 <= buffer.Length && count < 12)
        {
            var recLen = buffer[i];
            if (recLen == 0)
            {
                sb.AppendLine($"  pad@i={i}");
                i = ((i / Sector) + 1) * Sector;
                continue;
            }
            if (recLen < 34 || i + recLen > buffer.Length) break;

            var idLen = buffer[i + 32];
            var idBytes = buffer.AsSpan(i + 33, idLen).ToArray();
            sb.AppendLine($"  i={i} recLen={recLen} flags={buffer[i + 25]:X2} idLen={idLen} idHex={BitConverter.ToString(idBytes)}");
            i += recLen;
            count++;
        }
    }

    private static byte[] ReadSector(Stream fs, int sector)
    {
        var buf = new byte[Sector];
        fs.Position = sector * (long)Sector;
        fs.Read(buf, 0, Sector);
        return buf;
    }

    private static uint ReadLE32(ReadOnlySpan<byte> b, int o)
        => (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
}
