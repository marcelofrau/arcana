using Arcana.Core.Cryptography;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class BrotliEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<BrotliEngine>();
    public string Name => "Brotli";
    public string Extension => ".br";
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
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        try
        {
            Stream readStream = stream;
            if (Password != null)
            {
                _log.Warning("Password set on Brotli archive which does not support native encryption");
                var provider = new EncryptionProvider(new EncryptionOptions { Password = Password });
                readStream = provider.CreateDecryptingStream(stream);
            }

            _log.Debug("Decompressing Brotli data from {Path}", path);
            using var decompressor = new System.IO.Compression.BrotliStream(readStream, System.IO.Compression.CompressionMode.Decompress);
            using var ms = new MemoryStream();
            await decompressor.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Position = 0;

            var fileName = System.IO.Path.GetFileNameWithoutExtension(path) ?? "unknown";
            var vfs = new Filesystem.VirtualFileSystem();
            vfs.AddFile(fileName, ms);

            var entries = new List<ArchiveEntry>
            {
                new()
                {
                    Path = fileName,
                    Name = fileName,
                    Size = ms.Length,
                    CompressedSize = stream.Length,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow,
                }
            };

            _log.Information("Opened {Path} with 1 entry, size {Size} bytes", path, ms.Length);
            return new Archive
            {
                Format = CompressionFormat.Brotli,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open Brotli archive {Path}", path);
            throw;
        }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => SaveAsync(archive, stream, options, progress, ct).GetAwaiter().GetResult();

    public async Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                                IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        _log.Verbose("Saving archive with {FileCount} files, level {Level}", archive.Vfs.Root.Children.Count, options.Level);
        try
        {
            Stream writeStream = stream;
            var encryptOnDispose = false;

            if (options.Encryption?.Password != null)
            {
                _log.Debug("Encryption enabled for Brotli save, algorithm {Algorithm}", options.Encryption.Algorithm);
                var encOpts = new EncryptionOptions
                {
                    Password = options.Encryption.Password,
                    Algorithm = options.Encryption.Algorithm,
                    Kdf = options.Encryption.Kdf,
                };
                var provider = new EncryptionProvider(encOpts);
                writeStream = provider.CreateEncryptingStream(stream);
                encryptOnDispose = true;
            }

            var level = MapLevel(options.Level);
            _log.Debug("Brotli compression level set to {Level}", level);

            foreach (var node in archive.Vfs.Root.Children)
            {
                ct.ThrowIfCancellationRequested();
                if (node.Type is not Filesystem.NodeType.File) continue;

                await using var content = node.OpenRead();
                using var compressor = new System.IO.Compression.BrotliStream(writeStream, level, true);
                await content.CopyToAsync(compressor, ct).ConfigureAwait(false);

                progress?.Report(new ProgressReport
                {
                    CurrentFile = node.Name,
                    FilesProcessed = 1,
                    TotalFiles = 1,
                    CurrentOperation = "Compressing",
                });
            }

            if (encryptOnDispose)
                writeStream.Dispose();

            _log.Information("Saved Brotli archive to stream");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save Brotli archive");
            throw;
        }
    }

    private static System.IO.Compression.CompressionLevel MapLevel(CompressionLevel level) => level switch
    {
        CompressionLevel.Store => System.IO.Compression.CompressionLevel.NoCompression,
        CompressionLevel.Fastest => System.IO.Compression.CompressionLevel.Fastest,
        CompressionLevel.Fast => System.IO.Compression.CompressionLevel.Fastest,
        CompressionLevel.Normal => System.IO.Compression.CompressionLevel.Optimal,
        CompressionLevel.Maximum => System.IO.Compression.CompressionLevel.SmallestSize,
        CompressionLevel.Ultra => System.IO.Compression.CompressionLevel.SmallestSize,
        CompressionLevel.Insane => System.IO.Compression.CompressionLevel.SmallestSize,
        _ => System.IO.Compression.CompressionLevel.Optimal,
    };
}
