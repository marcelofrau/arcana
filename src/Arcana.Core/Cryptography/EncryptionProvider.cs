namespace Arcana.Core.Cryptography;

public class EncryptionProvider
{
    private readonly EncryptionOptions _options;

    public EncryptionProvider(EncryptionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Encrypt(byte[] plaintext, byte[] associatedData)
    {
        throw new NotImplementedException("EncryptionProvider.Encrypt");
    }

    public byte[] Decrypt(byte[] ciphertext, byte[] associatedData)
    {
        throw new NotImplementedException("EncryptionProvider.Decrypt");
    }

    public Stream CreateEncryptingStream(Stream innerStream)
    {
        throw new NotImplementedException("EncryptionProvider.CreateEncryptingStream");
    }

    public Stream CreateDecryptingStream(Stream innerStream)
    {
        throw new NotImplementedException("EncryptionProvider.CreateDecryptingStream");
    }
}
