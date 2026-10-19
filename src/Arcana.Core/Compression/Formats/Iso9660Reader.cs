using System.Text;
using Serilog;

namespace Arcana.Core.Compression.Formats;

internal sealed record IsoDirectoryEntry(
    string Name,
    string FullPath,
    bool IsDirectory,
    long Offset,
    long Length,
    DateTime LastModified);

/// <summary>
/// Minimal ISO9660 reader. Parses the Primary Volume Descriptor (sector 16),
/// prefers Joliet (supplementary volume descriptors with UCS-2 names) when
/// present, then walks the directory hierarchy. Entries are metadata only —
/// file bytes stay on disk and are exposed lazily via <see cref="Filesystem.FileSliceStream"/>.
/// </summary>
internal sealed class Iso9660Reader
{
    private const int SectorSize = 2048;
    private static readonly ILogger Log = Serilog.Log.ForContext<Iso9660Reader>();

    private readonly Stream _stream;
    private readonly long _volumeBytes;
    private readonly bool _joliet;
    private readonly byte[] _rootRecord;

    private Iso9660Reader(Stream stream, long volumeBytes, bool joliet, byte[] rootRecord)
    {
        _stream = stream;
        _volumeBytes = volumeBytes;
        _joliet = joliet;
        _rootRecord = rootRecord;
    }

    public bool Joliet => _joliet;

    public static bool TryCreate(Stream stream, out Iso9660Reader? reader)
    {
        reader = null;

        var pvd = ReadSector(stream, 16);
        if (!IsVolumeDescriptor(pvd, 1))
            return false;

        var volumeBytes = ReadLE32(pvd, 80) * (long)SectorSize;
        var root = CopyRange(pvd, 156, 34);

        var joliet = false;
        var nameRoot = root;
        var maxSector = (int)Math.Min(64, volumeBytes / SectorSize);
        for (var sector = 17; sector < maxSector; sector++)
        {
            var svd = ReadSector(stream, sector);
            // Volume Descriptor Set Terminator (type 255) ends the descriptor set.
            // Some ISOs carry a second, spurious descriptor set later (e.g. hybrid
            // Mint images) whose Joliet root does NOT match the PVD — stop here.
            if (svd[0] == 255)
                break;
            if (!IsVolumeDescriptor(svd, 2))
                continue;
            // Joliet escape sequences: "%/@" (level 1), "%/C" (level 2), "%/E" (level 3)
            if (svd[88] == 0x25 && svd[89] == 0x2F && svd[90] is 0x40 or 0x43 or 0x45)
            {
                joliet = true;
                nameRoot = CopyRange(svd, 156, 34);
                break;
            }
        }

        reader = new Iso9660Reader(stream, volumeBytes, joliet, nameRoot);
        return true;
    }

    public List<IsoDirectoryEntry> Walk(CancellationToken ct)
    {
        var entries = new List<IsoDirectoryEntry>();
        WalkDirectory("/", _rootRecord, entries, ct, depth: 0);
        return entries;
    }

    private void WalkDirectory(string path, byte[] dirRecord, List<IsoDirectoryEntry> entries, CancellationToken ct, int depth)
    {
        if (depth > 64)
            throw new InvalidDataException("ISO directory nesting too deep");
        ct.ThrowIfCancellationRequested();

        var dirOffset = ReadLE32(dirRecord, 2) * (long)SectorSize;
        var dirLength = ReadLE32(dirRecord, 10);
        if (dirLength <= 0 || dirLength > 32 * 1024 * 1024)
            return;

        var buffer = ReadBlock(dirOffset, (int)dirLength);
        var i = 0;
        while (i + 1 <= buffer.Length)
        {
            var recLen = buffer[i];
            if (recLen == 0)
            {
                // padding to the next sector boundary
                i = ((i / SectorSize) + 1) * SectorSize;
                continue;
            }
            if (recLen < 34 || i + recLen > buffer.Length)
                break;

            var record = buffer.AsSpan(i, recLen);
            var idLen = record[32];
            if (idLen > 0)
            {
                // "." (0x00) and ".." (0x01) entries in raw ISO9660
                var isDot = idLen == 1 && (record[33] == 0x00 || record[33] == 0x01);
                if (!isDot)
                {
                    var id = DecodeName(record.Slice(33, idLen));
                    if (id is "." or ".." or "" || id.Length > 255)
                    {
                        i += recLen;
                        continue;
                    }

                    var flags = record[25];
                    var isDir = (flags & 0x02) != 0;
                    var offset = ReadLE32(record, 2) * (long)SectorSize;
                    var length = ReadLE32(record, 10);
                    var childPath = path == "/" ? "/" + id : path + "/" + id;

                    if (isDir)
                    {
                        entries.Add(new IsoDirectoryEntry(id, childPath, true, offset, length, ParseDate(record)));
                        WalkDirectory(childPath, record[..recLen].ToArray(), entries, ct, depth + 1);
                    }
                    else if ((flags & 0x80) != 0)
                    {
                        // multi-extent continuation record — first extent already added
                        Log.Warning("Skipping multi-extent file {Path}", childPath);
                    }
                    else
                    {
                        entries.Add(new IsoDirectoryEntry(id, childPath, false, offset, length, ParseDate(record)));
                    }
                }
            }
            i += recLen;
        }
    }

    private string DecodeName(ReadOnlySpan<byte> id)
    {
        var name = _joliet ? DecodeUcs2(id) : Encoding.ASCII.GetString(id);
        var semicolon = name.IndexOf(';');
        if (semicolon >= 0)
            name = name[..semicolon];
        return name.TrimEnd(' ');
    }

    private static string DecodeUcs2(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[bytes.Length / 2];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = (char)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);
        return new string(chars);
    }

    private byte[] ReadBlock(long offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > _volumeBytes)
            throw new InvalidDataException($"ISO record {offset}+{length} outside volume ({_volumeBytes} bytes)");
        var buffer = new byte[length];
        _stream.Position = offset;
        var read = 0;
        while (read < length)
        {
            var n = _stream.Read(buffer, read, length - read);
            if (n <= 0)
                throw new InvalidDataException("Truncated ISO volume");
            read += n;
        }
        return buffer;
    }

    private static byte[] ReadSector(Stream stream, int sector)
    {
        var buffer = new byte[SectorSize];
        stream.Position = sector * (long)SectorSize;
        var read = 0;
        while (read < SectorSize)
        {
            var n = stream.Read(buffer, read, SectorSize - read);
            if (n <= 0)
                break;
            read += n;
        }
        return buffer;
    }

    private static bool IsVolumeDescriptor(byte[] sector, byte type) =>
        sector.Length >= 7
        && sector[0] == type
        && sector[1] == (byte)'C' && sector[2] == (byte)'D'
        && sector[3] == (byte)'0' && sector[4] == (byte)'0'
        && sector[5] == (byte)'1' && sector[6] == 1;

    private static byte[] CopyRange(byte[] source, int offset, int length)
    {
        var copy = new byte[length];
        Array.Copy(source, offset, copy, 0, length);
        return copy;
    }

    private static DateTime ParseDate(ReadOnlySpan<byte> r)
    {
        var year = 1900 + r[18];
        var month = r[19];
        var day = r[20];
        if (year is < 1970 or > 2200 || month is < 1 or > 12 || day is < 1 or > 31)
            return DateTime.MinValue;
        try
        {
            return new DateTime(year, month, day, r[21], r[22], r[23], DateTimeKind.Local);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static uint ReadLE32(ReadOnlySpan<byte> b, int o) =>
        (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
}
