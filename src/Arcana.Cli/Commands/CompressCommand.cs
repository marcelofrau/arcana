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
        var levelOpt = new Option<int>("--level", "-l") { Description = "Compression level (0-9)", DefaultValueFactory = _ => 5 };
        var passwordOpt = new Option<string>("--password", "-p") { Description = "Encryption password" };

        command.Add(sourceArg);
        command.Add(outputOpt);
        command.Add(formatOpt);
        command.Add(levelOpt);
        command.Add(passwordOpt);

        command.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var sources = r.GetValue(sourceArg);
            var output = r.GetValue(outputOpt);
            var format = r.GetValue(formatOpt);
            var targetFormat = format != null ? ArchiveFormats.Parse(format) : ArchiveFormats.FromPath(output!);
            var level = r.GetValue(levelOpt);
            var password = r.GetValue(passwordOpt);
            Archive archive;
            try
            {
                archive = BuildArchive(sources!, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to build archive: {Message}", ex.Message);
                return 1;
            }
            using var lifetime = archive;
            var fileCount = archive.Vfs.EnumerateNodes().Count(n => n.Type == NodeType.File);
            Log.Information("Compress start: {SourceCount} files -> {Output}", fileCount, output);

            await Output.StartProgressAsync($"Compressing {fileCount} {(fileCount == 1 ? "file" : "files")} to [cyan]{Markup.Escape(Path.GetFileName(output)!)}[/]",
                async ctx =>
                {
                    var task = ctx.AddTask($"Compressing {fileCount} files", maxValue: Math.Max(1, fileCount));
                    var progress = new Progress<ProgressReport>(report =>
                    {
                        task.Value = report.FilesProcessed;
                        task.Description = report.CurrentFile != null
                            ? $"[cyan]{Markup.Escape(report.CurrentFile)}[/]"
                            : $"Compressing...";
                    });

                    await ArchiveWriter.SaveAsync(archive, output!, new CompressionOptions
                    {
                        Format = targetFormat,
                        Level = (CompressionLevel)Math.Clamp(level, 0, 9),
                        Encryption = password != null ? new EncryptionOptions { Password = password } : null,
                    }, progress, ct);
                });

            var fi = new FileInfo(output!);
            var size = FormatSize(fi.Length);
            Log.Information("Compress complete: {Output} ({Size})", output, size);
            Output.Success($"Created [cyan]{Markup.Escape(Path.GetFileName(output)!)}[/] [dim]({size})[/]");
            return 0;
        });

        return command;
    }

    private static Archive BuildArchive(string[] sources, CancellationToken ct)
    {
        var vfs = new VirtualFileSystem();
        try
        {
            foreach (var source in sources) vfs.AddPath(source, ct: ct);
            return new Archive { Format = CompressionFormat.Zip, FormatEngine = new ZipEngine(), Vfs = vfs };
        }
        catch
        {
            vfs.Dispose();
            throw;
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    };
}
