using SharpCompress.Common;
using SharpCompress.Readers;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class ArjEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<ArjEngine>();
    public string Name => "ARJ";
    public string Extension => ".arj";
    public bool CanRead => true;
    public bool CanWrite => false;
    public bool CanEncrypt => false;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => true;
    public string? Password { get; set; }

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default,
                                         IProgress<ProgressReport>? progress = null)
    {
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        try
        {
            var readerOptions = new ReaderOptions();
            if (Password != null)
            {
                readerOptions.Password = Password;
                _log.Debug("Password set for ARJ archive");
            }

            var reader = SharpCompress.Readers.Arj.ArjReader.OpenReader(stream, readerOptions);
            var entries = new List<ArchiveEntry>();
            var vfs = new Filesystem.VirtualFileSystem();
            var gate = new SemaphoreSlim(1, 1);
            vfs.Own(reader);
            var entryCount = 0;
            var throttle = new ProgressThrottle();

            while (reader.MoveToNextEntry())
            {
                ct.ThrowIfCancellationRequested();
                var entry = reader.Entry;
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
                        try { return new Filesystem.ArchiveEntryStream(reader.OpenEntryStream(), entry.Size, gate); }
                        catch (Exception ex) { gate.Release(); _log.Warning(ex, "Could not open ARJ entry {Path}", key); throw; }
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
                        TotalFiles = 0,
                        CurrentOperation = "Reading archive",
                    });
                }
            }

            _log.Information("Opened {Path} with {EntryCount} entries", path, entries.Count);
            return new Archive
            {
                Format = CompressionFormat.Arj,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open ARJ archive {Path}", path);
            throw;
        }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("ARJ writing is not supported");

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("ARJ writing is not supported");

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
