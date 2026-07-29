using System.CommandLine;
using System.CommandLine.Parsing;

namespace Arcana.Cli.Commands;

public static class HashCommand
{
    public static Command Create()
    {
        var command = new Command("hash", "Compute file checksums");

        var fileArg = new Argument<string[]>("file") { Description = "Files to hash" };
        var algorithmOpt = new Option<string>("--algorithm", "-a") { Description = "Hash algorithm" };
        var verifyOpt = new Option<FileInfo>("--verify") { Description = "Verify checksums from file" };

        command.Add(fileArg);
        command.Add(algorithmOpt);
        command.Add(verifyOpt);

        command.SetAction((ParseResult r) =>
        {
            var files = r.GetValue(fileArg);
            var algorithm = r.GetValue(algorithmOpt);
            var verify = r.GetValue(verifyOpt);
            Console.WriteLine($"Hash: {string.Join(", ", files)} [{algorithm}]");
            // TODO: Call Arcana.Core.Tools.HashCalculator
        });

        return command;
    }
}
