using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class BenchmarkCommand
{
    public static Command Create()
    {
        var command = new Command("benchmark", "Run compression benchmarks");

        var formatOpt = new Option<string[]>("--format", "-f") { Description = "Formats to benchmark" };
        var dataOpt = new Option<string>("--data", "-d") { Description = "Test data set" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Save results to JSON" };

        command.Add(formatOpt);
        command.Add(dataOpt);
        command.Add(outputOpt);

        command.SetAction((ParseResult r) =>
        {
            var formats = r.GetValue(formatOpt);
            var data = r.GetValue(dataOpt);
            var output = r.GetValue(outputOpt);
            Console.WriteLine($"Benchmark: [{string.Join(", ", formats)}] on {data}");
            // TODO: Implement benchmarking
        });

        return command;
    }
}
