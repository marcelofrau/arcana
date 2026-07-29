using Arcana.Core.Compression;

namespace Arcana.Core.Filesystem;

public class VirtualFileSystem : IDisposable
{
    public ArchiveNode Root { get; } = new()
    {
        Name = "",
        FullPath = "",
        Type = NodeType.Directory,
        LastModified = DateTime.UtcNow
    };

    public bool HasDirtyNodes => GetDirtyNodes().Count > 0;

    public ArchiveNode AddFile(string path, Stream content)
    {
        throw new NotImplementedException("VirtualFileSystem.AddFile");
    }

    public ArchiveNode AddDirectory(string path)
    {
        throw new NotImplementedException("VirtualFileSystem.AddDirectory");
    }

    public void DeleteNode(ArchiveNode node)
    {
        throw new NotImplementedException("VirtualFileSystem.DeleteNode");
    }

    public void RenameNode(ArchiveNode node, string newName)
    {
        throw new NotImplementedException("VirtualFileSystem.RenameNode");
    }

    public IReadOnlyList<ArchiveNode> GetDirtyNodes()
    {
        var dirty = new List<ArchiveNode>();
        CollectDirty(Root, dirty);
        return dirty;
    }

    public void MarkAllClean()
    {
        MarkClean(Root);
    }

    public ArchiveNode? FindNode(string path)
    {
        throw new NotImplementedException("VirtualFileSystem.FindNode");
    }

    public void ImportFromArchive(Archive archive)
    {
        throw new NotImplementedException("VirtualFileSystem.ImportFromArchive");
    }

    private static void CollectDirty(ArchiveNode node, List<ArchiveNode> dirty)
    {
        if (node.IsDirty)
            dirty.Add(node);
        foreach (var child in node.Children)
            CollectDirty(child, dirty);
    }

    private static void MarkClean(ArchiveNode node)
    {
        node.IsDirty = false;
        foreach (var child in node.Children)
            MarkClean(child);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
