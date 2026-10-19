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
        var outputArg = new Argument<string>("output-directory") { Description = "Extraction target directory", DefaultValueFactory = _ => "." };
        var passwordOpt = new Option<string>("--password", "-p") { Description = "Decryption password" };
        var overwriteOpt = new Option<bool>("--overwrite") { Description = "Overwrite existing files" };

        command.Add(archiveArg);
        command.Add(outputArg);
        command.Add(passwordOpt);
        command.Add(overwriteOpt);

        command.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var path = r.GetValue(archiveArg)!;
            var output = r.GetValue(outputArg) ?? ".";
            Log.Information("Extract start: {ArchivePath} -> {OutputDir}", path, output);
            using var archive = await ArchiveFactory.OpenAsync(path, r.GetValue(passwordOpt), ct: ct);
            ExtractResult? result = null;

            await Output.StartStatusAsync($"Extracting [cyan]{Markup.Escape(Path.GetFileName(path))}[/]",
                async ctx =>
                {
                    try
                    {
                        result = await ArchiveExtractor.ExtractAsync([archive.Vfs.Root], output, r.GetValue(overwriteOpt), ct: ct);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Extraction failed: {Message}", ex.Message);
                        throw;
                    }
                });

            Log.Information("Extract complete: {Extracted} extracted, {Skipped} skipped", result!.Extracted + result.Overwritten, result.Skipped);
            Output.Success($"Extracted [cyan]{result.Extracted + result.Overwritten}[/] files to [cyan]{Markup.Escape(output)}[/]; skipped {result.Skipped}");
            return 0;
        });

        return command;
    }

}
