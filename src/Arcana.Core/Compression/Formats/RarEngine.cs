using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Readers;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class RarEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<RarEngine>();
    public string Name => "RAR";
    public string Extension => ".rar";
    public bool CanRead => true;
    public bool CanWrite => false;
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
            var readerOptions = new ReaderOptions { LookForHeader = true, LeaveStreamOpen = stream is not FileStream };
            if (Password != null)
            {
                readerOptions.Password = Password;
                _log.Debug("Password set for RAR archive");
            }

            var rar = stream is FileStream
                ? (IArchive)RarArchive.OpenArchive(new FileInfo(path), readerOptions)
                : (IArchive)RarArchive.OpenArchive(stream, readerOptions);
            vfs.Own(rar);
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

            foreach (var entry in rar.Entries)
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
                        catch (Exception ex) { gate.Release(); _log.Warning(ex, "Could not open RAR entry {Path}", key); throw; }
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
                Format = CompressionFormat.Rar,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
                IsEncrypted = rar.IsEncrypted,
                Password = Password,
                SourcePaths = rar.Volumes.Select(v => v.FileName).OfType<string>().Select(Path.GetFullPath).ToArray(),
            };
        }
        catch (Exception ex)
        {
            vfs.Dispose();
            _log.Error(ex, "Failed to open RAR archive {Path}", path);
            throw;
        }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("RAR writing is not supported");

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("RAR writing is not supported");

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
