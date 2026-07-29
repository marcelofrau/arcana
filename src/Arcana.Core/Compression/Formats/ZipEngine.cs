using Arcana.Core.Cryptography;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class ZipEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<ZipEngine>();
    public string Name => "ZIP";
    public string Extension => ".zip";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => true;
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
                _log.Debug("Decrypting ZIP archive with password");
                var encOpts = new EncryptionOptions { Password = Password };
                var provider = new EncryptionProvider(encOpts);
                readStream = provider.CreateDecryptingStream(stream);
            }

            _log.Debug("Detected ZIP format for {Path}", path);
            var readerOptions = new ReaderOptions();

            var zip = OpenForReading(readStream, readerOptions);
            var entries = new List<ArchiveEntry>();
            var vfs = new Filesystem.VirtualFileSystem();

            foreach (var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = entry.Key ?? "";

                if (entry.IsDirectory)
                {
                    vfs.AddDirectory(key);
                    entries.Add(new ArchiveEntry
                    {
                        Path = key,
                        Name = key.Trim('/').Split('/').Last(),
                        Size = entry.Size,
                        CompressedSize = entry.CompressedSize,
                        IsDirectory = true,
                        LastModified = NormalizeTimestamp(entry.LastModifiedTime),
                    });
                    continue;
                }

                await using var entryStream = entry.OpenEntryStream();
                var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms, ct).ConfigureAwait(false);
                ms.Position = 0;

                vfs.AddFile(key, ms);

                entries.Add(new ArchiveEntry
                {
                    Path = key,
                    Name = System.IO.Path.GetFileName(key),
                    Size = entry.Size,
                    CompressedSize = entry.CompressedSize,
                    Crc32 = (uint)entry.Crc,
                    IsEncrypted = entry.IsEncrypted,
                    IsDirectory = false,
                    LastModified = NormalizeTimestamp(entry.LastModifiedTime),
                });
            }

            _log.Information("Opened {Path} with {EntryCount} entries", path, entries.Count);
            return new Archive
            {
                Format = CompressionFormat.Zip,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
                IsEncrypted = zip.IsEncrypted,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open ZIP archive {Path}", path);
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
            var level = MapLevel(options.Level);
            _log.Debug("ZIP compression level mapped to {Level}", level);
            var writerOptions = new WriterOptions(CompressionType.Deflate, level)
            {
                LeaveStreamOpen = true,
            };

            Stream writeStream = stream;
            var encryptOnDispose = false;

            if (options.Encryption?.Password != null)
            {
                _log.Debug("Encryption enabled for ZIP save, algorithm {Algorithm}", options.Encryption.Algorithm);
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

            {
                using var writer = WriterFactory.OpenWriter(writeStream, ArchiveType.Zip, writerOptions);
                await WriteNodesAsync(writer, archive.Vfs.Root, progress, ct).ConfigureAwait(false);
            }
            if (encryptOnDispose)
                writeStream.Dispose();

            _log.Information("Saved ZIP archive to stream");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save ZIP archive");
            throw;
        }
    }

    private static IArchive OpenForReading(Stream stream, ReaderOptions options)
    {
        return (IArchive)(object)ZipArchive.OpenArchive(stream, options);
    }

    private static async Task WriteNodesAsync(IWriter writer, Filesystem.ArchiveNode node,
        IProgress<ProgressReport>? progress, CancellationToken ct)
    {
        var totalFiles = CountFiles(node);
        var filesDone = 0;

        foreach (var child in node.Children)
        {
            ct.ThrowIfCancellationRequested();
            var entryPath = child.FullPath.TrimStart('/');

            if (child.Type == Filesystem.NodeType.Directory)
            {
                writer.WriteDirectory(entryPath, child.LastModified);
                await WriteNodesAsync(writer, child, progress, ct).ConfigureAwait(false);
            }
            else
            {
                await using var content = child.OpenRead();
                writer.Write(entryPath, content, child.LastModified);

                filesDone++;
                progress?.Report(new ProgressReport
                {
                    CurrentFile = child.Name,
                    FilesProcessed = filesDone,
                    TotalFiles = totalFiles,
                    CurrentOperation = "Compressing",
                });
            }
        }
    }

    private static int CountFiles(Filesystem.ArchiveNode node)
    {
        var count = 0;
        foreach (var child in node.Children)
            count += child.Type == Filesystem.NodeType.File ? 1 : CountFiles(child);
        return count;
    }

    private static int MapLevel(CompressionLevel level) => level switch
    {
        CompressionLevel.Store => 0,
        CompressionLevel.Fastest => 1,
        CompressionLevel.Fast => 3,
        CompressionLevel.Normal => 5,
        CompressionLevel.Maximum => 7,
        CompressionLevel.Ultra => 9,
        CompressionLevel.Insane => 9,
        _ => 5,
    };

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
