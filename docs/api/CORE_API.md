# Arcana.Core API Reference

Public interfaces and classes in `Arcana.Core`.

## Namespace: Arcana.Core

### `IArchiveFormat`

```csharp
public interface IArchiveFormat
{
    string Name { get; }
    string Extension { get; }
    bool CanRead { get; }
    bool CanWrite { get; }
    bool CanEncrypt { get; }
    bool SupportsSolid { get; }
    bool SupportsVolumes { get; }

    Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default);
    Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default);

    void Save(Archive archive, Stream stream, CompressionOptions options,
              IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                   IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
}
```

### `ICompressionEngine`

```csharp
public interface ICompressionEngine
{
    string Name { get; }
    CompressionLevel MinLevel { get; }
    CompressionLevel MaxLevel { get; }
    bool SupportsParallel { get; }

    void Compress(Stream source, Stream destination, CompressionLevel level,
                  IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    Task CompressAsync(Stream source, Stream destination, CompressionLevel level,
                       IProgress<ProgressReport>? progress = null, CancellationToken ct = default);

    void Decompress(Stream source, Stream destination,
                    IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    Task DecompressAsync(Stream source, Stream destination,
                         IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
}
```

### `Archive`

```csharp
public class Archive : IDisposable
{
    public CompressionFormat Format { get; }
    public IArchiveFormat FormatEngine { get; }
    public IReadOnlyList<ArchiveEntry> Entries { get; }
    public VirtualFileSystem Vfs { get; }
    public bool IsEncrypted { get; }
    public string? Comment { get; set; }

    public ArchiveEntry? FindEntry(string path);
    public IEnumerable<ArchiveEntry> FindEntries(string pattern);
}
```

### `ArchiveEntry`

```csharp
public class ArchiveEntry
{
    public string Path { get; }
    public string Name { get; }
    public long Size { get; }
    public long CompressedSize { get; }
    public double CompressionRatio => Size > 0 ? (double)CompressedSize / Size : 0;
    public uint Crc32 { get; }
    public string? Comment { get; }
    public bool IsEncrypted { get; }
    public bool IsDirectory { get; }
    public DateTime LastModified { get; }
    public string? Method { get; }
    public string? HostOS { get; }
    public uint Attributes { get; }
}
```

### `CompressionOptions`

```csharp
public class CompressionOptions
{
    public CompressionFormat Format { get; set; }
    public CompressionLevel Level { get; set; } = CompressionLevel.Normal;
    public EncryptionOptions? Encryption { get; set; }
    public bool EnableParallel { get; set; } = true;
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public int SolidBlockSize { get; set; } = 64; // MB, for 7z solid archives
    public bool PreserveTimestamps { get; set; } = true;
    public bool IncludeHiddenFiles { get; set; } = true;
}
```

### `CompressionLevel`

```csharp
public enum CompressionLevel
{
    Store = 0,
    Fastest = 1,
    Fast = 3,
    Normal = 5,
    Maximum = 7,
    Ultra = 9,
    Insane = 10
}
```

### `CompressionFormat`

```csharp
public enum CompressionFormat
{
    Zip,
    SevenZip,
    Zstandard,
    Brotli,
    Lz4,
    Lzma,
    Xz,
    BZip2,
    GZip,
    Tar,
    TarGz,
    TarBz2,
    TarXz,
    TarZstd
}
```

### `EncryptionOptions`

```csharp
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
```

### `CipherAlgorithm`

```csharp
public enum CipherAlgorithm
{
    Aes256Gcm,
    ChaCha20Poly1305
}
```

### `KeyDerivationFunction`

```csharp
public enum KeyDerivationFunction
{
    Argon2id
}
```

### `ProgressReport`

```csharp
public class ProgressReport
{
    public string? CurrentFile { get; init; }
    public long BytesProcessed { get; init; }
    public long TotalBytes { get; init; }
    public double Percentage => TotalBytes > 0 ? (double)BytesProcessed / TotalBytes * 100 : 0;
    public int FilesProcessed { get; init; }
    public int TotalFiles { get; init; }
    public string? CurrentOperation { get; init; }
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
}
```

### `AccessMode`

```csharp
public enum AccessMode
{
    Read,
    Write,
    ReadWrite
}
```

## Factory

```csharp
public static class ArchiveFactory
{
    public static IArchiveFormat GetFormat(CompressionFormat format);
    public static IArchiveFormat GetFormatFromExtension(string extension);
    public static IArchiveFormat GetFormatFromFileHeader(Stream stream);

    public static Archive Open(string path, AccessMode mode = AccessMode.Read);
    public static Task<Archive> OpenAsync(string path, AccessMode mode = AccessMode.Read, CancellationToken ct = default);
}
```

## Namespace: Arcana.Core.Cryptography

### `EncryptionProvider`

```csharp
public class EncryptionProvider
{
    public EncryptionProvider(EncryptionOptions options);

    public byte[] Encrypt(byte[] plaintext, byte[] associatedData);
    public byte[] Decrypt(byte[] ciphertext, byte[] associatedData);
    public Stream CreateEncryptingStream(Stream innerStream);
    public Stream CreateDecryptingStream(Stream innerStream);
}
```

## Namespace: Arcana.Core.Filesystem

### `VirtualFileSystem`

```csharp
public class VirtualFileSystem
{
    public ArchiveNode Root { get; }

    public ArchiveNode AddFile(string path, Stream content);
    public ArchiveNode AddDirectory(string path);
    public void DeleteNode(ArchiveNode node);
    public void RenameNode(ArchiveNode node, string newName);
    public IReadOnlyList<ArchiveNode> GetDirtyNodes();
    public bool HasDirtyNodes { get; }
    public void MarkAllClean();
    public ArchiveNode? FindNode(string path);
    public void ImportFromArchive(Archive archive);
}
```

### `ArchiveNode`

```csharp
public class ArchiveNode
{
    public string Name { get; set; }
    public string FullPath { get; }
    public NodeType Type { get; }
    public long OriginalSize { get; }
    public long CompressedSize { get; }
    public bool IsDirty { get; }
    public ArchiveNode? Parent { get; }
    public IReadOnlyList<ArchiveNode> Children { get; }
    public DateTime LastModified { get; set; }

    public Stream OpenRead();
    public Stream OpenWrite();
}
```

### `NodeType`

```csharp
public enum NodeType
{
    File,
    Directory
}
```

## Namespace: Arcana.Core.Tools

### `FileSplitter`

```csharp
public class FileSplitter
{
    public void Split(string sourcePath, string outputDir, long partSize,
                      IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    public Task SplitAsync(string sourcePath, string outputDir, long partSize,
                           IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
}
```

### `FileJoiner`

```csharp
public class FileJoiner
{
    public void Join(IEnumerable<string> parts, string outputPath,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    public Task JoinAsync(IEnumerable<string> parts, string outputPath,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
}
```

### `HashCalculator`

```csharp
public class HashCalculator
{
    public string ComputeHash(Stream stream, HashAlgorithm algorithm);
    public Task<string> ComputeHashAsync(Stream stream, HashAlgorithm algorithm, CancellationToken ct = default);
    public bool VerifyHash(string filePath, string expectedHash, HashAlgorithm algorithm);
}

public enum HashAlgorithm
{
    Md5,
    Sha1,
    Sha256,
    Sha512,
    Blake2b,
    Blake2s
}
```
