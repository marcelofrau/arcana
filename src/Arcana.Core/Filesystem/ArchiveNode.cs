namespace Arcana.Core.Filesystem;

public class ArchiveNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public NodeType Type { get; init; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public bool IsDirty { get; set; }
    public ArchiveNode? Parent { get; set; }
    public List<ArchiveNode> Children { get; set; } = new();
    public DateTime LastModified { get; set; }

    public IEnumerable<ArchiveNode> ChildFolders =>
        Children.Where(c => c.Type == NodeType.Directory);

    public Func<Stream>? ContentFactory { get; set; }

    public Stream OpenRead()
    {
        var stream = ContentFactory?.Invoke()
            ?? throw new InvalidOperationException($"No content available for '{FullPath}'");
        if (stream.CanSeek)
            stream.Position = 0;
        return stream;
    }

    public Stream OpenWrite()
    {
        var ms = new MemoryStream();
        ContentFactory = () =>
        {
            ms.Position = 0;
            return ms;
        };
        return ms;
    }
}
