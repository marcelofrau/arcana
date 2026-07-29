using System.Text;
using Arcana.Core.Cryptography;
using SharpCompress.Compressors;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class GzipEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<GzipEngine>();
    public string Name => "GZip";
    public string Extension => ".gz";
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
                _log.Warning("Password set on GZip archive which does not support native encryption");
                var provider = new EncryptionProvider(new EncryptionOptions { Password = Password });
                readStream = provider.CreateDecryptingStream(stream);
            }

            _log.Debug("Decompressing GZip data from {Path}", path);
            using var decompressor = new SharpCompress.Compressors.Deflate.GZipStream(readStream, CompressionMode.Decompress);
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
                Format = CompressionFormat.GZip,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open GZip archive {Path}", path);
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
                _log.Debug("Encryption enabled for GZip save, algorithm {Algorithm}", options.Encryption.Algorithm);
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

            foreach (var node in archive.Vfs.Root.Children)
            {
                ct.ThrowIfCancellationRequested();
                if (node.Type is not Filesystem.NodeType.File) continue;

                await using var content = node.OpenRead();
                using var compressor = new SharpCompress.Compressors.Deflate.GZipStream(
                    writeStream, CompressionMode.Compress,
                    SharpCompress.Compressors.Deflate.CompressionLevel.Default,
                    Encoding.Default, true);
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

            _log.Information("Saved GZip archive to stream");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save GZip archive");
            throw;
        }
    }
}
