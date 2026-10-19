using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.Core.Compression;

public static class ArchiveExtractor
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ArchiveExtractor));

    public static string GetDestinationPath(string directory, string entryPath)
    {
        var relative = entryPath.Replace('\\', '/');
        // VFS paths have one leading slash; UNC paths and traversal are never valid.
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
        AtomicFile.RejectLinks(target);
        return target;
    }

    public static async Task<ExtractResult> ExtractAsync(IEnumerable<ArchiveNode> selection, string destination,
        bool overwrite = false, IProgress<ProgressReport>? progress = null, CancellationToken ct = default,
        bool preserveStructure = true, Func<string, Task<ConflictDecision>>? resolveConflict = null)
    {
        var nodes = selection.SelectMany(Walk).Distinct().Where(n => n.FullPath.Length > 0).ToList();
        var targets = nodes.Where(n => preserveStructure || n.Type == NodeType.File)
            .Select(n => (Node: n, Path: GetDestinationPath(destination, preserveStructure ? n.FullPath : n.Name))).ToList();
        var comparison = OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        if (targets.Select(t => t.Path).Distinct(comparison).Count() != targets.Count)
            throw new InvalidDataException("Multiple archive entries resolve to the same destination.");
        ct.ThrowIfCancellationRequested();
        AtomicFile.RejectLinks(destination);
        Directory.CreateDirectory(destination);
        var results = new List<ExtractedFile>();
        long expanded = 0;
        ConflictAction? remainingAction = null;
        var total = targets.Count(t => t.Node.Type == NodeType.File);
        foreach (var (node, originalTarget) in targets)
        {
            var target = originalTarget;
            ct.ThrowIfCancellationRequested();
            AtomicFile.RejectLinks(target);
            if (node.Type == NodeType.Directory)
            {
                Directory.CreateDirectory(target);
                continue;
            }
            var exists = File.Exists(target);
            var replace = overwrite;
            if (exists && !overwrite && resolveConflict != null)
            {
                var decision = remainingAction.HasValue ? new ConflictDecision(remainingAction.Value)
                    : await resolveConflict(target).ConfigureAwait(false);
                if (decision.ApplyToAll) remainingAction = decision.Action;
                if (decision.Action == ConflictAction.Cancel) throw new OperationCanceledException(ct);
                replace = decision.Action == ConflictAction.Replace;
                if (decision.Action == ConflictAction.Rename)
                {
                    var number = 1;
                    do
                    {
                        var name = $"{Path.GetFileNameWithoutExtension(originalTarget)} ({number++}){Path.GetExtension(originalTarget)}";
                        target = GetDestinationPath(Path.GetDirectoryName(originalTarget)!, name);
                    } while (File.Exists(target) || Directory.Exists(target));
                    exists = false;
                }
                Log.Debug("Extraction conflict for {Path}: {Action}", originalTarget, decision.Action);
            }
            var relative = Path.GetRelativePath(destination, target);
            if (exists && !replace)
            {
                Log.Debug("Skipped existing file {Path}", target);
                results.Add(new ExtractedFile(relative, ExtractStatus.Skipped));
            }
            else
            {
                await AtomicFile.WriteAsync(target, async (output, token) =>
                {
                    using var counted = new ProgressStream(output, progress, node.Name, node.OriginalSize,
                        results.Count, total, "Extracting", isOutput: true, leaveOpen: true);
                    expanded += await ArchiveContent.CopyVerifiedAsync(node, counted, token, ArchiveLimits.MaxExpandedBytes - expanded).ConfigureAwait(false);
                }, ct, replace).ConfigureAwait(false);
                if (node.LastModified != DateTime.MinValue)
                    File.SetLastWriteTimeUtc(target, node.LastModified.ToUniversalTime());
                results.Add(new ExtractedFile(relative, exists ? ExtractStatus.Overwritten : ExtractStatus.Extracted));
            }
            progress?.Report(new ProgressReport { CurrentFile = node.Name, FilesProcessed = results.Count,
                TotalFiles = total, CurrentOperation = "Extracting" });
        }
        Log.Information("Extraction completed: {Count} files to {Destination}", results.Count, destination);
        return new ExtractResult(results.Count(r => r.Status == ExtractStatus.Extracted),
            results.Count(r => r.Status == ExtractStatus.Skipped),
            results.Count(r => r.Status == ExtractStatus.Overwritten), results);
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
