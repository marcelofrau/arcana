using Arcana.Core.Compression;
using Serilog;

namespace Arcana.Core.Filesystem;

public class VirtualFileSystem : IDisposable
{
    private readonly ILogger _log = Serilog.Log.ForContext<VirtualFileSystem>();
    private readonly List<IDisposable> _resources = [];
    private readonly Dictionary<string, ArchiveNode> _index = new(StringComparer.Ordinal);
    private bool _disposed;
    private long _spooledBytes;
    private FileStream? _spool;
    private int _nodeCount;

    public VirtualFileSystem()
    {
        _index["/"] = Root;
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
        var node = AddLazyFile(path, () => content, ownsContent: false);
        node.OriginalSize = content.CanSeek ? content.Length : 0;
        _resources.Add(content);
        return node;
    }

    public void Own(IDisposable resource)
    {
        _log.Verbose("Owning archive resource {Type}", resource.GetType().Name);
        _resources.Add(resource);
    }

    public async Task<ArchiveNode> AddSpoolFileAsync(string path, Stream source, CancellationToken ct)
    {
        if (_spool == null)
        {
            _spool = ArchiveTemporaryFile.Create();
            Own(_spool);
        }
        var temporary = _spool;
        var offset = temporary.Length;
        temporary.Position = offset;
        try
        {
            var length = await ArchiveContent.CopyBoundedAsync(source, temporary,
                ArchiveLimits.MaxExpandedBytes - _spooledBytes, ct).ConfigureAwait(false);
            await temporary.FlushAsync(ct).ConfigureAwait(false);
            var node = AddLazyFile(path, () => new FileSliceStream(temporary.Name, offset, length, FileShare.ReadWrite | FileShare.Delete));
            node.OriginalSize = length;
            _spooledBytes += length;
            return node;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Could not spool entry {Path}", path);
            temporary.SetLength(offset);
            throw;
        }
    }

    public ArchiveNode AddLazyFile(string path, Func<Stream> contentFactory, bool ownsContent = true)
    {
        _log.Verbose("AddLazyFile {Path}", path);
        ObjectDisposedException.ThrowIf(_disposed, this);
        path = NormalizePath(path).Trim('/');
        if (path.Split('/').Length > ArchiveLimits.MaxPathDepth)
            throw new InvalidDataException("Archive path exceeds the nesting limit.");
        var slash = path.LastIndexOf('/');
        var parent = EnsureDirectory(slash < 0 ? "" : path[..slash]);
        var name = slash < 0 ? path : path[(slash + 1)..];
        if (string.IsNullOrEmpty(name) || _index.ContainsKey(NormalizePath(path)))
            throw new InvalidDataException($"Duplicate archive entry: {path}");
        var node = new ArchiveNode
        {
            Name = name,
            FullPath = NormalizePath(path),
            Type = NodeType.File,
            Parent = parent,
            LastModified = DateTime.UtcNow,
            ContentFactory = contentFactory,
            OwnsContent = ownsContent,
        };
        parent.Children.Add(node);
        _index.Add(node.FullPath, node);
        if (++_nodeCount > ArchiveLimits.MaxEntries) throw new InvalidDataException("Archive contains too many entries.");
        Root.IsDirty = true;
        return node;
    }

    public ArchiveNode AddDirectory(string path)
    {
        _log.Verbose("AddDirectory {Path}", path);
        return EnsureDirectory(NormalizePath(path));
    }

    public void DeleteNode(ArchiveNode node)
    {
        if (node.Parent == null) throw new InvalidOperationException("Cannot delete the archive root.");
        _log.Information("Removing {Path}", node.FullPath);
        foreach (var removed in Walk(node)) _index.Remove(removed.FullPath);
        node.Parent?.Children.Remove(node);
        node.Parent = null;
        Root.IsDirty = true;
    }

    public void RenameNode(ArchiveNode node, string newName)
    {
        if (node.Parent == null) throw new InvalidOperationException("Cannot rename the archive root.");
        if (string.IsNullOrWhiteSpace(newName) || newName is "." or ".." || newName.IndexOfAny(['/', '\\']) >= 0)
            throw new ArgumentException("Enter a file name, not a path.", nameof(newName));
        if (node.Parent.Children.Any(c => c != node && c.Name.Equals(newName, StringComparison.Ordinal)))
            throw new IOException($"An entry named '{newName}' already exists.");
        _log.Information("Renaming {Path} to {Name}", node.FullPath, newName);
        foreach (var renamed in Walk(node)) _index.Remove(renamed.FullPath);
        node.Name = newName;
        UpdatePaths(node);
        Root.IsDirty = true;
    }

    private void UpdatePaths(ArchiveNode node)
    {
        node.FullPath = NormalizePath($"{node.Parent?.FullPath}/{node.Name}");
        _index.Add(node.FullPath, node);
        node.IsDirty = true;
        foreach (var child in node.Children) UpdatePaths(child);
    }

    public IEnumerable<ArchiveNode> EnumerateNodes() => Walk(Root).Skip(1);

    internal static IEnumerable<ArchiveNode> Walk(ArchiveNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in Walk(child)) yield return descendant;
    }

    public void AddPath(string sourcePath, string prefix = "", CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourcePath));
        var attributes = File.GetAttributes(full);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException($"Symbolic link import is not supported: {full}");
        var entryPath = (prefix.Trim('/') + "/" + Path.GetFileName(full)).TrimStart('/');
        _log.Debug("Importing {Source} as {Entry}", full, entryPath);
        if ((attributes & FileAttributes.Directory) != 0)
        {
            var directory = AddDirectory(entryPath);
            directory.LastModified = Directory.GetLastWriteTimeUtc(full);
            foreach (var child in Directory.EnumerateFileSystemEntries(full)) AddPath(child, entryPath, ct);
        }
        else
        {
            var info = new FileInfo(full);
            var node = AddLazyFile(entryPath, () => File.OpenRead(full));
            node.OriginalSize = info.Length;
            node.LastModified = info.LastWriteTimeUtc;
        }
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
        return _index.GetValueOrDefault(NormalizePath(path));
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
        if (parts.Length > ArchiveLimits.MaxPathDepth) throw new InvalidDataException("Archive path exceeds the nesting limit.");
        var current = Root;

        foreach (var part in parts)
        {
            var fullPath = $"{current.FullPath}/{part}";
            var child = _index.GetValueOrDefault(fullPath);
            if (child is { Type: NodeType.File })
                throw new InvalidDataException($"File/directory collision at {path}");

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
                _index.Add(child.FullPath, child);
                if (++_nodeCount > ArchiveLimits.MaxEntries) throw new InvalidDataException("Archive contains too many entries.");
                Root.IsDirty = true;
            }
            current = child;
        }
        return current;
    }

    private static string NormalizePath(string path) =>
        "/" + string.Join('/', path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).Where(p => p != "."));

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
        if (_disposed) return;
        _disposed = true;
        _log.Debug("Disposing VFS resources ({Count})", _resources.Count);
        foreach (var resource in _resources.AsEnumerable().Reverse())
        {
            try { resource.Dispose(); }
            catch (Exception ex) { _log.Warning(ex, "Could not dispose archive resource"); }
        }
        _resources.Clear();
        _index.Clear();
        Root.Children.Clear();
        GC.SuppressFinalize(this);
    }
}
