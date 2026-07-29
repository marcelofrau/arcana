using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class JoinCommand
{
    public static Command Create()
    {
        var command = new Command("join", "Join split file parts");

        var partsArg = new Argument<string[]>("parts") { Description = "File parts to join" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output file path" };

        command.Add(partsArg);
        command.Add(outputOpt);

        command.SetAction((ParseResult r) =>
        {
            var parts = r.GetValue(partsArg);
            var output = r.GetValue(outputOpt);
            Console.WriteLine($"Join: {string.Join(", ", parts)} → {output ?? "./output"}");
            // TODO: Call Arcana.Core.Tools.FileJoiner
        });

        return command;
    }
}
