using System.CommandLine;
using System.CommandLine.Parsing;
using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class ListCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ListCommand));

    public static Command Create()
    {
        var command = new Command("list", "List archive contents");

        var archiveArg = new Argument<string>("archive") { Description = "Path to archive" };
        var detailedOpt = new Option<bool>("--detailed", "-l") { Description = "Detailed listing" };

        command.Add(archiveArg);
        command.Add(detailedOpt);

        command.SetAction(async (ParseResult r) =>
        {
            var path = r.GetValue(archiveArg)!;
            var detailed = r.GetValue(detailedOpt);

            var archive = await ArchiveFactory.OpenAsync(path);

            Log.Debug("Listing {Archive} ({EntryCount} entries)", path, archive.Entries.Count);
            Log.Information("Format detected: {Format}", archive.Format);

            if (!archive.Entries.Any())
            {
                Output.Warning("Archive is empty");
                return 0;
            }

            if (detailed)
            {
                var table = new Table { Border = TableBorder.Rounded };
                table.AddColumn(new TableColumn("[grey]Date[/]").RightAligned());
                table.AddColumn(new TableColumn("[grey]Size[/]").RightAligned());
                table.AddColumn(new TableColumn("[grey]Compressed[/]").RightAligned());
                table.AddColumn(new TableColumn("[grey]Ratio[/]").RightAligned());
                table.AddColumn(new TableColumn("[grey]Attrs[/]").Centered());
                table.AddColumn(new TableColumn("[grey]Name[/]"));

                foreach (var entry in archive.Entries.OrderBy(e => e.Path))
                {
                    var date = entry.LastModified > new DateTime(2000, 1, 1)
                        ? entry.LastModified.ToString("yyyy-MM-dd HH:mm")
                        : "[dim]—[/]";
                    var size = entry.IsDirectory ? "[dim]—[/]" : FormatSize(entry.Size);
                    var compressed = entry.IsDirectory ? "[dim]—[/]" : FormatSize(entry.CompressedSize);
                    var ratio = entry is { IsDirectory: false, CompressedSize: > 0 }
                        ? $"{entry.CompressedSize * 100.0 / Math.Max(entry.Size, 1),5:F1}%"
                        : "[dim]—[/]";
                    var attrs = $"{(entry.IsDirectory ? "[blue]d[/]" : " ")}{(entry.IsEncrypted ? "[yellow]e[/]" : " ")}";

                    var name = entry.IsDirectory
                        ? $"[cyan]{entry.Path}/[/]"
                        : entry.Path;

                    table.AddRow(date, size, compressed, ratio, attrs, name);
                }

                var files = archive.Entries.Count(e => !e.IsDirectory);
                var dirs = archive.Entries.Count(e => e.IsDirectory);
                var totalSize = archive.Entries.Sum(e => e.Size);
                var totalCompressed = archive.Entries.Sum(e => e.CompressedSize);

                table.Caption = new TableTitle(
                    $"[bold]{files}[/] {(files == 1 ? "file" : "files")}, " +
                    $"[bold]{dirs}[/] {(dirs == 1 ? "dir" : "dirs")} — " +
                    $"[dim]{FormatSize(totalSize)} → {FormatSize(totalCompressed)}[/]"
                );

                Output.Write(table);
            }
            else
            {
                foreach (var entry in archive.Entries.OrderBy(e => e.Path))
                {
                    if (entry.IsDirectory)
                        Output.MarkupLine($"[cyan]{entry.Path}/[/]");
                    else
                        Output.MarkupLine($"{entry.Path}");
                }

                var fileCount = archive.Entries.Count(e => !e.IsDirectory);
                var dirCount = archive.Entries.Count(e => e.IsDirectory);
                Output.MarkupLine("");
                Output.MarkupLine(
                    $"[bold]{fileCount}[/] {(fileCount == 1 ? "file" : "files")}, " +
                    $"[bold]{dirCount}[/] {(dirCount == 1 ? "dir" : "dirs")}, " +
                    $"[dim]{FormatSize(archive.Entries.Sum(e => e.Size))}[/]"
                );
            }

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
