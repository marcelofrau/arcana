using Serilog;

namespace Arcana.Core.Compression;

public static class ArchiveFormats
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ArchiveFormats));
    public static CompressionFormat Parse(string name)
    {
        Log.Debug("Resolving requested format {Name}", name);
        return name.TrimStart('.').ToLowerInvariant() switch
        {
            "zip" => CompressionFormat.Zip,
            "7z" or "sevenzip" => CompressionFormat.SevenZip,
            "tar" => CompressionFormat.Tar,
            "tar.gz" or "tgz" => CompressionFormat.TarGz,
            "tar.bz2" or "tbz2" => CompressionFormat.TarBz2,
            "tar.xz" or "txz" => CompressionFormat.TarXz,
            "tar.zst" or "tar.zstd" or "tzst" => CompressionFormat.TarZstd,
            "gz" or "gzip" => CompressionFormat.GZip,
            "bz2" or "bzip2" => CompressionFormat.BZip2,
            "xz" => CompressionFormat.Xz,
            "lzma" => CompressionFormat.Lzma,
            "zst" or "zstd" => CompressionFormat.Zstandard,
            "br" or "brotli" => CompressionFormat.Brotli,
            "lz4" => CompressionFormat.Lz4,
            "snappy" => CompressionFormat.Snappy,
            "rar" => CompressionFormat.Rar,
            "iso" => CompressionFormat.Iso,
            _ => throw new NotSupportedException($"Unsupported output format: {name}"),
        };
    }

    public static CompressionFormat FromPath(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        var tar = name.LastIndexOf(".tar.", StringComparison.Ordinal);
        return Parse(tar >= 0 ? name[(tar + 1)..] : Path.GetExtension(path));
    }

    public static bool IsSingleFile(CompressionFormat format) => format is
        CompressionFormat.GZip or CompressionFormat.BZip2 or CompressionFormat.Xz or CompressionFormat.Lzma
        or CompressionFormat.Zstandard or CompressionFormat.Brotli or CompressionFormat.Lz4 or CompressionFormat.Snappy;
}
