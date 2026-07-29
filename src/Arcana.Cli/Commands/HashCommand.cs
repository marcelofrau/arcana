using System.CommandLine;
using System.CommandLine.Parsing;
using Arcana.Core.Tools;
using Serilog;
using Spectre.Console;

namespace Arcana.Cli.Commands;

public static class HashCommand
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HashCommand));

    public static Command Create()
    {
        var command = new Command("hash", "Compute file checksums");

        var fileArg = new Argument<string[]>("file") { Description = "Files to hash" };
        var algorithmOpt = new Option<string>("--algorithm", "-a") { Description = "MD5, SHA1, SHA256 (default), SHA512" };
        var verifyOpt = new Option<FileInfo>("--verify") { Description = "Verify checksums from file" };

        command.Add(fileArg);
        command.Add(algorithmOpt);
        command.Add(verifyOpt);

        command.SetAction(async (ParseResult r) =>
        {
            var files = r.GetValue(fileArg);
            var algoName = r.GetValue(algorithmOpt) ?? "SHA256";
            var verify = r.GetValue(verifyOpt);

            var algorithm = ParseAlgorithm(algoName);
            var calc = new HashCalculator();

            if (verify != null)
            {
                VerifyFromFile(calc, verify, algorithm);
                return 0;
            }

            foreach (var file in files!)
            {
                if (!File.Exists(file))
                {
                    Log.Warning("File not found: {File}", file);
                    Output.Error($"File not found: {file}");
                    continue;
                }

                Log.Information("Hashing {File} with {Algorithm}", file, algoName);
                var hash = await Output.StartStatusAsync(
                    $"Hashing [cyan]{Path.GetFileName(file)}[/]",
                    async ctx =>
                    {
                        await using var stream = File.OpenRead(file);
                        return await calc.ComputeHashAsync(stream, algorithm);
                    });

                Output.MarkupLine($"[green]{hash}[/]  [dim]{file}[/]");
            }

            return 0;
        });

        return command;
    }

    private static void VerifyFromFile(HashCalculator calc, FileInfo checksumFile, Core.Tools.HashAlgorithm algorithm)
    {
        foreach (var line in File.ReadLines(checksumFile.FullName))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var parts = trimmed.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var expected = parts[0];
            var filePath = parts[1];

            if (!File.Exists(filePath))
            {
                Output.Error($"File not found: {filePath}");
                continue;
            }

            var ok = calc.VerifyHash(filePath, expected, algorithm);
            Log.Information(ok ? "{File} verified OK" : "{File} verification FAILED", filePath);
            if (ok)
                Output.MarkupLine($"[green]✓[/] {filePath}");
            else
                Output.MarkupLine($"[red]✗[/] {filePath} [dim](expected {expected})[/]");
        }
    }

    private static Core.Tools.HashAlgorithm ParseAlgorithm(string name) => name.ToLowerInvariant() switch
    {
        "md5" => Core.Tools.HashAlgorithm.Md5,
        "sha1" => Core.Tools.HashAlgorithm.Sha1,
        "sha256" => Core.Tools.HashAlgorithm.Sha256,
        "sha512" => Core.Tools.HashAlgorithm.Sha512,
        _ => Core.Tools.HashAlgorithm.Sha256,
    };
}
