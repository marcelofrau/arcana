namespace Arcana.Core.Tools;

public enum HashAlgorithm
{
    Md5,
    Sha1,
    Sha256,
    Sha512,
    Blake2b,
    Blake2s
}

public class HashCalculator
{
    public string ComputeHash(Stream stream, HashAlgorithm algorithm)
    {
        throw new NotImplementedException("HashCalculator.ComputeHash");
    }

    public Task<string> ComputeHashAsync(Stream stream, HashAlgorithm algorithm, CancellationToken ct = default)
    {
        throw new NotImplementedException("HashCalculator.ComputeHashAsync");
    }

    public bool VerifyHash(string filePath, string expectedHash, HashAlgorithm algorithm)
    {
        throw new NotImplementedException("HashCalculator.VerifyHash");
    }
}
