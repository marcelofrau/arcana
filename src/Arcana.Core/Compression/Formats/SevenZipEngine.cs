using Arcana.Core.Cryptography;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class SevenZipEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<SevenZipEngine>();
    public string Name => "7z";
    public string Extension => ".7z";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => false;
    public bool SupportsSolid => true;
    public bool SupportsVolumes => true;
    public string? Password { get; set; }

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default,
                                         IProgress<ProgressReport>? progress = null)
    {
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        var vfs = new Filesystem.VirtualFileSystem();
        try
        {
            var readStream = LegacyArchiveEncryption.Open(stream, Password, [0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C]);

            _log.Debug("Detected 7z format for {Path}", path);
            var nativeFile = stream is FileStream && ReferenceEquals(readStream, stream);
            var readerOptions = new ReaderOptions { Password = Password, LeaveStreamOpen = !nativeFile };
            var sevenZip = nativeFile
                ? (SharpCompress.Archives.IArchive)SevenZipArchive.OpenArchive(new FileInfo(path), readerOptions)
                : OpenForReading(readStream, readerOptions);
            vfs.Own(sevenZip);
            if (!ReferenceEquals(readStream, stream)) vfs.Own(readStream);
            var entries = new List<ArchiveEntry>();
            var gate = new SemaphoreSlim(1, 1);
            var entryCount = 0;
            var throttle = new ProgressThrottle();

            // Report initial indeterminate progress
            progress?.Report(new ProgressReport
            {
                CurrentFile = Path.GetFileName(path),
                FilesProcessed = 0,
                TotalFiles = 0,
                CurrentOperation = "Reading archive",
            });

            foreach (var entry in sevenZip.Entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = entry.Key ?? "";
                if (entry.IsEncrypted && Password == null)
                    throw new SharpCompress.Common.CryptographicException("Password required.");

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
                }
                else
                {
                    var node = vfs.AddLazyFile(key, () =>
                    {
                        gate.Wait();
                        try { return new Filesystem.ArchiveEntryStream(entry.OpenEntryStream(), entry.Size, gate); }
                        catch (Exception ex) { gate.Release(); _log.Warning(ex, "Could not open 7z entry {Path}", key); throw; }
                    });
                    node.OriginalSize = entry.Size;
                    node.Crc32 = entry.IsEncrypted && entry.Crc == 0 ? null : (uint)entry.Crc;
                    node.LastModified = NormalizeTimestamp(entry.LastModifiedTime);

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

                entryCount++;
                if (throttle.ShouldReport(entryCount))
                {
                    progress?.Report(new ProgressReport
                    {
                        CurrentFile = entry.Key ?? "",
                        FilesProcessed = entryCount,
                        TotalFiles = 0, // Unknown total
                        CurrentOperation = "Reading archive",
                    });
                }
            }

            // Final progress
            progress?.Report(new ProgressReport
            {
                CurrentFile = "",
                FilesProcessed = entryCount,
                TotalFiles = entryCount,
                CurrentOperation = "Reading archive",
            });

            _log.Information("Opened {Path} with {EntryCount} entries", path, entries.Count);
            return new Archive
            {
                Format = CompressionFormat.SevenZip,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
                IsEncrypted = sevenZip.IsEncrypted || !ReferenceEquals(readStream, stream),
                Password = Password,
                SourcePaths = sevenZip.Volumes.Select(v => v.FileName).OfType<string>().Select(Path.GetFullPath).ToArray(),
            };
        }
        catch (Exception ex)
        {
            vfs.Dispose();
            _log.Error(ex, "Failed to open 7z archive {Path}", path);
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
            var dictSize = 1 << Math.Clamp((int)options.Level + 20, 20, 27); // 1MB to 128MB
            var fastBytes = Math.Clamp((int)options.Level * 4 + 32, 32, 273);
            var lzmaProps = new LzmaEncoderProperties(false, dictSize, fastBytes);
            
            var writerOptions = new SevenZipWriterOptions(CompressionType.LZMA2)
            {
                LeaveStreamOpen = true,
                LzmaProperties = lzmaProps,
            };

            if (options.Encryption?.Password != null)
            {
                throw new NotSupportedException("Encrypted 7z writing is not supported by the managed backend. Use ZIP AES-256.");
            }
            else
            {
                _log.Debug("No encryption for 7z save, using LZMA2");
                using var writer = WriterFactory.OpenWriter(stream, ArchiveType.SevenZip, writerOptions);
                await WriteNodesAsync(writer, archive.Vfs.Root, progress, ct).ConfigureAwait(false);
            }

            _log.Information("Saved 7z archive to stream");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save 7z archive");
            throw;
        }
    }

    private static SharpCompress.Archives.IArchive OpenForReading(Stream stream, ReaderOptions options)
    {
        return (SharpCompress.Archives.IArchive)(object)SevenZipArchive.OpenArchive(stream, options);
    }

    private static async Task WriteNodesAsync(IWriter writer, Filesystem.ArchiveNode node,
        IProgress<ProgressReport>? progress, CancellationToken ct)
    {
        var nodes = Filesystem.VirtualFileSystem.Walk(node).Skip(1).ToList();
        var totalFiles = nodes.Count(n => n.Type == Filesystem.NodeType.File);
        var filesDone = 0;

        foreach (var child in nodes)
        {
            ct.ThrowIfCancellationRequested();
            var entryPath = child.FullPath.TrimStart('/');

            if (child.Type == Filesystem.NodeType.Directory)
            {
                writer.WriteDirectory(entryPath, child.LastModified);
            }
            else
            {
                await using var content = new ProgressStream(child.OpenRead(), progress, child.Name, child.OriginalSize,
                    filesDone, totalFiles, "Compressing", cancellationToken: ct);
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

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
