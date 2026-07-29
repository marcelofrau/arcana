using Konscious.Security.Cryptography;
using System.Security.Cryptography;

namespace Arcana.Core.Cryptography;

public static class Argon2KeyDerivation
{
    public static byte[] DeriveKey(string password, byte[] salt, int keyLength,
                                   int memoryMB = 64, int iterations = 3, int parallelism = 4)
    {
        using var argon2 = new Argon2id(
            System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryMB * 1024,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        return argon2.GetBytes(keyLength);
    }

    public static byte[] GenerateSalt(int length = 16)
    {
        var salt = new byte[length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }
}
