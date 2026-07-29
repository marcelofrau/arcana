using Arcana.Core.Compression;
using Serilog;

namespace Arcana.Core.Filesystem;

public class VirtualFileSystem : IDisposable
{
    private readonly ILogger _log = Serilog.Log.ForContext<VirtualFileSystem>();

    public VirtualFileSystem()
    {
        _log.Debug("VFS created with {NodeCount} nodes", Root.Children.Count);
    }

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
        _log.Verbose("AddFile {Path}", path);
        var parent = EnsureDirectory(Path.GetDirectoryName(path) ?? "");
        var name = Path.GetFileName(path);
        var node = new ArchiveNode
        {
            Name = name,
            FullPath = NormalizePath(path),
            Type = NodeType.File,
            Parent = parent,
            LastModified = DateTime.UtcNow,
            ContentFactory = () => content
        };
        parent.Children.Add(node);
        return node;
    }

    public ArchiveNode AddDirectory(string path)
    {
        _log.Verbose("AddDirectory {Path}", path);
        return EnsureDirectory(NormalizePath(path));
    }

    public void DeleteNode(ArchiveNode node)
    {
        node.Parent?.Children.Remove(node);
        node.Parent = null;
    }

    public void RenameNode(ArchiveNode node, string newName)
    {
        node.Name = newName;
        node.FullPath = NormalizePath(
            node.Parent != null
                ? $"{node.Parent.FullPath}/{newName}"
                : newName
        );
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
        _log.Verbose("FindNode {Path}", path);
        var normalized = NormalizePath(path).TrimStart('/');
        if (string.IsNullOrEmpty(normalized))
            return Root;

        var parts = normalized.Split('/');
        var current = Root;
        foreach (var part in parts)
        {
            current = current.Children.Find(c =>
                c.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (current == null) return null;
        }
        return current;
    }

    public void ImportFromArchive(Archive archive)
    {
        foreach (var entry in archive.Entries)
            AddFile(entry.Path, new MemoryStream());
    }

    private ArchiveNode EnsureDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
            return Root;

        var normalized = NormalizePath(path).TrimStart('/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = Root;

        foreach (var part in parts)
        {
            var child = current.Children.Find(c =>
                c.Name.Equals(part, StringComparison.OrdinalIgnoreCase) &&
                c.Type == NodeType.Directory);

            if (child == null)
            {
                child = new ArchiveNode
                {
                    Name = part,
                    FullPath = $"{current.FullPath}/{part}",
                    Type = NodeType.Directory,
                    Parent = current,
                    LastModified = DateTime.UtcNow,
                };
                current.Children.Add(child);
            }
            current = child;
        }
        return current;
    }

    private static string NormalizePath(string path) =>
        "/" + path.Replace('\\', '/').Trim('/');

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
