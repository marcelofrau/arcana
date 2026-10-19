using System.CommandLine;
using System.CommandLine.Parsing;
using Arcana.Core.Compression;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class ConvertCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ConvertCommand));

    public static Command Create()
    {
        var command = new Command("convert", "Convert between archive formats");

        var sourceArg = new Argument<string>("source") { Description = "Source archive" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output archive path", Required = true };
        var formatOpt = new Option<string>("--format", "-f") { Description = "Output format (auto if omitted)" };
        var levelOpt = new Option<int>("--level", "-l") { Description = "Compression level", DefaultValueFactory = _ => 5 };
        var passwordOpt = new Option<string>("--password", "-p") { Description = "Source archive password (preserved on output)" };

        command.Add(sourceArg);
        command.Add(outputOpt);
        command.Add(formatOpt);
        command.Add(levelOpt);
        command.Add(passwordOpt);

        command.SetAction(async (ParseResult r, CancellationToken ct) =>
        {
            var source = r.GetValue(sourceArg)!;
            var output = r.GetValue(outputOpt)!;
            var formatName = r.GetValue(formatOpt);
            var level = r.GetValue(levelOpt);

            if (!File.Exists(source))
            {
                Output.Error($"File not found: {source}");
                return 1;
            }

            var sourceFormat = ArchiveFactory.GetFormatFromExtension(Path.GetExtension(source)).Name;
            var targetFormatName = ArchiveFactory.GetFormatFromExtension(
                formatName != null ? $".{formatName}" : Path.GetExtension(output)).Name;
            Log.Information("Convert {Source} ({SourceFormat}) -> {Output} ({TargetFormat})",
                source, sourceFormat, output, targetFormatName);

            await Output.StartStatusAsync($"Converting [cyan]{Markup.Escape(Path.GetFileName(source))}[/]",
                async ctx =>
                {
                    try
                    {
                        using var sourceArchive = await ArchiveFactory.OpenAsync(source, r.GetValue(passwordOpt), ct: ct);

                        var targetFormat = formatName != null
                            ? ArchiveFormats.Parse(formatName) : ArchiveFormats.FromPath(output);

                        var options = new CompressionOptions
                        {
                            Format = targetFormat,
                            Level = (CompressionLevel)Math.Clamp(level, 0, 10),
                        };

                        await ArchiveWriter.SaveAsync(sourceArchive, output, options, ct: ct);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Conversion failed: {Message}", ex.Message);
                        throw;
                    }
                });

            var fi = new FileInfo(output);
            Output.Success($"Created [cyan]{Markup.Escape(Path.GetFileName(output))}[/] [dim]({FormatSize(fi.Length)})[/]");
            return 0;
        });

        return command;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    };
}
