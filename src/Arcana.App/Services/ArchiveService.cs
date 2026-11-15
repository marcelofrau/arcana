using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcana.Core.Compression;
using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.App.Services;

public class ArchiveService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ArchiveService>();

    public Archive? CurrentArchive { get; private set; }

    public async Task<Archive> OpenAsync(string path, CancellationToken ct = default, string? password = null,
                                     IProgress<ProgressReport>? progress = null)
    {
        CurrentArchive = await ArchiveFactory.OpenAsync(path, password, mode: AccessMode.Read, ct: ct, progress: progress);
        return CurrentArchive;
    }

    public async Task SaveAsync(Archive archive, string path, CompressionOptions options,
                                IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        await ArchiveWriter.SaveAsync(archive, path, options, progress, ct);
        Log.Information("Saved archive to {Path}", path);
    }

    public Task<ExtractResult> ExtractAsync(Archive archive, ArchiveNode node, string destinationDir,
        bool overwrite, IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => ArchiveExtractor.ExtractAsync([node], destinationDir, overwrite, progress, ct);

    public async Task<IReadOnlyList<TestResult>> TestAsync(Archive archive, ArchiveNode node,
                                                           IProgress<ProgressReport>? progress = null,
                                                           CancellationToken ct = default)
    {
        var entries = archive.Vfs.EnumerateNodes()
            .Where(e => e.Type == NodeType.File && IsUnder(e.FullPath, node.FullPath))
            .ToList();

        var results = new List<TestResult>();
        var throttle = new ProgressThrottle(Math.Max(1, entries.Count / 100));
        // Time-gated only (the count step never trips): the per-chunk callback fires every
        // 80 KB, so a single huge entry still shows bytes moving instead of sitting at 0%.
        var byteThrottle = new ProgressThrottle(int.MaxValue);
        var totalBytes = entries.Sum(e => e.OriginalSize);
        var totalCompressedBytes = entries.Sum(e => e.CompressedSize);
        var started = System.Diagnostics.Stopwatch.StartNew();
        var done = 0;
        long doneBytes = 0;
        long doneCompressedBytes = 0;

        Log.Information("Testing {Count} entries ({Bytes} bytes) under {Path}", entries.Count, totalBytes, node.FullPath);

        void Report(ArchiveNode current, long entryBytes)
        {
            // Same estimate as extraction: the archive side follows the entry's own ratio so
            // the second line and the ratio text move while one huge entry is still reading.
            var estimate = current.OriginalSize > 0
                ? (long)((double)current.CompressedSize * entryBytes / current.OriginalSize)
                : 0;
            progress?.Report(new ProgressReport
            {
                CurrentFile = current.Name,
                FilesProcessed = done,
                TotalFiles = entries.Count,
                BytesProcessed = doneBytes + entryBytes,
                TotalBytes = totalBytes,
                CompressedBytes = doneCompressedBytes + estimate,
                TotalCompressedBytes = totalCompressedBytes,
                CurrentOperation = "Testing",
            });
        }

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            var success = false;
            var message = "OK";
            try
            {
                doneBytes += await ArchiveContent.CopyVerifiedAsync(entry, Stream.Null, ct,
                    onProgress: bytes =>
                    {
                        if (byteThrottle.ShouldReport(bytes))
                            Report(entry, bytes);
                    });
                success = true;
                message = entry.Crc32.HasValue ? "CRC verified" : "Content readable; no stored CRC";
            }
            catch (OperationCanceledException) { Log.Information("Integrity check cancelled"); throw; }
            catch (Exception ex)
            {
                Log.Warning(ex, "Integrity check failed for {Path}", entry.FullPath);
                message = ex.Message;
            }

            results.Add(new TestResult(entry.FullPath, success, message));
            done++;
            doneCompressedBytes += entry.CompressedSize;
            if (throttle.ShouldReport(done))
                Report(entry, 0);
        }

        Log.Information("Tested {Count} entries in {Elapsed:0.0}s, {Failed} failed",
            results.Count, started.Elapsed.TotalSeconds, results.Count(r => !r.Success));
        return results;
    }

    public void Close()
    {
        CurrentArchive?.Dispose();
        CurrentArchive = null;
    }

    private static bool IsUnder(string path, string dirPath)
    {
        var normalized = dirPath.Trim('/');
        if (normalized.Length == 0)
            return true;
        return path.Trim('/').StartsWith(normalized + "/", System.StringComparison.Ordinal)
            || path.Trim('/').Equals(normalized, System.StringComparison.Ordinal);
    }

}

public sealed record TestResult(string Path, bool Success, string Message);
