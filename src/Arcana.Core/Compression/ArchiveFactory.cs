using Serilog;

namespace Arcana.Core.Compression;

public static class ArchiveFactory
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ArchiveFactory));
    public static IArchiveFormat GetFormat(CompressionFormat format)
    {
        var result = format switch
        {
            CompressionFormat.Zip => (IArchiveFormat)new Formats.ZipEngine(),
            CompressionFormat.SevenZip => new Formats.SevenZipEngine(),
            CompressionFormat.Zstandard => new Formats.ZstdEngine(),
            CompressionFormat.Tar => new Formats.TarEngine(),
            CompressionFormat.TarGz => new Formats.TarEngine(),
            CompressionFormat.TarBz2 => new Formats.TarEngine(),
            CompressionFormat.TarXz => new Formats.TarEngine(),
            CompressionFormat.TarZstd => new Formats.TarEngine(),
            CompressionFormat.Rar => new Formats.RarEngine(),
            CompressionFormat.Ace => new Formats.AceEngine(),
            CompressionFormat.Arj => new Formats.ArjEngine(),
            CompressionFormat.Cab => new Formats.CabEngine(),
            CompressionFormat.Lzh => new Formats.LzhEngine(),
            CompressionFormat.Hawkynt => new Formats.HawkyntFallbackEngine(),
            CompressionFormat.Brotli => new Formats.BrotliEngine(),
            CompressionFormat.Lz4 => new Formats.Lz4Engine(),
            CompressionFormat.Lzma => new Formats.LzmaEngine(),
            CompressionFormat.Xz => new Formats.XzEngine(),
            CompressionFormat.BZip2 => new Formats.BZip2Engine(),
            CompressionFormat.GZip => new Formats.GzipEngine(),
            CompressionFormat.Snappy => new Formats.SnappyEngine(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
        Log.Debug("Resolved {CompressionFormat} engine", format);
        return result;
    }

    public static IArchiveFormat GetFormatFromExtension(string extension)
    {
        var engine = extension.ToLowerInvariant() switch
        {
            ".zip" => GetFormat(CompressionFormat.Zip),
            ".7z" => GetFormat(CompressionFormat.SevenZip),
            ".zst" or ".zstd" => GetFormat(CompressionFormat.Zstandard),
            ".tar" => GetFormat(CompressionFormat.Tar),
            ".tar.gz" or ".tgz" => GetFormat(CompressionFormat.TarGz),
            ".tar.bz2" or ".tbz2" => GetFormat(CompressionFormat.TarBz2),
            ".tar.xz" or ".txz" => GetFormat(CompressionFormat.TarXz),
            ".tar.zst" or ".tzst" => GetFormat(CompressionFormat.TarZstd),
            ".rar" => GetFormat(CompressionFormat.Rar),
            ".ace" => GetFormat(CompressionFormat.Ace),
            ".arj" => GetFormat(CompressionFormat.Arj),
            ".cab" => GetFormat(CompressionFormat.Cab),
            ".lzh" or ".lha" => GetFormat(CompressionFormat.Lzh),
            ".gz" => GetFormat(CompressionFormat.GZip),
            ".bz2" => GetFormat(CompressionFormat.BZip2),
            ".xz" => GetFormat(CompressionFormat.Xz),
            ".lzma" => GetFormat(CompressionFormat.Lzma),
            ".br" => GetFormat(CompressionFormat.Brotli),
            ".lz4" => GetFormat(CompressionFormat.Lz4),
            ".snappy" => GetFormat(CompressionFormat.Snappy),
            _ => new Formats.HawkyntFallbackEngine()
        };
        Log.Debug("Extension {Extension} -> {EngineName}", extension, engine.Name);
        return engine;
    }

    public static IArchiveFormat GetFormatFromFileHeader(Stream stream)
    {
        var header = new byte[8];
        var read = stream.Read(header, 0, 8);
        stream.Seek(-read, SeekOrigin.Current);

        var engine = (read >= 4, header) switch
        {
            (true, [0x50, 0x4B, 0x03, 0x04, ..]) => GetFormat(CompressionFormat.Zip),
            (true, [0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C, ..]) => GetFormat(CompressionFormat.SevenZip),
            (true, [0x28, 0xB5, 0x2F, 0xFD, ..]) => GetFormat(CompressionFormat.Zstandard),
            (true, [0x1F, 0x8B, ..]) => GetFormat(CompressionFormat.GZip),
            (true, [0x42, 0x5A, 0x68, ..]) => GetFormat(CompressionFormat.BZip2),
            (true, [0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00, ..]) => GetFormat(CompressionFormat.Xz),
            (true, [0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, ..]) => GetFormat(CompressionFormat.Rar),
            (true, [0x2A, 0x2A, 0x41, 0x43, 0x45, 0x2A, 0x2A, ..]) => GetFormat(CompressionFormat.Ace),
            (true, [0x60, 0xEA, ..]) => GetFormat(CompressionFormat.Arj),
            (true, [0x4D, 0x53, 0x43, 0x46, ..]) => GetFormat(CompressionFormat.Cab),
            (true, [0x2D, 0x6C, 0x68, ..]) => GetFormat(CompressionFormat.Lzh),
            (true, [0x04, 0x22, 0x4D, 0x18, ..]) => GetFormat(CompressionFormat.Lz4),
            _ when IsTarHeader(stream) => GetFormat(CompressionFormat.Tar),
            _ => throw new NotSupportedException("Could not detect archive format from header")
        };
        Log.Debug("Header matched {EngineName} via magic bytes", engine.Name);
        return engine;
    }

    public static Archive Open(string path, string? password = null, AccessMode mode = AccessMode.Read)
    {
        using var stream = File.OpenRead(path);
        var format = GetFormatFromPathOrHeader(path, stream);
        SetPassword(format, password);
        return format.Open(path, stream, mode);
    }

    private static bool IsTarHeader(Stream stream)
    {
        var pos = stream.Position;
        try
        {
            stream.Seek(257, SeekOrigin.Begin);
            var magic = new byte[5];
            _ = stream.Read(magic, 0, 5);
            return magic[0] == 'u' && magic[1] == 's' && magic[2] == 't' && magic[3] == 'a' && magic[4] == 'r';
        }
        finally
        {
            stream.Seek(pos, SeekOrigin.Begin);
        }
    }

    public static async Task<Archive> OpenAsync(string path, string? password = null, AccessMode mode = AccessMode.Read, CancellationToken ct = default)
    {
        Log.Information("Opening archive {Path}", path);
        try
        {
            var stream = File.OpenRead(path);
            var format = GetFormatFromPathOrHeader(path, stream);
            SetPassword(format, password);
            return await format.OpenAsync(path, stream, mode, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open {Path}: {Message}", path, ex.Message);
            throw;
        }
    }

    private static IArchiveFormat GetFormatFromPathOrHeader(string path, Stream stream)
    {
        Log.Verbose("Detecting format for {Path}", path);
        var lower = Path.GetExtension(path).ToLowerInvariant();
        if (lower == ".gz" || lower == ".bz2" || lower == ".xz" || lower == ".zst")
        {
            var baseName = Path.GetFileNameWithoutExtension(path);
            if (baseName.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
            {
                var ext = $".tar{lower}";
                Log.Debug("Tar-with-compression detected: {Ext}", ext);
                return GetFormatFromExtension(ext);
            }
        }

        try
        {
            return GetFormatFromFileHeader(stream);
        }
        catch (NotSupportedException)
        {
            Log.Warning("No header match for {Path}, trying Hawkynt fallback", path);
            var fallback = new Formats.HawkyntFallbackEngine();
            if (fallback.FindDescriptor(path, stream) is not null)
                return fallback;
            throw;
        }
    }

    private static void SetPassword(IArchiveFormat format, string? password)
    {
        if (password == null) return;
        Log.Debug("Password set on {EngineName}", format.Name);
        switch (format)
        {
            case Formats.ZipEngine zip: zip.Password = password; break;
            case Formats.SevenZipEngine sz: sz.Password = password; break;
            case Formats.ZstdEngine zstd: zstd.Password = password; break;
            case Formats.RarEngine rar: rar.Password = password; break;
            case Formats.AceEngine ace: ace.Password = password; break;
            case Formats.ArjEngine arj: arj.Password = password; break;
            case Formats.CabEngine cab: cab.Password = password; break;
            case Formats.LzhEngine lzh: lzh.Password = password; break;
            case Formats.HawkyntFallbackEngine hf: hf.Password = password; break;
        }
    }
}
