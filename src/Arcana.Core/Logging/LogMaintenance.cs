using System.Globalization;
using System.IO.Compression;
using Serilog;

namespace Arcana.Core.Logging;

/// <summary>
/// Keeps the log folder small: the newest daily logs stay as plain text, older ones are gzipped,
/// and only a fixed number of archives are kept. Runs once per start, in the background.
/// </summary>
public static class LogMaintenance
{
    /// <summary>Daily logs kept uncompressed (the newest ones).</summary>
    public const int KeepRecent = 5;

    /// <summary>Compressed archives kept; older ones are deleted.</summary>
    public const int KeepArchives = 10;

    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(LogMaintenance));

    public static void RunInBackground(string directory)
    {
        var task = Task.Run(() => Run(directory));
        // A short-lived process (CLI) exits before a pool thread finishes; give it a moment.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => task.Wait(TimeSpan.FromSeconds(5));
    }

    public static void Run(string directory)
    {
        try
        {
            if (!Directory.Exists(directory)) return;
            var today = $"arcana-{DateTime.Now:yyyyMMdd}";

            // Windows matches "*.log" against "x.log.gz" too, so filter the extension explicitly.
            var logs = Directory.EnumerateFiles(directory, "arcana-*.log")
                .Where(p => p.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                .Where(p => !Path.GetFileName(p).StartsWith(today, StringComparison.Ordinal))
                .OrderByDescending(LogDate)
                .ToList();

            long freed = 0;
            var compressed = 0;
            foreach (var path in logs.Skip(KeepRecent))
            {
                var archive = path + ".gz";
                if (!File.Exists(archive))
                {
                    var original = new FileInfo(path).Length;
                    var temporary = archive + ".tmp";
                    using (var input = File.OpenRead(path))
                    using (var output = File.Create(temporary))
                    using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                        input.CopyTo(gzip);
                    File.Move(temporary, archive, overwrite: true);
                    freed += original - new FileInfo(archive).Length;
                    compressed++;
                }
                File.Delete(path);
            }

            long removed = 0;
            var deleted = 0;
            foreach (var old in Directory.EnumerateFiles(directory, "arcana-*.log.gz")
                         .OrderByDescending(LogDate).Skip(KeepArchives).ToList())
            {
                removed += new FileInfo(old).Length;
                File.Delete(old);
                deleted++;
            }

            if (compressed > 0 || deleted > 0)
                Log.Information(
                    "Log maintenance: {Compressed} log(s) compressed, {Deleted} archive(s) deleted, {FreedKb} KB reclaimed",
                    compressed, deleted, (freed + removed) / 1024);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not compact old logs in {Directory}", directory);
        }
    }

    private static DateTime LogDate(string path)
    {
        var name = Path.GetFileName(path);
        if (name.Length >= 15 && DateTime.TryParseExact(name.AsSpan(7, 8), "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        return File.GetLastWriteTime(path);
    }
}
