using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class SplitCommand
{
    public static Command Create()
    {
        var command = new Command("split", "Split a file into parts");

        var fileArg = new Argument<string>("file") { Description = "File to split" };
        var partSizeOpt = new Option<string>("--part-size", "-s") { Description = "Part size (e.g., 10M, 100M, 1G)" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output directory" };

        command.Add(fileArg);
        command.Add(partSizeOpt);
        command.Add(outputOpt);

        command.SetAction((ParseResult r) =>
        {
            var file = r.GetValue(fileArg);
            var partSize = r.GetValue(partSizeOpt);
            var output = r.GetValue(outputOpt);
            Console.WriteLine($"Split: {file} → {output ?? "./"} [{partSize}]");
            // TODO: Call Arcana.Core.Tools.FileSplitter
        });

        return command;
    }
}
