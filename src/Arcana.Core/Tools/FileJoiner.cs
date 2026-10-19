using System.Text.RegularExpressions;
using Arcana.Core.Compression;
using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.Core.Tools;

public class FileJoiner
{
    private static readonly ILogger Log = Serilog.Log.ForContext<FileJoiner>();
    private static (string Base, int Number)? ParsePart(string path)
    {
        var name = Path.GetFileName(path);
        var match = Regex.Match(name, @"^(.*)\.part(\d{3,})(\.[^/]*)?$", RegexOptions.CultureInvariant);
        if (match.Success && int.TryParse(match.Groups[2].Value, out var part))
            return (match.Groups[1].Value + ".part#" + match.Groups[3].Value, part);
        match = Regex.Match(name, @"^(.*)\.(\d{3,})$", RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[2].Value, out part)
            ? (match.Groups[1].Value + ".#", part) : null;
    }

    public static List<string> AutoDiscoverParts(string path)
    {
        var isDirectory = Directory.Exists(path);
        var directory = isDirectory ? Path.GetFullPath(path) : Path.GetDirectoryName(Path.GetFullPath(path))!;
        var requested = isDirectory ? null : ParsePart(path);
        if (!isDirectory && requested == null)
            throw new InvalidOperationException("Select a numbered part (.001 or .part001.ext).");
        var candidates = Directory.EnumerateFiles(directory)
            .Select(p => (Path: p, Part: ParsePart(p)))
            .Where(p => p.Part != null && (isDirectory || p.Part.Value.Base == requested!.Value.Base))
            .ToList();
        if (candidates.Count == 0 || candidates.Select(p => p.Part!.Value.Base).Distinct().Count() != 1)
            throw new InvalidOperationException("Select one part sequence; this directory has no parts or multiple sequences.");
        var sorted = candidates.OrderBy(p => p.Part!.Value.Number).ToList();
        for (var i = 0; i < sorted.Count; i++)
            if (sorted[i].Part!.Value.Number != i + 1)
                throw new InvalidOperationException($"Missing or duplicate part {i + 1}.");
        Log.Debug("Discovered {Count} consecutive parts in {Directory}", sorted.Count, directory);
        return sorted.Select(p => p.Path).ToList();
    }

    public void Join(IEnumerable<string> parts, string outputPath,
        IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => JoinAsync(parts, outputPath, progress, ct).GetAwaiter().GetResult();

    public async Task JoinAsync(IEnumerable<string> parts, string outputPath,
        IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        var list = parts.Select(Path.GetFullPath).ToList();
        if (list.Count == 0 || list.Distinct().Count() != list.Count)
            throw new ArgumentException("Provide a nonempty sequence without duplicate parts.", nameof(parts));
        if (list.Any(p => p.Equals(Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Output must not replace an input part.", nameof(outputPath));
        var parsed = list.Select(ParsePart).ToList();
        if (parsed.All(p => p.HasValue))
        {
            if (parsed.Select(p => p!.Value.Base).Distinct().Count() != 1
                || parsed.Where((p, i) => p!.Value.Number != i + 1).Any())
                throw new InvalidOperationException("Parts must belong to one consecutive sequence starting at 001.");
        }
        var total = list.Sum(p => new FileInfo(p).Length);
        var done = 0L;
        Log.Information("Joining {Count} parts to {Path}", list.Count, outputPath);
        await AtomicFile.WriteAsync(outputPath, async (output, token) =>
        {
            var buffer = new byte[81920];
            foreach (var path in list)
            {
                token.ThrowIfCancellationRequested();
                await using var input = File.OpenRead(path);
                int read;
                while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                    done += read;
                    progress?.Report(new ProgressReport { CurrentFile = Path.GetFileName(path),
                        BytesProcessed = done, TotalBytes = total, CurrentOperation = "Joining" });
                }
            }
            if (done != total) throw new IOException("Part sizes changed while joining.");
        }, ct).ConfigureAwait(false);
        Log.Information("Join complete: {Path}", outputPath);
    }
}
