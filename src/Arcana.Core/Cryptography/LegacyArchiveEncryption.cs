using Serilog;

namespace Arcana.Core.Cryptography;

/// <summary>Read compatibility for the unversioned AES-GCM envelope written by early Arcana builds.</summary>
internal static class LegacyArchiveEncryption
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(LegacyArchiveEncryption));

    public static Stream Open(Stream input, string? password, byte[] signature)
    {
        var position = input.Position;
        var header = new byte[signature.Length];
        var read = input.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        input.Position = position;
        if (read == header.Length && header.AsSpan().SequenceEqual(signature))
            return input;
        if (password == null)
            throw new SharpCompress.Common.CryptographicException("Password required for a legacy Arcana archive, or the archive is damaged.");
        if (input.Length - position < 44)
            throw new InvalidDataException("Truncated legacy encrypted archive.");
        Log.Information("Reading legacy Arcana encryption; new archives use standard format encryption");
        return new EncryptionProvider(new EncryptionOptions { Password = password }).CreateDecryptingStream(input);
    }
}
