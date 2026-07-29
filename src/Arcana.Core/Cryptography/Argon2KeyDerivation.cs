namespace Arcana.Core.Cryptography;

public static class Argon2KeyDerivation
{
    public static byte[] DeriveKey(string password, byte[] salt, int keyLength,
                                   int memoryMB = 64, int iterations = 3, int parallelism = 4)
    {
        throw new NotImplementedException("Argon2KeyDerivation.DeriveKey");
    }

    public static byte[] GenerateSalt(int length = 16)
    {
        var salt = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);
        return salt;
    }
}
