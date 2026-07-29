using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class ListCommand
{
    public static Command Create()
    {
        var command = new Command("list", "List archive contents");

        var archiveArg = new Argument<string>("archive") { Description = "Path to archive" };
        var detailedOpt = new Option<bool>("--detailed", "-l") { Description = "Detailed listing" };
        var jsonOpt = new Option<bool>("--json", "-j") { Description = "JSON output" };

        command.Add(archiveArg);
        command.Add(detailedOpt);
        command.Add(jsonOpt);

        command.SetAction((ParseResult r) =>
        {
            var archive = r.GetValue(archiveArg);
            var detailed = r.GetValue(detailedOpt);
            var json = r.GetValue(jsonOpt);
            Console.WriteLine($"List: {archive} [detailed={detailed}, json={json}]");
            // TODO: Call Arcana.Core
        });

        return command;
    }
}
