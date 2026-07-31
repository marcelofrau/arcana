using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.Core.Compression;

public class Archive : IDisposable
{
    private static readonly ILogger Log = Serilog.Log.ForContext<Archive>();

    public CompressionFormat Format { get; init; }
    public IArchiveFormat FormatEngine { get; init; } = null!;
    public IReadOnlyList<ArchiveEntry> Entries { get; init; } = Array.Empty<ArchiveEntry>();
    public VirtualFileSystem Vfs { get; init; } = new();
    public bool IsEncrypted { get; init; }
    public string? Comment { get; set; }

    public ArchiveEntry? FindEntry(string path)
    {
        Log.Debug("FindEntry: {Path}", path);
        return Entries.FirstOrDefault(e =>
            e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<ArchiveEntry> FindEntries(string pattern)
    {
        Log.Debug("FindEntries: {Pattern}", pattern);
        return Entries.Where(e =>
            e.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    public void SyncNodeMetadata()
    {
        Log.Debug("SyncNodeMetadata: {Count} entries", Entries.Count);
        foreach (var entry in Entries)
        {
            var node = Vfs.FindNode(entry.Path);
            if (node == null)
                continue;
            node.OriginalSize = entry.Size;
            node.CompressedSize = entry.CompressedSize;
            node.LastModified = entry.LastModified;
        }
    }

    public void Dispose()
    {
        Log.Debug("Archive disposed ({Format})", Format);
        Vfs.Dispose();
        GC.SuppressFinalize(this);
    }
}
