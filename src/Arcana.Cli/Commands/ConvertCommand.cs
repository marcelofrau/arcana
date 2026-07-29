using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class ConvertCommand
{
    public static Command Create()
    {
        var command = new Command("convert", "Convert between archive formats");

        var sourceArg = new Argument<string>("source") { Description = "Source archive" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output archive path", Required = true };
        var formatOpt = new Option<string>("--format", "-f") { Description = "Output format (auto if omitted)" };
        var levelOpt = new Option<int>("--level", "-l") { Description = "Compression level" };

        command.Add(sourceArg);
        command.Add(outputOpt);
        command.Add(formatOpt);
        command.Add(levelOpt);

        command.SetAction((ParseResult r) =>
        {
            var source = r.GetValue(sourceArg);
            var output = r.GetValue(outputOpt);
            var format = r.GetValue(formatOpt);
            var level = r.GetValue(levelOpt);
            Console.WriteLine($"Convert: {source} → {output} [{format ?? "auto"} lv{level}]");
            // TODO: Call Arcana.Core
        });

        return command;
    }
}
