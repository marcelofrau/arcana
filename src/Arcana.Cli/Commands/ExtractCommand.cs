using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class ExtractCommand
{
    public static Command Create()
    {
        var command = new Command("extract", "Extract files from an archive");

        var archiveArg = new Argument<string>("archive") { Description = "Path to archive" };
        var outputArg = new Argument<string>("output-directory") { Description = "Extraction target directory" };
        var passwordOpt = new Option<string>("--password", "-p") { Description = "Decryption password" };
        var overwriteOpt = new Option<string>("--overwrite", "-o") { Description = "Overwrite mode" };

        command.Add(archiveArg);
        command.Add(outputArg);
        command.Add(passwordOpt);
        command.Add(overwriteOpt);

        command.SetAction((ParseResult r) =>
        {
            var archive = r.GetValue(archiveArg);
            var output = r.GetValue(outputArg);
            var password = r.GetValue(passwordOpt);
            var overwrite = r.GetValue(overwriteOpt);
            Console.WriteLine($"Extract: {archive} → {output ?? "./"}");
            // TODO: Call Arcana.Core
        });

        return command;
    }
}
