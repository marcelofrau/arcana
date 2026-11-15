using System.Collections.Concurrent;
using System.Threading.Channels;
using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.Core.Compression;

public static class ArchiveExtractor
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ArchiveExtractor));

    /// <summary>Entries at least this large get byte-level progress; smaller ones only the file counter.</summary>
    private const long ByteProgressThreshold = 1 << 20;

    public static string GetDestinationPath(string directory, string entryPath,
        ConcurrentDictionary<string, byte>? linkCache = null)
    {
        var relative = entryPath.Replace('\\', '/');
        if (relative.StartsWith("//", StringComparison.Ordinal))
            throw new InvalidDataException($"Unsafe archive path: {entryPath}");
        relative = relative.TrimStart('/');
        var parts = relative.Split('/');
        if (parts.Length > ArchiveLimits.MaxPathDepth) throw new InvalidDataException("Archive path exceeds the nesting limit.");
        if (parts.Any(p => p is "" or "." or ".." || p.Any(c => c < 32 || ":<>\"|?*".Contains(c))
            || p.EndsWith('.') || p.EndsWith(' ') || IsDeviceName(p)))
            throw new InvalidDataException($"Unsafe archive path: {entryPath}");
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(root, Path.Combine(parts)));
        if (!target.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException($"Archive path escapes its destination: {entryPath}");
        AtomicFile.RejectLinks(target, linkCache);
        return target;
    }

    public static async Task<ExtractResult> ExtractAsync(IEnumerable<ArchiveNode> selection, string destination,
        bool overwrite = false, IProgress<ProgressReport>? progress = null, CancellationToken ct = default,
        bool preserveStructure = true, Func<string, Task<ConflictDecision>>? resolveConflict = null,
        int? maxParallel = null)
    {
        var nodes = selection.SelectMany(Walk).Distinct().Where(n => n.FullPath.Length > 0).ToList();
        // One pass builds the file and directory targets, detects collisions and reports
        // preparation progress. The old shape materialized the whole target list twice more
        // (Distinct over 274k paths, then two Where passes) for no benefit.
        var comparison = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var seenTargets = new HashSet<string>(nodes.Count, comparison);
        var fileTargets = new List<(ArchiveNode Node, string Path)>(nodes.Count);
        // Every directory that must exist before writing: explicit directory entries plus the
        // parents of file targets. Created once per directory instead of once per file.
        var directories = new HashSet<string>(comparison);
        // One validated-segment cache for the whole operation: 274k entries then cost one
        // attribute check per destination directory instead of one per path segment per entry.
        var linkCache = new ConcurrentDictionary<string, byte>(comparison);
        var prepareClock = System.Diagnostics.Stopwatch.StartNew();
        long lastPrepareMs = 0;
        foreach (var node in nodes)
        {
            ct.ThrowIfCancellationRequested();
            if (!preserveStructure && node.Type != NodeType.File) continue;
            var target = GetDestinationPath(destination, preserveStructure ? node.FullPath : node.Name, linkCache);
            if (!seenTargets.Add(target))
                throw new InvalidDataException("Multiple archive entries resolve to the same destination.");
            if (node.Type == NodeType.Directory) directories.Add(target);
            else
            {
                fileTargets.Add((node, target));
                directories.Add(Path.GetDirectoryName(target)!);
            }
            if (prepareClock.ElapsedMilliseconds - lastPrepareMs >= 100)
            {
                lastPrepareMs = prepareClock.ElapsedMilliseconds;
                progress?.Report(new ProgressReport
                {
                    CurrentFile = node.Name,
                    FilesProcessed = fileTargets.Count + directories.Count,
                    CurrentOperation = "Preparing",
                });
            }
        }
        ct.ThrowIfCancellationRequested();
        AtomicFile.RejectLinks(destination, linkCache);
        Directory.CreateDirectory(destination);

        // Pre-create all directories first (sequential, fast)
        foreach (var directory in directories) Directory.CreateDirectory(directory);

        var prepareMs = prepareClock.ElapsedMilliseconds;
        var total = fileTargets.Count;
        // Uncompressed size of the selection: lets the dialog show byte %, speed and ETA
        // instead of only a file counter.
        var totalBytes = fileTargets.Sum(t => t.Node.OriginalSize);
        // Archive-side size of the same selection: drives the ratio readout and the
        // archive-side bar. Zero for formats that do not expose per-entry compressed sizes.
        var totalCompressedBytes = fileTargets.Sum(t => t.Node.CompressedSize);
        if (total == 0)
        {
            Log.Information("Extraction completed: 0 files to {Destination}", destination);
            return new ExtractResult(0, 0, 0, []);
        }

        var parallelism = maxParallel ?? Math.Min(Environment.ProcessorCount, 8);
        var results = new ConcurrentBag<ExtractedFile>();
        long expanded = 0;
    long extractedBytes = 0;
        long compressedBytes = 0;
        long processedCount = 0;
        ConflictAction? remainingAction = null;
        var remainingActionLock = new object();
        var conflictSemaphore = new SemaphoreSlim(1, 1); // Serialize conflict resolution
        
        // Throttled progress reporting - avoid flooding UI thread, but still update
        // per file on small archives (~1% of the total, capped at 50 files).
        var progressThrottle = new ProgressThrottle(Math.Clamp(total / 100, 1, 50));

        // Bounded channel to avoid memory pressure - capacity = 2x parallelism
        var channel = Channel.CreateBounded<(ArchiveNode Node, string Target, string OriginalTarget)>(new BoundedChannelOptions(parallelism * 2)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

        // Producer: read archive entries sequentially, push to channel
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var (node, originalTarget) in fileTargets)
                {
                    ct.ThrowIfCancellationRequested();
                    await channel.Writer.WriteAsync((node, originalTarget, originalTarget), ct).ConfigureAwait(false);
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, ct);

        // Consumers: pull from channel, write files in parallel
        var consumerTasks = Enumerable.Range(0, parallelism).Select(_ => Task.Run(async () =>
        {
            await foreach (var (node, originalTarget, _) in channel.Reader.ReadAllAsync(ct))
            {
                ct.ThrowIfCancellationRequested();
                var target = originalTarget;
                AtomicFile.RejectLinks(target, linkCache);

                var exists = File.Exists(target);
                var replace = overwrite;
                ConflictDecision? decision = null;

                if (exists && !overwrite && resolveConflict != null)
                {
                    // Serialize conflict resolution to ensure ApplyToAll works correctly
                    await conflictSemaphore.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        lock (remainingActionLock)
                        {
                            if (remainingAction.HasValue)
                                decision = new ConflictDecision(remainingAction.Value);
                        }
                        if (decision == null)
                            decision = await resolveConflict(target).ConfigureAwait(false);
                        
                        if (decision.ApplyToAll)
                        {
                            lock (remainingActionLock)
                            {
                                remainingAction = decision.Action;
                            }
                        }
                        if (decision.Action == ConflictAction.Cancel)
                            throw new OperationCanceledException(ct);
                        replace = decision.Action == ConflictAction.Replace;
                        if (decision.Action == ConflictAction.Rename)
                        {
                            var number = 1;
                            do
                            {
                                var name = $"{Path.GetFileNameWithoutExtension(originalTarget)} ({number++}){Path.GetExtension(originalTarget)}";
                                target = GetDestinationPath(Path.GetDirectoryName(originalTarget)!, name, linkCache);
                            } while (File.Exists(target) || Directory.Exists(target));
                            exists = false;
                        }
                        Log.Debug("Extraction conflict for {Path}: {Action}", originalTarget, decision.Action);
                    }
                    finally
                    {
                        conflictSemaphore.Release();
                    }
                }

                var relative = Path.GetRelativePath(destination, target);
                ExtractedFile result;
                if (exists && !replace)
                {
                    Log.Debug("Skipped existing file {Path}", target);
                    result = new ExtractedFile(relative, ExtractStatus.Skipped);
                }
                else
                {
                    await AtomicFile.WriteAsync(target, async (output, token) =>
                    {
                        // Byte-level progress only for entries big enough to matter: ProgressStream
                        // reports once per file on dispose, which is 274k UI posts on a huge archive.
                        using var counted = node.OriginalSize >= ByteProgressThreshold
                            ? new ProgressStream(output, progress, node.Name, totalBytes,
                                (int)Interlocked.Read(ref processedCount), total, "Extracting", isOutput: true, leaveOpen: true,
                                baseBytes: Interlocked.Read(ref extractedBytes),
                                compressedBaseBytes: Interlocked.Read(ref compressedBytes),
                                compressedScale: node.CompressedSize > 0 && node.OriginalSize > 0
                                    ? (double)node.CompressedSize / node.OriginalSize
                                    : 0)
                            : null;
                        var copied = await ArchiveContent.CopyVerifiedAsync(node, counted ?? output, token, ArchiveLimits.MaxExpandedBytes - Interlocked.Read(ref expanded)).ConfigureAwait(false);
                        Interlocked.Add(ref expanded, copied);
                        Interlocked.Add(ref extractedBytes, copied);
                    }, ct, replace, node.OriginalSize > 0 ? node.OriginalSize : null,
                        // Replacing an existing file keeps temp+rename so a failed read cannot
                        // truncate what is already there; fresh files are written in place.
                        durable: false, linkCache: linkCache, directoryExists: true, directWrite: !exists).ConfigureAwait(false);
                    if (node.LastModified != DateTime.MinValue)
                        File.SetLastWriteTimeUtc(target, node.LastModified.ToUniversalTime());
                    Interlocked.Add(ref compressedBytes, node.CompressedSize);
                    result = new ExtractedFile(relative, exists ? ExtractStatus.Overwritten : ExtractStatus.Extracted);
                }

                results.Add(result);
                var currentCount = Interlocked.Increment(ref processedCount);

                if (progressThrottle.ShouldReport(currentCount))
                    progress?.Report(new ProgressReport
                    {
                        CurrentFile = node.Name,
                        FilesProcessed = (int)currentCount,
                        TotalFiles = total,
                        BytesProcessed = Interlocked.Read(ref extractedBytes),
                        TotalBytes = totalBytes,
                        CompressedBytes = Interlocked.Read(ref compressedBytes),
                        TotalCompressedBytes = totalCompressedBytes,
                        CurrentOperation = "Extracting",
                    });
            }
        }, ct)).ToArray();

        await Task.WhenAll(producerTask, Task.WhenAll(consumerTasks)).ConfigureAwait(false);

        // Final progress report
        progress?.Report(new ProgressReport
        {
            CurrentFile = "",
            FilesProcessed = total,
            TotalFiles = total,
            BytesProcessed = Interlocked.Read(ref extractedBytes),
            TotalBytes = totalBytes,
            CompressedBytes = Interlocked.Read(ref compressedBytes),
            TotalCompressedBytes = totalCompressedBytes,
            CurrentOperation = "Extracting",
        });

        var resultList = results.ToList();
        var elapsed = prepareClock.Elapsed.TotalSeconds;
        var extractSeconds = Math.Max(elapsed - prepareMs / 1000.0, 0.001);
        Log.Information(
            "Extraction completed: {Count} files to {Destination} in {Seconds:0.0}s ({Rate:0.0} files/s; prepare {Prepare:0.0}s; parallel={Parallel})",
            resultList.Count, destination, elapsed, resultList.Count / extractSeconds, prepareMs / 1000.0, parallelism);
        return new ExtractResult(
            resultList.Count(r => r.Status == ExtractStatus.Extracted),
            resultList.Count(r => r.Status == ExtractStatus.Skipped),
            resultList.Count(r => r.Status == ExtractStatus.Overwritten),
            resultList);
    }

    private static IEnumerable<ArchiveNode> Walk(ArchiveNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in Walk(child))
                yield return descendant;
    }

    private static bool IsDeviceName(string name)
    {
        var stem = name.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$"
            || stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && "123456789¹²³".Contains(stem[3]);
    }
}

public enum ExtractStatus { Extracted, Skipped, Overwritten }
public enum ConflictAction { Skip, Replace, Rename, Cancel }
public sealed record ConflictDecision(ConflictAction Action, bool ApplyToAll = false);
public sealed record ExtractedFile(string RelativePath, ExtractStatus Status);
public sealed record ExtractResult(int Extracted, int Skipped, int Overwritten, IReadOnlyList<ExtractedFile> Files);
