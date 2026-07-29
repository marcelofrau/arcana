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

    public Stream OpenRead()
    {
        throw new NotImplementedException("ArchiveNode.OpenRead");
    }

    public Stream OpenWrite()
    {
        throw new NotImplementedException("ArchiveNode.OpenWrite");
    }
}
