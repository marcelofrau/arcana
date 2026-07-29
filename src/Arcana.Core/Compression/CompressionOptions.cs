using Arcana.Core.Cryptography;

namespace Arcana.Core.Compression;

public class CompressionOptions
{
    public CompressionFormat Format { get; set; } = CompressionFormat.Zip;
    public CompressionLevel Level { get; set; } = CompressionLevel.Normal;
    public EncryptionOptions? Encryption { get; set; }
    public bool EnableParallel { get; set; } = true;
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public int SolidBlockSize { get; set; } = 64;
    public bool PreserveTimestamps { get; set; } = true;
    public bool IncludeHiddenFiles { get; set; } = true;
}
