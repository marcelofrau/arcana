using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using Arcana.Core.Compression;
using Arcana.Core.Tools;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class JoinCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(JoinCommand));

    public static Command Create()
    {
        var command = new Command("join", "Join split file parts");

        var partsArg = new Argument<string[]>("parts") { Description = "File parts, directory, or first part (e.g., file.001)" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output file path" };

        command.Add(partsArg);
        command.Add(outputOpt);

        command.SetAction(async (ParseResult r) =>
        {
            var raw = r.GetValue(partsArg)!;
            var output = r.GetValue(outputOpt) ?? "output";

            string[] parts;

            if (raw.Length == 1 && (!File.Exists(raw[0]) || Regex.IsMatch(Path.GetExtension(raw[0]), @"^\.[0-9]{3,4}$") || Directory.Exists(raw[0])))
            {
                try
                {
                    parts = FileJoiner.AutoDiscoverParts(raw[0]).ToArray();
                    Log.Debug("Auto-discovered {PartCount} parts", parts.Length);
                }
                catch (InvalidOperationException ex)
                {
                    Output.Error(ex.Message);
                    return 1;
                }
            }
            else
            {
                var missing = raw.Where(p => !File.Exists(p)).ToList();
                if (missing.Count > 0)
                {
                    foreach (var m in missing)
                        Output.Error($"Part not found: {m}");
                    return 1;
                }
                parts = raw;
            }

            Log.Information("Join {PartCount} parts -> {Output}", parts.Length, output);
            var totalSize = parts.Sum(p => new FileInfo(p).Length);

            await Output.StartProgressAsync($"Joining [cyan]{parts.Length}[/] parts",
                async ctx =>
                {
                    var task = ctx.AddTask("Joining", maxValue: totalSize);
                    var progress = new Progress<ProgressReport>(report =>
                    {
                        task.Value = report.BytesProcessed;
                        task.Description = report.CurrentFile ?? "Joining...";
                    });

                    try
                    {
                        var joiner = new FileJoiner();
                        await joiner.JoinAsync(parts, output, progress);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Join failed: {Message}", ex.Message);
                        throw;
                    }
                });

            Output.Success($"Created [cyan]{Path.GetFileName(output)}[/] from [cyan]{parts.Length}[/] parts");
            return 0;
        });

        return command;
    }
}
