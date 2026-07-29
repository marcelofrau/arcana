using System.Security.Cryptography;
using Serilog;

namespace Arcana.Core.Tools;

public enum HashAlgorithm
{
    Md5,
    Sha1,
    Sha256,
    Sha512,
}

public class HashCalculator
{
    private readonly ILogger _log = Serilog.Log.ForContext<HashCalculator>();

    public string ComputeHash(Stream stream, HashAlgorithm algorithm)
    {
        _log.Debug("Computing {Algorithm} hash", algorithm);
        using var algo = CreateAlgorithm(algorithm);
        var hash = algo.ComputeHash(stream);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        _log.Information("Hash complete: {Hash}", hex);
        return hex;
    }

    public async Task<string> ComputeHashAsync(Stream stream, HashAlgorithm algorithm, CancellationToken ct = default)
    {
        _log.Debug("Computing {Algorithm} hash", algorithm);
        using var algo = CreateAlgorithm(algorithm);
        _log.Verbose("Hashing progress started");
        var hash = await algo.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        _log.Information("Hash complete: {Hash}", hex);
        return hex;
    }

    public bool VerifyHash(string filePath, string expectedHash, HashAlgorithm algorithm)
    {
        _log.Debug("Verifying {Algorithm} hash for {Path}", algorithm, filePath);
        using var stream = File.OpenRead(filePath);
        var computed = ComputeHash(stream, algorithm);
        return string.Equals(computed, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static System.Security.Cryptography.HashAlgorithm CreateAlgorithm(HashAlgorithm algorithm)
    {
        return algorithm switch
        {
            Tools.HashAlgorithm.Md5 => MD5.Create(),
            Tools.HashAlgorithm.Sha1 => SHA1.Create(),
            Tools.HashAlgorithm.Sha256 => SHA256.Create(),
            Tools.HashAlgorithm.Sha512 => SHA512.Create(),
            _ => SHA256.Create(),
        };
    }
}
