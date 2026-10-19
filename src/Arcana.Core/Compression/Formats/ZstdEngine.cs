using Arcana.Core.Cryptography;
using Arcana.Core.Filesystem;
using Serilog;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace Arcana.Core.Compression.Formats;

public class ZstdEngine : IArchiveFormat
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ZstdEngine>();
    public string Name => "Zstandard";
    public string Extension => ".zst";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => false;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => false;
    public string? Password { get; set; }

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        var vfs = new VirtualFileSystem();
        try
        {
            var input = LegacyArchiveEncryption.Open(stream, Password, [0x28, 0xB5, 0x2F, 0xFD]);
            var legacy = !ReferenceEquals(input, stream);
            using var decompressor = new DecompressionStream(input);
            decompressor.SetParameter(ZSTD_dParameter.ZSTD_d_windowLogMax, 27);
            var name = Path.GetFileNameWithoutExtension(path);
            var node = await vfs.AddSpoolFileAsync(name, decompressor, ct).ConfigureAwait(false);
            Log.Information("Opened Zstandard stream {Path} ({Size} bytes)", path, node.OriginalSize);
            return new Archive { Format = CompressionFormat.Zstandard, FormatEngine = this, Vfs = vfs,
                IsEncrypted = legacy, Password = Password,
                Entries = [new ArchiveEntry { Path = name, Name = name, Size = node.OriginalSize,
                    CompressedSize = stream.Length, LastModified = DateTime.UtcNow }] };
        }
        catch (Exception ex) { vfs.Dispose(); Log.Error(ex, "Could not open Zstandard {Path}", path); throw; }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
        IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => SaveAsync(archive, stream, options, progress, ct).GetAwaiter().GetResult();

    public async Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
        IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        if (options.Encryption?.Password != null)
            throw new NotSupportedException("Zstandard has no native password protection. Use ZIP AES-256.");
        if (archive.Vfs.Root.Children.Count != 1 || archive.Vfs.Root.Children[0].Type != NodeType.File)
            throw new InvalidOperationException("Zstandard requires one file; use TAR + Zstandard for folders.");
        var node = archive.Vfs.Root.Children[0];
        await using var compressor = CreateCompressor(stream, options);
        await using var content = node.OpenRead();
        await content.CopyToAsync(compressor, ct).ConfigureAwait(false);
        progress?.Report(new ProgressReport { CurrentFile = node.Name, FilesProcessed = 1, TotalFiles = 1, CurrentOperation = "Compressing" });
        Log.Information("Saved Zstandard stream");
    }

    internal static CompressionStream CreateCompressor(Stream stream, CompressionOptions options)
    {
        var compressor = new CompressionStream(stream, Math.Clamp((int)options.Level, 1, 19), leaveOpen: true);
        compressor.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);
        if (options.EnableParallel)
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Math.Clamp(options.ThreadCount, 1, 16));
        Log.Debug("Managed Zstandard compressor created (parallel {Parallel})", options.EnableParallel);
        return compressor;
    }
}
