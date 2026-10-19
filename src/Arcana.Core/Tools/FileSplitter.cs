using Arcana.Core.Compression;
using Arcana.Core.Filesystem;
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(partSize);
        ct.ThrowIfCancellationRequested();
        Directory.CreateDirectory(outputDir);
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var ext = Path.GetExtension(sourcePath);
        var buffer = new byte[81920];
        var partIndex = 0;

        await using var source = File.OpenRead(sourcePath);
        var totalBytes = source.Length;
        _log.Debug("Split start: {Path} ({Size}) into {PartSize} parts", sourcePath, totalBytes, partSize);

        var created = new List<string>();
        try
        {
        do
        {
            ct.ThrowIfCancellationRequested();
            partIndex++;

            var partFile = hjsplitMode
                ? Path.Combine(outputDir, $"{Path.GetFileName(sourcePath)}.{partIndex:D3}")
                : Path.Combine(outputDir, $"{baseName}.part{partIndex:D3}{ext}");

            _log.Verbose("Writing part {Index}: {PartPath}", partIndex, partFile);
            await AtomicFile.WriteAsync(partFile, async (part, token) =>
            {
                var remaining = Math.Min(partSize, totalBytes - source.Position);
                while (remaining > 0)
                {
                token.ThrowIfCancellationRequested();
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = await source.ReadAsync(buffer, 0, toRead, ct).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException("Source changed while splitting.");

                await part.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                remaining -= read;

                progress?.Report(new ProgressReport
                {
                    CurrentFile = Path.GetFileName(partFile),
                    BytesProcessed = source.Position,
                    TotalBytes = totalBytes,
                    CurrentOperation = "Splitting",
                });
                }
            }, ct, overwrite: false);
            created.Add(partFile);
        } while (source.Position < totalBytes);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Split failed; removing {Count} new parts", created.Count);
            foreach (var path in created)
            {
                try { File.Delete(path); }
                catch (Exception cleanup) { _log.Warning(cleanup, "Could not remove part {Path}", path); }
            }
            throw;
        }

        _log.Information("Split complete: {PartCount} parts", partIndex);
    }
}
