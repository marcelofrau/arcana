namespace Arcana.Core.Compression;

/// <summary>Resource ceilings for untrusted archives, enforced before publishing extracted data.</summary>
public static class ArchiveLimits
{
    public const long MaxExpandedBytes = 64L * 1024 * 1024 * 1024;
    public const int MaxEntries = 100_000;
    public const int MaxPathDepth = 128;
    public const long MaxBufferedWriteBytes = 256L * 1024 * 1024;
}
