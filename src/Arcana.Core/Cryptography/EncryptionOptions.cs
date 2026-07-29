using Arcana.Core.Compression;

namespace Arcana.Core.Cryptography;

public class EncryptionOptions
{
    public CipherAlgorithm Algorithm { get; set; } = CipherAlgorithm.Aes256Gcm;
    public KeyDerivationFunction Kdf { get; set; } = KeyDerivationFunction.Argon2id;
    public byte[]? Key { get; set; }
    public string? Password { get; set; }
    public int KdfMemoryMB { get; set; } = 64;
    public int KdfIterations { get; set; } = 3;
    public int KdfParallelism { get; set; } = 4;
}
