using Arcana.Core.Cryptography;
using K4os.Compression.LZ4;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class Lz4Engine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<Lz4Engine>();
    public string Name => "LZ4";
    public string Extension => ".lz4";
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
                _log.Warning("Password set on LZ4 archive which does not support native encryption");
                var encOpts = new EncryptionOptions { Password = Password };
                var provider = new EncryptionProvider(encOpts);
                readStream = provider.CreateDecryptingStream(stream);
            }

            _log.Debug("Decompressing LZ4 data from {Path}", path);
            using var ms = new MemoryStream();
            await readStream.CopyToAsync(ms, ct).ConfigureAwait(false);
            var compressed = ms.ToArray();
            var data = LZ4Pickler.Unpickle(compressed);

            var fileName = System.IO.Path.GetFileNameWithoutExtension(path) ?? "unknown";
            var vfs = new Filesystem.VirtualFileSystem();
            vfs.AddFile(fileName, new MemoryStream(data));

            var entries = new List<ArchiveEntry>
            {
                new()
                {
                    Path = fileName,
                    Name = fileName,
                    Size = data.Length,
                    CompressedSize = compressed.Length,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow,
                }
            };

            _log.Information("Opened {Path} with 1 entry, size {Size} bytes", path, data.Length);
            return new Archive
            {
                Format = CompressionFormat.Lz4,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open LZ4 archive {Path}", path);
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
                _log.Debug("Encryption enabled for LZ4 save, algorithm {Algorithm}", options.Encryption.Algorithm);
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
            _log.Debug("LZ4 compression level set to {Level}", level);

            foreach (var node in archive.Vfs.Root.Children)
            {
                ct.ThrowIfCancellationRequested();
                if (node.Type is not Filesystem.NodeType.File) continue;

                await using var content = node.OpenRead();
                using var ms = new MemoryStream();
                await content.CopyToAsync(ms, ct).ConfigureAwait(false);
                var raw = ms.ToArray();
                var compressed = LZ4Pickler.Pickle(raw, level);
                await writeStream.WriteAsync(compressed, ct).ConfigureAwait(false);

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

            _log.Information("Saved LZ4 archive to stream");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save LZ4 archive");
            throw;
        }
    }

    private static LZ4Level MapLevel(CompressionLevel level) => level switch
    {
        Core.Compression.CompressionLevel.Store => LZ4Level.L00_FAST,
        Core.Compression.CompressionLevel.Fastest => LZ4Level.L00_FAST,
        Core.Compression.CompressionLevel.Fast => LZ4Level.L03_HC,
        Core.Compression.CompressionLevel.Normal => LZ4Level.L07_HC,
        Core.Compression.CompressionLevel.Maximum => LZ4Level.L09_HC,
        Core.Compression.CompressionLevel.Ultra => LZ4Level.L12_MAX,
        Core.Compression.CompressionLevel.Insane => LZ4Level.L12_MAX,
        _ => LZ4Level.L07_HC,
    };
}
