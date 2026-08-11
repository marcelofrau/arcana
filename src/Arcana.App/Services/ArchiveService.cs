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
    private static readonly uint[] CrcTable = BuildCrcTable();

    public Archive? CurrentArchive { get; private set; }

    public async Task<Archive> OpenAsync(string path, CancellationToken ct = default)
    {
        CurrentArchive = await ArchiveFactory.OpenAsync(path, mode: AccessMode.Read, ct: ct);
        return CurrentArchive;
    }

    public async Task SaveAsync(Archive archive, string path, CompressionOptions options,
                                IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        await using var file = File.Create(path);
        // Write-side counter: reports compressed bytes written live, driving the
        // overall bar and the compression-ratio chart in the progress dialog.
        using var counted = new ProgressStream(file, progress, operation: "Compressing", isOutput: true);
        await archive.FormatEngine.SaveAsync(archive, counted, options, progress, ct);
        Log.Information("Saved archive to {Path}", path);
    }

    public async Task<ExtractResult> ExtractAsync(Archive archive, ArchiveNode node, string destinationDir,
        bool overwrite, IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDir);

        var files = new List<ArchiveNode>();
        CollectFiles(node, files);
        var done = 0;
        var extracted = 0;
        var skipped = 0;
        var overwritten = 0;
        var fileEntries = new List<ExtractedFile>();

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var rel = file.FullPath.TrimStart('/');
            var target = Path.Combine(destinationDir, rel);
            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            if (File.Exists(target) && !overwrite)
            {
                skipped++;
                fileEntries.Add(new ExtractedFile(rel, ExtractStatus.Skipped));
                progress?.Report(new ProgressReport
                {
                    CurrentFile = file.Name,
                    FilesProcessed = ++done,
                    TotalFiles = files.Count,
                    CurrentOperation = "Extracting",
                });
                continue;
            }

            var wasOverwrite = File.Exists(target);
            await using var src = file.OpenRead();
            // Write-side counter: reports bytes written to disk, which drives the
            // per-file bar and the speed chart during extraction.
            await using var dst = new ProgressStream(File.Create(target), progress,
                currentFile: file.Name, totalBytes: file.OriginalSize,
                filesProcessed: done, totalFiles: files.Count, operation: "Extracting", isOutput: true);
            await src.CopyToAsync(dst, ct);

            if (wasOverwrite) overwritten++; else extracted++;
            fileEntries.Add(new ExtractedFile(rel, wasOverwrite ? ExtractStatus.Overwritten : ExtractStatus.Extracted));
            done++;
            progress?.Report(new ProgressReport
            {
                CurrentFile = file.Name,
                FilesProcessed = done,
                TotalFiles = files.Count,
                CurrentOperation = "Extracting",
            });
        }

        Log.Information("Extracted {Extracted} (overwritten {Overwritten}, skipped {Skipped}) to {Destination}",
            extracted, overwritten, skipped, destinationDir);
        return new ExtractResult(extracted, skipped, overwritten, fileEntries);
    }

    public async Task<IReadOnlyList<TestResult>> TestAsync(Archive archive, ArchiveNode node,
                                                           IProgress<ProgressReport>? progress = null,
                                                           CancellationToken ct = default)
    {
        var entries = archive.Entries
            .Where(e => !e.IsDirectory && IsUnder(e.Path, node.FullPath))
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
                var vfsNode = archive.Vfs.FindNode(entry.Path);
                if (vfsNode == null)
                {
                    message = "entry missing";
                }
                else
                {
                    await using var content = vfsNode.OpenRead();
                    var crc = ComputeCrc32(content);
                    if (entry.Crc32 != 0 && entry.Crc32 != crc)
                    {
                        success = false;
                        message = "CRC mismatch";
                    }
                    else
                    {
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            results.Add(new TestResult(entry.Path, success, message));
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
        var normalized = dirPath.TrimEnd('/');
        if (normalized.Length == 0)
            return true;
        return path.StartsWith(normalized + "/", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void CollectFiles(ArchiveNode node, List<ArchiveNode> files)
    {
        foreach (var child in node.Children)
        {
            if (child.Type == NodeType.File)
                files.Add(child);
            else
                CollectFiles(child, files);
        }
    }

    private static uint ComputeCrc32(System.IO.Stream stream)
    {
        uint crc = 0xFFFFFFFF;
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
                crc = (crc >> 8) ^ CrcTable[(crc ^ buffer[i]) & 0xFF];
        }
        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }
}

public sealed record TestResult(string Path, bool Success, string Message);

/// <summary>Outcome of an extracted entry, used for the completion dialog badges.</summary>
public enum ExtractStatus { Extracted, Skipped, Overwritten }

public sealed record ExtractedFile(string RelativePath, ExtractStatus Status);

public sealed record ExtractResult(
    int Extracted, int Skipped, int Overwritten, IReadOnlyList<ExtractedFile> Files);
