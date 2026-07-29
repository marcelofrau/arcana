using System.Text.RegularExpressions;
using Arcana.Core.Compression;
using Serilog;

namespace Arcana.Core.Tools;

public class FileJoiner
{
    private readonly ILogger _log = Serilog.Log.ForContext<FileJoiner>();
    public static List<string> AutoDiscoverParts(string path)
    {
        if (Directory.Exists(path))
        {
            var dir = new DirectoryInfo(path);
            var files = dir.GetFiles()
                .Where(f => Regex.IsMatch(f.Extension, @"^\.[0-9]{3,4}$"))
                .OrderBy(f => f.Extension)
                .Select(f => f.FullName)
                .ToList();
            return files.Count > 0 ? files : throw new InvalidOperationException("No numbered part files found in directory");
        }

        var m = Regex.Match(Path.GetExtension(path), @"^\.(\d{3,4})$");
        if (!m.Success)
            throw new InvalidOperationException("Path does not match HJSplit naming (file.001, file.002...)");

        var basePath = Path.Combine(
            Path.GetDirectoryName(path)!,
            Path.GetFileNameWithoutExtension(path));

        var parts = new List<string>();
        for (var i = 1; ; i++)
        {
            var partFile = $"{basePath}.{i:D3}";
            if (!File.Exists(partFile)) break;
            parts.Add(partFile);
        }

        return parts.Count > 0 ? parts : throw new InvalidOperationException($"No parts found for base: {basePath}");
    }

    public void Join(IEnumerable<string> parts, string outputPath,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        JoinAsync(parts, outputPath, progress, ct).GetAwaiter().GetResult();
    }

    public async Task JoinAsync(IEnumerable<string> parts, string outputPath,
                                IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        var partList = parts.ToList();
        _log.Information("Join start: {PartCount} parts", partList.Count);
        _log.Debug("Parts: {@PartPaths}", partList);
        var totalBytes = partList.Sum(GetFileSize);
        var bytesDone = 0L;
        var buffer = new byte[81920];

        await using var output = File.Create(outputPath);

        for (var i = 0; i < partList.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var partFile = partList[i];

            _log.Verbose("Reading part {PartPath}", partFile);
            await using var input = File.OpenRead(partFile);
            int read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                bytesDone += read;

                progress?.Report(new ProgressReport
                {
                    CurrentFile = Path.GetFileName(partFile),
                    BytesProcessed = bytesDone,
                    TotalBytes = totalBytes,
                    CurrentOperation = "Joining",
                });
            }
        }

        _log.Information("Join complete: {Output}", outputPath);
    }

    private static long GetFileSize(string path) => new FileInfo(path).Length;
}
