using System.CommandLine;
using System.CommandLine.Parsing;
using Arcana.Core.Compression;
using Arcana.Core.Tools;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class SplitCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SplitCommand));

    public static Command Create()
    {
        var command = new Command("split", "Split a file into parts");

        var fileArg = new Argument<string>("file") { Description = "File to split" };
        var partSizeOpt = new Option<string>("--part-size", "-s") { Description = "Part size (e.g., 10M, 100M, 1G)" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output directory" };
        var hjsplitOpt = new Option<bool>("--hjsplit", "HJSplit-compatible naming (file.001, file.002...)");

        command.Add(fileArg);
        command.Add(partSizeOpt);
        command.Add(outputOpt);
        command.Add(hjsplitOpt);

        command.SetAction(async (ParseResult r) =>
        {
            var file = r.GetValue(fileArg)!;
            var partSizeStr = r.GetValue(partSizeOpt) ?? "100M";
            var outputDir = r.GetValue(outputOpt) ?? ".";
            var hjsplit = r.GetValue(hjsplitOpt);

            if (!File.Exists(file))
            {
                Output.Error($"File not found: {file}");
                return 1;
            }

            var partSize = ParseSize(partSizeStr);
            var fi = new FileInfo(file);
            var totalParts = (int)Math.Ceiling((double)fi.Length / partSize);
            Log.Information("Split {File} into {PartCount} parts ({PartSize})", file, totalParts, partSizeStr);

            await Output.StartProgressAsync($"Splitting [cyan]{Path.GetFileName(file)}[/] into {totalParts} parts",
                async ctx =>
                {
                    var task = ctx.AddTask("Splitting", maxValue: fi.Length);
                    var progress = new Progress<ProgressReport>(report =>
                    {
                        task.Value = report.BytesProcessed;
                        task.Description = report.CurrentFile ?? "Splitting...";
                    });

                    try
                    {
                        var splitter = new FileSplitter();
                        await splitter.SplitAsync(file, outputDir, partSize, progress, hjsplitMode: hjsplit);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Split failed: {Message}", ex.Message);
                        throw;
                    }
                });

            var suffix = hjsplit ? " (HJSplit format)" : "";
            Output.Success($"Split into [cyan]{totalParts}[/] parts in [cyan]{outputDir}[/]{suffix}");
            return 0;
        });

        return command;
    }

    private static long ParseSize(string value)
    {
        value = value.Trim().ToUpperInvariant();
        var multiplier = value switch
        {
            _ when value.EndsWith('G') => 1L << 30,
            _ when value.EndsWith('M') => 1L << 20,
            _ when value.EndsWith('K') => 1L << 10,
            _ => 1,
        };
        var numStr = multiplier > 1 ? value[..^1] : value;
        return long.TryParse(numStr, out var num) ? num * multiplier : 100 * (1L << 20);
    }
}
