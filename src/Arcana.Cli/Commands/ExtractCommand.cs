using System.CommandLine;
using System.CommandLine.Parsing;
using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Filesystem;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class ExtractCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ExtractCommand));

    public static Command Create()
    {
        var command = new Command("extract", "Extract files from an archive");

        var archiveArg = new Argument<string>("archive") { Description = "Path to archive" };
        var outputArg = new Argument<string>("output-directory") { Description = "Extraction target directory" };
        var passwordOpt = new Option<string>("--password", "-p") { Description = "Decryption password" };
        var overwriteOpt = new Option<bool>("--overwrite") { Description = "Overwrite existing files" };

        command.Add(archiveArg);
        command.Add(outputArg);
        command.Add(passwordOpt);
        command.Add(overwriteOpt);

        command.SetAction(async (ParseResult r) =>
        {
            var path = r.GetValue(archiveArg)!;
            var output = r.GetValue(outputArg) ?? ".";
            Log.Information("Extract start: {ArchivePath} -> {OutputDir}", path, output);
            Directory.CreateDirectory(output);

            var archive = await ArchiveFactory.OpenAsync(path);
            var nodes = WalkFiles(archive.Vfs.Root).Where(n => n.Type == NodeType.File).ToList();

            if (nodes.Count == 0)
            {
                Output.Warning("Archive is empty");
                return 0;
            }

            await Output.StartStatusAsync($"Extracting [cyan]{Path.GetFileName(path)}[/]",
                async ctx =>
                {
                    var count = 0;
                    try
                    {
                        foreach (var node in nodes)
                        {
                            var entryPath = node.FullPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                            var destPath = Path.Combine(output, entryPath);

                            ctx.Status = $"[cyan]{node.Name}[/]";
                            Log.Debug("Extracting {EntryPath}", entryPath);

                            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                            await using var content = node.OpenRead();
                            await using var dest = File.Create(destPath);
                            await content.CopyToAsync(dest);
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Extraction failed: {Message}", ex.Message);
                        throw;
                    }
                });

            Log.Information("Extract complete: {FileCount} files", nodes.Count);
            Output.Success($"Extracted [cyan]{nodes.Count}[/] {(nodes.Count == 1 ? "file" : "files")} to [cyan]{output}[/]");
            return 0;
        });

        return command;
    }

    private static IEnumerable<ArchiveNode> WalkFiles(ArchiveNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        foreach (var descendant in WalkFiles(child))
            yield return descendant;
    }
}
