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

    public async Task<Archive> OpenAsync(string path, CancellationToken ct = default, string? password = null)
    {
        CurrentArchive = await ArchiveFactory.OpenAsync(path, password, mode: AccessMode.Read, ct: ct);
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
        var done = 0;

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            var success = false;
            var message = "OK";
            try
            {
                await ArchiveContent.CopyVerifiedAsync(entry, Stream.Null, ct);
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
            progress?.Report(new ProgressReport
            {
                CurrentFile = entry.Name,
                FilesProcessed = done,
                TotalFiles = entries.Count,
                CurrentOperation = "Testing",
            });
        }

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
