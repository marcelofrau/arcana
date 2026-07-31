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
        await using var stream = File.Create(path);
        await archive.FormatEngine.SaveAsync(archive, stream, options, progress, ct);
    }

    public async Task ExtractAsync(Archive archive, ArchiveNode node, string destinationDir,
                                   IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDir);

        var files = new List<ArchiveNode>();
        CollectFiles(node, files);
        var done = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var rel = file.FullPath.TrimStart('/');
            var target = Path.Combine(destinationDir, rel);
            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            await using var src = file.OpenRead();
            await using var dst = File.Create(target);
            await src.CopyToAsync(dst, ct);

            done++;
            progress?.Report(new ProgressReport
            {
                CurrentFile = file.Name,
                FilesProcessed = done,
                TotalFiles = files.Count,
                CurrentOperation = "Extracting",
            });
        }

        Log.Information("Extracted {Count} files to {Destination}", files.Count, destinationDir);
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
