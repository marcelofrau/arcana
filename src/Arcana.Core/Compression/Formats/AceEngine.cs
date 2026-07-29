using Compression.Registry;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class AceEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<AceEngine>();
    public string Name => "ACE";
    public string Extension => ".ace";
    public bool CanRead => true;
    public bool CanWrite => false;
    public bool CanEncrypt => true;
    public bool SupportsSolid => true;
    public bool SupportsVolumes => true;
    public string? Password { get; set; }

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        try
        {
            HawkyntInit.Ensure();

            var descriptor = FormatRegistry.GetByExtension(path);
            var descriptorId = descriptor?.Id ?? "Ace";
            _log.Debug("Detected format via descriptor {DescriptorId}", descriptorId);
            var ops = FormatRegistry.GetArchiveOps(descriptorId);
            var entries = ops.List(stream, Password);

            var result = new List<ArchiveEntry>();
            var vfs = new Filesystem.VirtualFileSystem();

            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = entry.Name.Replace('\\', '/');

                if (entry.IsDirectory)
                {
                    vfs.AddDirectory(key);
                    result.Add(new ArchiveEntry
                    {
                        Path = key,
                        Name = key.Trim('/').Split('/').Last(),
                        Size = entry.OriginalSize,
                        CompressedSize = entry.CompressedSize,
                        IsDirectory = true,
                        LastModified = NormalizeTimestamp(entry.LastModified),
                    });
                    continue;
                }

                var data = ops.ExtractEntryToMemory(stream, entry.Name, Password);
                vfs.AddFile(key, new MemoryStream(data));

                result.Add(new ArchiveEntry
                {
                    Path = key,
                    Name = System.IO.Path.GetFileName(key),
                    Size = entry.OriginalSize,
                    CompressedSize = entry.CompressedSize,
                    IsEncrypted = entry.IsEncrypted,
                    IsDirectory = false,
                    LastModified = NormalizeTimestamp(entry.LastModified),
                });
            }

            stream.Position = 0;

            _log.Information("Opened {Path} with {EntryCount} entries", path, result.Count);
            return new Archive
            {
                Format = CompressionFormat.Ace,
                FormatEngine = this,
                Entries = result,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open ACE archive {Path}", path);
            throw;
        }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("ACE writing is not supported");

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("ACE writing is not supported");

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
