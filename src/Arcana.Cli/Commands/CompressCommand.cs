using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class CompressCommand
{
    public static Command Create()
    {
        var command = new Command("compress", "Compress files and directories into an archive");

        var sourceArg = new Argument<string[]>("source") { Description = "Files/directories to compress" };
        var outputOpt = new Option<string>("--output", "-o") { Description = "Output archive path", Required = true };
        var formatOpt = new Option<string>("--format", "-f") { Description = "Archive format" };
        var levelOpt = new Option<int>("--level", "-l") { Description = "Compression level (0-9)" };
        var passwordOpt = new Option<string>("--password", "-p") { Description = "Encryption password" };

        command.Add(sourceArg);
        command.Add(outputOpt);
        command.Add(formatOpt);
        command.Add(levelOpt);
        command.Add(passwordOpt);

        command.SetAction((ParseResult r) =>
        {
            var sources = r.GetValue(sourceArg);
            var output = r.GetValue(outputOpt);
            var format = r.GetValue(formatOpt);
            var level = r.GetValue(levelOpt);
            var password = r.GetValue(passwordOpt);

            Console.WriteLine($"Compress: {string.Join(", ", sources)} → {output} [{format} lv{level}]");
            // TODO: Call Arcana.Core
        });

        return command;
    }
}
