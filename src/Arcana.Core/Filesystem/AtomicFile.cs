using System.Collections.Concurrent;
using Serilog;

namespace Arcana.Core.Filesystem;

/// <summary>Publishes a completed file by renaming a temporary on the same volume.</summary>
public static class AtomicFile
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(AtomicFile));

    /// <param name="durable">
    /// When true the file is flushed to the physical disk before the rename (power-loss safety).
    /// Bulk extraction passes false: one fsync per file costs ~20 ms, which for a 274k-entry
    /// archive is over an hour of waiting for a guarantee nobody needs mid-extraction.
    /// </param>
    /// <param name="directoryExists">
    /// Callers that already created the destination tree (extraction) skip the per-file
    /// directory syscall and the path split it needs.
    /// </param>
    /// <param name="directWrite">
    /// Bulk extraction writes the final path directly and deletes it if the write fails, instead
    /// of writing a temporary and renaming it: one create plus one delete on failure instead of
    /// create, write, rename per file. A crash mid-file leaves that one file truncated.
    /// </param>
    public static async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write,
        CancellationToken ct = default, bool overwrite = true, long? preallocateSize = null,
        bool durable = true, ConcurrentDictionary<string, byte>? linkCache = null,
        bool directoryExists = false, bool directWrite = false)
    {
        if (!Path.IsPathFullyQualified(path)) path = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(path)!;
        var temporary = Path.Combine(directory, $".arcana-{Guid.NewGuid():N}.tmp");
        ct.ThrowIfCancellationRequested();
        RejectLinks(path, linkCache);
        if (!directoryExists) Directory.CreateDirectory(directory);
        if (directWrite)
        {
            await WriteDirectAsync(path, write, ct, durable, preallocateSize).ConfigureAwait(false);
            return;
        }
        Log.Debug("Writing temporary for {Path} (preallocate: {Size})", path, preallocateSize);
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, 81920, FileOptions.Asynchronous))
            {
                if (preallocateSize.HasValue && preallocateSize.Value > 0)
                {
                    stream.SetLength(preallocateSize.Value);
                    stream.Position = 0;
                    Log.Debug("Preallocated {Size} bytes for {Path}", preallocateSize.Value, path);
                }
                await write(stream, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                await stream.FlushAsync(ct).ConfigureAwait(false);
                if (durable) stream.Flush(flushToDisk: true);
            }
            RejectLinks(path, linkCache);
            ct.ThrowIfCancellationRequested();
            // Replace also works with open readers sharing delete access on Windows.
            if (overwrite && File.Exists(path)) File.Replace(temporary, path, destinationBackupFileName: null);
            else File.Move(temporary, path, overwrite: false);
            // Per-file detail: bulk callers (extraction) log their own summary instead.
            Log.Verbose("Committed {Path}", path);
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

    /// <summary>Writes <paramref name="path"/> in place, deleting it again if the write fails.</summary>
    private static async Task WriteDirectAsync(string path, Func<Stream, CancellationToken, Task> write,
        CancellationToken ct, bool durable, long? preallocateSize)
    {
        try
        {
            await using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite,
                FileShare.None, 81920, FileOptions.Asynchronous))
            {
                if (preallocateSize.HasValue && preallocateSize.Value > 0)
                {
                    stream.SetLength(preallocateSize.Value);
                    stream.Position = 0;
                }
                await write(stream, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                await stream.FlushAsync(ct).ConfigureAwait(false);
                if (durable) stream.Flush(flushToDisk: true);
            }
            Log.Verbose("Wrote {Path}", path);
        }
        catch (Exception ex)
        {
            try { File.Delete(path); }
            catch (Exception cleanup) { Log.Warning(cleanup, "Could not remove partial {Path}", path); }
            if (ex is OperationCanceledException)
                Log.Information("Cancelled write to {Path}; partial file removed", path);
            else
                Log.Error(ex, "Could not write {Path}; partial file removed", path);
            throw;
        }
    }

    /// <summary>Rejects reparse points (symlinks, junctions) on any segment of <paramref name="path"/>.</summary>
    /// <param name="linkCache">
    /// Optional per-operation cache of already validated segments. Bulk callers (extraction) reuse it so a
    /// destination tree of thousands of entries costs one attribute check per directory, not per entry.
    /// </param>
    public static void RejectLinks(string path, ConcurrentDictionary<string, byte>? linkCache = null)
    {
        for (var current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
        {
            // Ancestors are cached in the same upward walk, so a hit ends the check.
            if (linkCache != null && linkCache.ContainsKey(current)) break;
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Symbolic links and junctions are not allowed in the destination: {current}");
            }
            catch (FileNotFoundException) { /* destination doesn't exist yet - normal during extraction */ }
            catch (DirectoryNotFoundException) { /* parent doesn't exist yet - normal during extraction */ }
            linkCache?.TryAdd(current, 0);
        }
    }
}
