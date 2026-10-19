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

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
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

            using var rar = stream is FileStream
                ? (IArchive)RarArchive.OpenArchive(new FileInfo(path), readerOptions)
                : (IArchive)RarArchive.OpenArchive(stream, readerOptions);
            var entries = new List<ArchiveEntry>();
            if (rar.IsSolid)
            {
                using var reader = rar.ExtractAllEntries();
                while (reader.MoveToNextEntry())
                    await AddEntryAsync(vfs, entries, reader.Entry, reader.OpenEntryStream, ct).ConfigureAwait(false);
            }
            else
            {
                foreach (var entry in rar.Entries)
                    await AddEntryAsync(vfs, entries, entry, entry.OpenEntryStream, ct).ConfigureAwait(false);
            }

            _log.Information("Opened {Path} with {EntryCount} entries", path, entries.Count);
            return new Archive
            {
                Format = CompressionFormat.Rar,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
                IsEncrypted = entries.Any(e => e.IsEncrypted),
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

    private static async Task AddEntryAsync(Filesystem.VirtualFileSystem vfs, List<ArchiveEntry> entries,
                                            IEntry entry, Func<Stream> openEntryStream, CancellationToken ct)
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
            return;
        }

        await using var entryStream = openEntryStream();
        await vfs.AddSpoolFileAsync(key, entryStream, ct).ConfigureAwait(false);

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

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
