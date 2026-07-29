namespace Arcana.Core.Compression;

public static class ArchiveFactory
{
    public static IArchiveFormat GetFormat(CompressionFormat format)
    {
        return format switch
        {
            CompressionFormat.Zip => new Formats.ZipEngine(),
            CompressionFormat.SevenZip => new Formats.SevenZipEngine(),
            CompressionFormat.Zstandard => new Formats.ZstdEngine(),
            CompressionFormat.Brotli => throw new NotSupportedException("Not yet implemented"),
            CompressionFormat.Lz4 => throw new NotSupportedException("Not yet implemented"),
            CompressionFormat.Lzma => throw new NotSupportedException("Not yet implemented"),
            CompressionFormat.Xz => throw new NotSupportedException("Not yet implemented"),
            CompressionFormat.BZip2 => throw new NotSupportedException("Not yet implemented"),
            CompressionFormat.GZip => throw new NotSupportedException("Not yet implemented"),
            CompressionFormat.Tar => throw new NotSupportedException("Not yet implemented"),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static IArchiveFormat GetFormatFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".zip" => GetFormat(CompressionFormat.Zip),
            ".7z" => GetFormat(CompressionFormat.SevenZip),
            ".zst" or ".zstd" => GetFormat(CompressionFormat.Zstandard),
            _ => throw new NotSupportedException($"Unsupported extension: {extension}")
        };
    }

    public static IArchiveFormat GetFormatFromFileHeader(Stream stream)
    {
        var header = new byte[8];
        var read = stream.Read(header, 0, 8);
        stream.Seek(-read, SeekOrigin.Current);

        return (read >= 4, header) switch
        {
            (true, [0x50, 0x4B, 0x03, 0x04, ..]) => GetFormat(CompressionFormat.Zip),
            (true, [0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C, ..]) => GetFormat(CompressionFormat.SevenZip),
            (true, [0x28, 0xB5, 0x2F, 0xFD, ..]) => GetFormat(CompressionFormat.Zstandard),
            _ => throw new NotSupportedException("Could not detect archive format from header")
        };
    }

    public static Archive Open(string path, AccessMode mode = AccessMode.Read)
    {
        using var stream = File.OpenRead(path);
        var format = GetFormatFromFileHeader(stream);
        return format.Open(path, stream, mode);
    }

    public static async Task<Archive> OpenAsync(string path, AccessMode mode = AccessMode.Read, CancellationToken ct = default)
    {
        var stream = File.OpenRead(path);
        var format = GetFormatFromFileHeader(stream);
        return await format.OpenAsync(path, stream, mode, ct);
    }
}
