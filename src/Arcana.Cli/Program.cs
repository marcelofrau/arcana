using System.CommandLine;

namespace Arcana.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Arcana — Modern, cross-platform compression toolkit");

        rootCommand.Add(Commands.CompressCommand.Create());
        rootCommand.Add(Commands.ExtractCommand.Create());
        rootCommand.Add(Commands.ListCommand.Create());
        rootCommand.Add(Commands.SplitCommand.Create());
        rootCommand.Add(Commands.JoinCommand.Create());
        rootCommand.Add(Commands.HashCommand.Create());
        rootCommand.Add(Commands.ConvertCommand.Create());
        rootCommand.Add(Commands.BenchmarkCommand.Create());

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
