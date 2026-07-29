using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Arcana.Core.Compression;
using Arcana.Core.Compression.Formats;
using Arcana.Core.Filesystem;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class BenchmarkCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(BenchmarkCommand));
    private static readonly (string Name, CompressionFormat Format, IArchiveFormat Engine)[] Formats =
    [
        ("ZIP", CompressionFormat.Zip, new ZipEngine()),
        ("7z", CompressionFormat.SevenZip, new SevenZipEngine()),
        ("Zstd", CompressionFormat.Zstandard, new ZstdEngine()),
    ];

    public static Command Create()
    {
        var command = new Command("benchmark", "Run compression benchmarks");

        var dataOpt = new Option<string>("--data", "-d") { Description = "Data size: tiny (1K), small (1M), medium (10M)" };

        command.Add(dataOpt);

        command.SetAction(async (ParseResult r) =>
        {
            var dataSize = r.GetValue(dataOpt) ?? "tiny";

            var data = GenerateData(dataSize);
            Log.Information("Benchmark: {DataSize} data", dataSize);
            var vfs = new VirtualFileSystem();
            vfs.AddFile("benchmark.dat", new MemoryStream(data));

            var results = new List<BenchResult>();

            await Output.StartStatusAsync("Running benchmarks...",
                async ctx =>
                {
                    foreach (var (name, format, engine) in Formats)
                    {
                        ctx.Status = $"Testing [cyan]{name}[/]...";
                        Log.Debug("Benchmarking {Format}", name);
                        var result = await RunBenchmark(engine, format, data);
                        results.Add(result);
                    }
                });

            Log.Information("Benchmark complete");

            var table = new Table { Border = TableBorder.Rounded };
            table.AddColumn(new TableColumn("[grey]Format[/]"));
            table.AddColumn(new TableColumn("[grey]Size[/]").RightAligned());
            table.AddColumn(new TableColumn("[grey]Ratio[/]").RightAligned());
            table.AddColumn(new TableColumn("[grey]Time[/]").RightAligned());
            table.AddColumn(new TableColumn("[grey]Speed[/]").RightAligned());

            foreach (var r2 in results.OrderBy(x => x.CompressedSize))
            {
                var ratio = r2.OriginalSize > 0
                    ? $"{r2.CompressedSize * 100.0 / r2.OriginalSize,5:F1}%"
                    : "—";
                var speed = r2.Elapsed.TotalSeconds > 0
                    ? $"{r2.OriginalSize / (1024.0 * 1024) / r2.Elapsed.TotalSeconds,5:F1} MB/s"
                    : "—";

                table.AddRow(
                    $"[cyan]{r2.Name}[/]",
                    FormatSize(r2.CompressedSize),
                    ratio,
                    $"{r2.Elapsed.TotalSeconds,5:F2}s",
                    speed
                );
            }

            Output.MarkupLine("");
            Output.Write(table);
            return 0;
        });

        return command;
    }

    private static async Task<BenchResult> RunBenchmark(IArchiveFormat engine, CompressionFormat format,
        byte[] data)
    {
        var vfs = new VirtualFileSystem();
        vfs.AddFile("benchmark.dat", new MemoryStream(data));

        var archive = new Archive
        {
            Format = format,
            FormatEngine = engine,
            Entries = new List<ArchiveEntry>
            {
                new()
                {
                    Path = "benchmark.dat",
                    Name = "benchmark.dat",
                    Size = data.Length,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow,
                }
            },
            Vfs = vfs,
        };

        var sw = Stopwatch.StartNew();
        using var output = new MemoryStream();
        await engine.SaveAsync(archive, output, new CompressionOptions { Format = format });
        sw.Stop();

        return new BenchResult
        {
            Name = engine.Name,
            OriginalSize = data.Length,
            CompressedSize = output.Length,
            Elapsed = sw.Elapsed,
        };
    }

    private static byte[] GenerateData(string size) => size.ToLowerInvariant() switch
    {
        "tiny" or "1k" => RandomBytes(1024),
        "small" or "1m" => RandomBytes(1024 * 1024),
        "medium" or "10m" => RandomBytes(10 * 1024 * 1024),
        _ => RandomBytes(1024),
    };

    private static byte[] RandomBytes(int count)
    {
        var data = new byte[count];
        new Random(42).NextBytes(data);
        return data;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    };

    private sealed class BenchResult
    {
        public string Name { get; init; } = "";
        public long OriginalSize { get; init; }
        public long CompressedSize { get; init; }
        public TimeSpan Elapsed { get; init; }
    }
}
