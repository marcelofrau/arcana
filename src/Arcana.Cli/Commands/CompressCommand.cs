using System.CommandLine;
using System.CommandLine.Parsing;
using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Cryptography;
using Arcana.Core.Filesystem;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class CompressCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(CompressCommand));

    public static Command Create()
    {
        var command = new Command("compress", "Compress files and directories into an archive");

        var sourceArg = new Argument<string[]>("source") { Description = "Files/directories to compress" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output archive path", Required = true };
        var formatOpt = new Option<string>("--format", "-f") { Description = "Archive format (zip, 7z, zstd)" };
        var levelOpt = new Option<int>("--level", "-l") { Description = "Compression level (0-9)" };
        var passwordOpt = new Option<string>("--password", "-p") { Description = "Encryption password" };

        command.Add(sourceArg);
        command.Add(outputOpt);
        command.Add(formatOpt);
        command.Add(levelOpt);
        command.Add(passwordOpt);

        command.SetAction(async (ParseResult r) =>
        {
            var sources = r.GetValue(sourceArg);
            var output = r.GetValue(outputOpt);
            var format = r.GetValue(formatOpt) ?? "zip";
            var level = r.GetValue(levelOpt);
            var password = r.GetValue(passwordOpt);
            var fileCount = CountSources(sources!);

            Log.Information("Compress start: {SourceCount} sources -> {Output}", fileCount, output);
            Archive archive;
            try
            {
                archive = BuildArchive(sources!, output!, format, level);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to build archive: {Message}", ex.Message);
                return 1;
            }

            await Output.StartProgressAsync($"Compressing {fileCount} {(fileCount == 1 ? "file" : "files")} to [cyan]{Path.GetFileName(output)}[/]",
                async ctx =>
                {
                    var task = ctx.AddTask($"Compressing {fileCount} files", maxValue: fileCount);
                    var progress = new Progress<ProgressReport>(report =>
                    {
                        task.Value = report.FilesProcessed;
                        task.Description = report.CurrentFile != null
                            ? $"[cyan]{report.CurrentFile}[/]"
                            : $"Compressing...";
                    });

                    var engine = new ZipEngine();
                    await using var stream = File.Create(output!);
                    await engine.SaveAsync(archive, stream, new CompressionOptions
                    {
                        Format = CompressionFormat.Zip,
                        Level = (CompressionLevel)Math.Clamp(level, 0, 9),
                        Encryption = password != null ? new EncryptionOptions { Password = password } : null,
                    }, progress);
                });

            var fi = new FileInfo(output!);
            var size = FormatSize(fi.Length);
            Log.Information("Compress complete: {Output} ({Size})", output, size);
            Output.Success($"Created [cyan]{Path.GetFileName(output)}[/] [dim]({size})[/]");
            return 0;
        });

        return command;
    }

    private static Archive BuildArchive(string[] sources, string outputPath, string format, int level)
    {
        var vfs = new VirtualFileSystem();
        var entries = new List<ArchiveEntry>();

        foreach (var source in sources)
        {
            var attr = File.GetAttributes(source);
            if (attr.HasFlag(FileAttributes.Directory))
                AddDirectoryToVfs(vfs, entries, source, "");
            else
                AddFileToVfs(vfs, entries, source, "");
        }

        return new Archive
        {
            Format = CompressionFormat.Zip,
            FormatEngine = new ZipEngine(),
            Entries = entries,
            Vfs = vfs,
        };
    }

    private static void AddDirectoryToVfs(VirtualFileSystem vfs, List<ArchiveEntry> entries, string dirPath, string prefix)
    {
        var dirName = Path.GetFileName(dirPath);
        var dirEntryPath = string.IsNullOrEmpty(prefix) ? dirName : $"{prefix}/{dirName}";

        vfs.AddDirectory(dirEntryPath);

        foreach (var file in Directory.GetFiles(dirPath))
            AddFileToVfs(vfs, entries, file, dirEntryPath);

        foreach (var subDir in Directory.GetDirectories(dirPath))
            AddDirectoryToVfs(vfs, entries, subDir, dirEntryPath);
    }

    private static void AddFileToVfs(VirtualFileSystem vfs, List<ArchiveEntry> entries, string filePath, string prefix)
    {
        var fileName = Path.GetFileName(filePath);
        var entryPath = string.IsNullOrEmpty(prefix) ? fileName : $"{prefix}/{fileName}";
        var fi = new FileInfo(filePath);

        using var stream = File.OpenRead(filePath);
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;

        Log.Debug("Added file {EntryPath} to archive", entryPath);
        vfs.AddFile(entryPath, ms);

        entries.Add(new ArchiveEntry
        {
            Path = entryPath,
            Name = fileName,
            Size = fi.Length,
            IsDirectory = false,
            LastModified = fi.LastWriteTimeUtc,
        });
    }

    private static int CountSources(string[] sources)
    {
        var count = 0;
        foreach (var source in sources)
        {
            if (Directory.Exists(source))
                count += Directory.GetFiles(source, "*", SearchOption.AllDirectories).Length;
            else
                count++;
        }
        return count;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    };
}
