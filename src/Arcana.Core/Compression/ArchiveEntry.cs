namespace Arcana.Core.Compression;

public class ArchiveEntry
{
    public string Path { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public long Size { get; init; }
    public long CompressedSize { get; init; }
    public double CompressionRatio => Size > 0 ? (double)CompressedSize / Size : 0;
    public uint Crc32 { get; init; }
    public string? Comment { get; init; }
    public bool IsEncrypted { get; init; }
    public bool IsDirectory { get; init; }
    public DateTime LastModified { get; init; }
    public string? Method { get; init; }
    public string? HostOS { get; init; }
    public uint Attributes { get; init; }
}
