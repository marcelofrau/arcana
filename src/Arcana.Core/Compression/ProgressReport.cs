namespace Arcana.Core.Compression;

public class ProgressReport
{
    public string? CurrentFile { get; init; }
    public long BytesProcessed { get; init; }
    public long TotalBytes { get; init; }
    public double Percentage => TotalBytes > 0 ? (double)BytesProcessed / TotalBytes * 100 : 0;
    public int FilesProcessed { get; init; }
    public int TotalFiles { get; init; }
    public string? CurrentOperation { get; init; }
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
}
