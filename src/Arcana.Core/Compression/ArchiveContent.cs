using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.Core.Compression;

public static class ArchiveContent
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ArchiveContent));
    private static readonly uint[] Table = BuildTable();

    internal static async Task<long> CopyBoundedAsync(Stream source, Stream destination, long limit, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long size = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            size = checked(size + read);
            if (size > limit) throw new InvalidDataException("Archive exceeds the expanded-content limit.");
            await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
        }
        Log.Verbose("Copied {Bytes} bytes of bounded archive content", size);
        return size;
    }

    public static async Task<long> CopyVerifiedAsync(ArchiveNode node, Stream destination, CancellationToken ct = default,
        long limit = ArchiveLimits.MaxExpandedBytes)
    {
        await using var source = node.OpenRead();
        var buffer = new byte[81920];
        var size = 0L;
        uint crc = uint.MaxValue;
        int read;
        while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) != 0)
        {
            size = checked(size + read);
            if (size > limit) throw new InvalidDataException("Extraction exceeds the expanded-content limit.");
            if (node.OriginalSize > 0 && size > node.OriginalSize)
                throw new InvalidDataException($"Entry exceeds its declared size: {node.FullPath}");
            if (node.Crc32.HasValue)
                for (var i = 0; i < read; i++) crc = (crc >> 8) ^ Table[(crc ^ buffer[i]) & 0xFF];
            await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
        }
        if (node.OriginalSize > 0 && size != node.OriginalSize)
            throw new InvalidDataException($"Truncated archive entry: {node.FullPath}");
        if (node.Crc32.HasValue && (crc ^ uint.MaxValue) != node.Crc32.Value)
            throw new InvalidDataException($"CRC mismatch: {node.FullPath}");
        Log.Debug("Verified content for {Path} ({Size} bytes, stored checksum {HasChecksum})", node.FullPath, size, node.Crc32.HasValue);
        return size;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var c = i;
            for (var bit = 0; bit < 8; bit++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }
}
