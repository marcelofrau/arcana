using Serilog;

namespace Arcana.Core.Filesystem;

/// <summary>Publishes a completed file by renaming a temporary on the same volume.</summary>
public static class AtomicFile
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(AtomicFile));

    public static async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write,
        CancellationToken ct = default, bool overwrite = true)
    {
        path = Path.GetFullPath(path);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".arcana-{Guid.NewGuid():N}.tmp");
        ct.ThrowIfCancellationRequested();
        RejectLinks(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Log.Debug("Writing temporary for {Path}", path);
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, 81920, FileOptions.Asynchronous))
            {
                await write(stream, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                await stream.FlushAsync(ct).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            RejectLinks(path);
            ct.ThrowIfCancellationRequested();
            // Replace also works with open readers sharing delete access on Windows.
            if (overwrite && File.Exists(path)) File.Replace(temporary, path, destinationBackupFileName: null);
            else File.Move(temporary, path, overwrite: false);
            Log.Information("Committed {Path}", path);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Cancelled write to {Path}; destination preserved", path);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not commit {Path}; destination preserved", path);
            throw;
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception ex) { Log.Warning(ex, "Could not remove temporary {Path}", temporary); }
        }
    }

    public static void RejectLinks(string path)
    {
        for (var current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Symbolic links and junctions are not allowed in the destination: {current}");
            }
            catch (FileNotFoundException ex) { Log.Verbose(ex, "Destination does not exist yet: {Path}", current); }
            catch (DirectoryNotFoundException ex) { Log.Verbose(ex, "Parent does not exist yet: {Path}", current); }
        }
    }
}
