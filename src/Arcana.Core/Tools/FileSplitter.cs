using Arcana.Core.Compression;
using Serilog;

namespace Arcana.Core.Tools;

public class FileSplitter
{
    private readonly ILogger _log = Serilog.Log.ForContext<FileSplitter>();
    public void Split(string sourcePath, string outputDir, long partSize,
                      IProgress<ProgressReport>? progress = null, CancellationToken ct = default,
                      bool hjsplitMode = false)
    {
        SplitAsync(sourcePath, outputDir, partSize, progress, ct, hjsplitMode).GetAwaiter().GetResult();
    }

    public async Task SplitAsync(string sourcePath, string outputDir, long partSize,
                                 IProgress<ProgressReport>? progress = null, CancellationToken ct = default,
                                 bool hjsplitMode = false)
    {
        Directory.CreateDirectory(outputDir);
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var ext = Path.GetExtension(sourcePath);
        var buffer = new byte[81920];
        var partIndex = 0;

        await using var source = File.OpenRead(sourcePath);
        var totalBytes = source.Length;
        _log.Debug("Split start: {Path} ({Size}) into {PartSize} parts", sourcePath, totalBytes, partSize);

        while (source.Position < totalBytes)
        {
            ct.ThrowIfCancellationRequested();
            partIndex++;

            var partFile = hjsplitMode
                ? Path.Combine(outputDir, $"{baseName}.{partIndex:D3}")
                : Path.Combine(outputDir, $"{baseName}.part{partIndex:D3}{ext}");

            _log.Verbose("Writing part {Index}: {PartPath}", partIndex, partFile);
            await using var part = File.Create(partFile);
            var remaining = Math.Min(partSize, totalBytes - source.Position);

            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = await source.ReadAsync(buffer, 0, toRead, ct).ConfigureAwait(false);
                if (read == 0) break;

                await part.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                remaining -= read;

                progress?.Report(new ProgressReport
                {
                    CurrentFile = Path.GetFileName(partFile),
                    BytesProcessed = totalBytes - remaining,
                    TotalBytes = totalBytes,
                    CurrentOperation = "Splitting",
                });
            }
        }

        _log.Information("Split complete: {PartCount} parts", partIndex);
    }
}
