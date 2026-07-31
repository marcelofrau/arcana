using System.CommandLine;
using System.CommandLine.Parsing;
using Arcana.Core.Logging;
using Serilog;

namespace Arcana.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        LogConfig.Init();

        if (args.Any(a => a == "--no-color"))
            Output.SetNoColor(true);

        var noColorOpt = new Option<bool>("--no-color") { Description = "Disable colored output", Recursive = true };
        var logLevelOpt = new Option<string>("--log-level") { Description = "Log level: trace, debug, info, warn, error, fatal" };

        var rootCommand = new RootCommand("Arcana — Modern, cross-platform compression toolkit");
        rootCommand.Add(noColorOpt);
        rootCommand.Add(logLevelOpt);

        rootCommand.Add(Commands.CompressCommand.Create());
        rootCommand.Add(Commands.ExtractCommand.Create());
        rootCommand.Add(Commands.ListCommand.Create());
        rootCommand.Add(Commands.SplitCommand.Create());
        rootCommand.Add(Commands.JoinCommand.Create());
        rootCommand.Add(Commands.HashCommand.Create());
        rootCommand.Add(Commands.ConvertCommand.Create());
        rootCommand.Add(Commands.BenchmarkCommand.Create());

        var parseResult = rootCommand.Parse(args);

        var logLevel = parseResult.GetValue(logLevelOpt);
        if (logLevel != null)
            LogConfig.SetLevel(logLevel);
        Log.Information("Arcana CLI invoked with {ArgCount} argument(s), log level {Level}",
            args.Length, logLevel ?? "default");

        var start = DateTime.UtcNow;
        var result = await parseResult.InvokeAsync();
        Log.Information("Command finished in {Elapsed} ms with exit code {ExitCode}",
            (DateTime.UtcNow - start).TotalMilliseconds, result);

        Log.CloseAndFlush();
        return result;
    }
}
