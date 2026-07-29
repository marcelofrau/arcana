using Arcana.Core.Filesystem;

namespace Arcana.Core.Compression;

public class Archive : IDisposable
{
    public CompressionFormat Format { get; init; }
    public IArchiveFormat FormatEngine { get; init; } = null!;
    public IReadOnlyList<ArchiveEntry> Entries { get; init; } = Array.Empty<ArchiveEntry>();
    public VirtualFileSystem Vfs { get; init; } = new();
    public bool IsEncrypted { get; init; }
    public string? Comment { get; set; }

    public ArchiveEntry? FindEntry(string path)
        => Entries.FirstOrDefault(e =>
            e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<ArchiveEntry> FindEntries(string pattern)
        => Entries.Where(e =>
            e.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase));

    public void Dispose()
    {
        Vfs.Dispose();
        GC.SuppressFinalize(this);
    }
}
