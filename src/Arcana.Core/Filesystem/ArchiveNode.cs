namespace Arcana.Core.Filesystem;

public class ArchiveNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public NodeType Type { get; init; }
    public long OriginalSize { get; set; }
    public long CompressedSize { get; set; }
    public uint? Crc32 { get; set; }
    public bool IsDirty { get; set; }
    public ArchiveNode? Parent { get; set; }
    public List<ArchiveNode> Children { get; set; } = new();
    public DateTime LastModified { get; set; }

    public IEnumerable<ArchiveNode> ChildFolders =>
        Children.Where(c => c.Type == NodeType.Directory);

    public Func<Stream>? ContentFactory { get; set; }

    /// <summary>
    /// True when <see cref="ContentFactory"/> hands out a fresh owned stream on
    /// every call (callers must dispose it, e.g. lazy disk-backed engines).
    /// False when the stream is a shared backing stream owned by the node
    /// (callers must NOT dispose it — see <see cref="NonDisposingReadStream"/>).
    /// </summary>
    public bool OwnsContent { get; init; }

    public Stream OpenRead()
    {
        var stream = ContentFactory?.Invoke()
            ?? throw new InvalidOperationException($"No content available for '{FullPath}'");
        if (stream.CanSeek)
            stream.Position = 0;
        if (OwnsContent)
            return stream;
        // Callers dispose the returned stream; the node owns the backing stream, so
        // hand out a wrapper whose Dispose is a no-op to keep it alive for reuse.
        return new NonDisposingReadStream(stream);
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
