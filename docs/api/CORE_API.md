# Arcana.Core API Reference

Public interfaces and classes in `Arcana.Core`, as implemented. Signatures verified against source (2026-07-31).

## Namespace: `Arcana.Core.Compression`

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

    public ArchiveEntry? FindEntry(string path);                    // OrdinalIgnoreCase
    public IEnumerable<ArchiveEntry> FindEntries(string pattern);   // name contains
    public void SyncNodeMetadata();                                 // entry sizes → VFS nodes
}
```

### `ArchiveEntry`

```csharp
public class ArchiveEntry
{
    public string Path { get; init; }
    public string Name { get; init; }
    public long Size { get; init; }
    public long CompressedSize { get; init; }
    public double CompressionRatio { get; init; }   // CompressedSize / Size
    public uint Crc32 { get; init; }
    public string? Comment { get; init; }
    public bool IsEncrypted { get; init; }
    public bool IsDirectory { get; init; }
    public DateTime LastModified { get; init; }
    public string? Method { get; init; }
    public string? HostOS { get; init; }
    public uint Attributes { get; init; }
}
```

### `CompressionOptions`

```csharp
public class CompressionOptions
{
    public CompressionFormat Format { get; set; } = CompressionFormat.Zip;
    public CompressionLevel Level { get; set; } = CompressionLevel.Normal;
    public EncryptionOptions? Encryption { get; set; }
    public bool EnableParallel { get; set; } = true;
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public int SolidBlockSize { get; set; } = 64;   // MB, 7z solid
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
    TarZstd,
    Rar,
    Ace,
    Arj,
    Cab,
    Lzh,
    Hawkynt,
    Snappy
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

### `ProgressReport`

```csharp
public class ProgressReport
{
    public string? CurrentFile { get; init; }
    public long BytesProcessed { get; init; }
    public long TotalBytes { get; init; }
    public double Percentage { get; init; }
    public int FilesProcessed { get; init; }
    public int TotalFiles { get; init; }
    public string? CurrentOperation { get; init; }
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
}
```

## Namespace: `Arcana.Core.Compression` — Factory

### `ArchiveFactory` (static)

```csharp
public static class ArchiveFactory
{
    public static IArchiveFormat GetFormat(CompressionFormat format);
    public static IArchiveFormat GetFormatFromExtension(string extension);   // default → HawkyntFallback
    public static IArchiveFormat GetFormatFromFileHeader(Stream stream);     // 8-byte magic; throws NotSupportedException

    public static Archive Open(string path, string? password = null, AccessMode mode = AccessMode.Read);
    public static Task<Archive> OpenAsync(string path, string? password = null,
                                          AccessMode mode = AccessMode.Read, CancellationToken ct = default);
    public static IArchiveFormat GetFormatFromPathOrHeader(string path, Stream stream); // tar routing + fallback

    public static void SetPassword(Archive archive, string? password);
}
```

Detection order: extension → magic bytes → Hawkynt `FormatRegistry` fallback. Tar streams are routed by basename (`.tar.gz`, `.tar.bz2`, `.tar.xz`, `.tar.zst`).

## Namespace: `Arcana.Core.Cryptography`

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
    Aes256Gcm,          // ✅ implemented
    ChaCha20Poly1305    // ❌ declared, not implemented
}
```

### `KeyDerivationFunction`

```csharp
public enum KeyDerivationFunction
{
    Argon2id
}
```

### `EncryptionProvider`

```csharp
public class EncryptionProvider
{
    public EncryptionProvider(EncryptionOptions options);

    public byte[] Encrypt(byte[] plaintext, byte[] associatedData);
    public byte[] Decrypt(byte[] ciphertext, byte[] associatedData);

    public Stream CreateEncryptingStream(Stream innerStream);  // prepends salt (16 B) when password-derived
    public Stream CreateDecryptingStream(Stream innerStream);  // strips salt (16 B)
}
```

AES-256-GCM with 12-byte nonce and 16-byte tag. Whole-buffer buffering.

### `Argon2KeyDerivation`

```csharp
public static class Argon2KeyDerivation
{
    public static byte[] DeriveKey(string password, byte[] salt,
                                   int memoryMB = 64, int iterations = 3, int parallelism = 4);
    public static byte[] GenerateSalt(int size = 16);
}
```

## Namespace: `Arcana.Core.Filesystem`

### `VirtualFileSystem`

```csharp
public class VirtualFileSystem
{
    public ArchiveNode Root { get; }
    public bool HasDirtyNodes { get; }

    public ArchiveNode AddFile(string path, Stream content);
    public ArchiveNode AddDirectory(string path);
    public void DeleteNode(ArchiveNode node);
    public void RenameNode(ArchiveNode node, string newName);
    public IReadOnlyList<ArchiveNode> GetDirtyNodes();
    public void MarkAllClean();
    public ArchiveNode? FindNode(string path);   // case-insensitive walk
    public void ImportFromArchive(Archive archive);  // NOTE: nodes get empty streams
}
```

### `ArchiveNode`

```csharp
public class ArchiveNode
{
    public string Name { get; set; }
    public string FullPath { get; }
    public NodeType Type { get; init; }
    public long OriginalSize { get; }
    public long CompressedSize { get; }
    public bool IsDirty { get; }
    public ArchiveNode? Parent { get; }
    public IReadOnlyList<ArchiveNode> Children { get; }
    public IReadOnlyList<ArchiveNode> ChildFolders { get; }   // directory-only subset
    public DateTime LastModified { get; set; }

    public Stream OpenRead();    // lazy via ContentFactory
    public Stream OpenWrite();   // MemoryStream + factory replace
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

## Namespace: `Arcana.Core.Tools`

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

Naming: HJSplit mode → `{name}.{i:D3}`; normal mode → `{name}.part{i:D3}{ext}`.

### `FileJoiner`

```csharp
public class FileJoiner
{
    public IReadOnlyList<string> AutoDiscoverParts(string firstPart);   // scan or `.{3,4}` suffix
    public void Join(IEnumerable<string> parts, string outputPath,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    public Task JoinAsync(IEnumerable<string> parts, string outputPath,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
}
```

### `HashCalculator`

```csharp
public enum HashAlgorithm
{
    Md5,
    Sha1,
    Sha256,
    Sha512
}

public class HashCalculator
{
    public string ComputeHash(Stream stream, HashAlgorithm algorithm);
    public Task<string> ComputeHashAsync(Stream stream, HashAlgorithm algorithm, CancellationToken ct = default);
    public bool VerifyHash(string filePath, string expectedHash, HashAlgorithm algorithm);
}
```

### `ImageConverter` / `BatchProcessor`

```csharp
public class ImageConverter    // STUB
{
    public Task ConvertAsync(string source, string destination, ...);   // throws NotImplementedException
}

public class BatchProcessor    // STUB
{
    public Task ProcessBatchAsync(...);                                  // throws NotImplementedException
}
```

## Namespace: `Arcana.Core.Logging`

### `LogConfig`

```csharp
public static class LogConfig
{
    public static void Init();              // console + rolling file in %AppData%\Arcana\logs
    public static void SetLevel(string level);   // trace | debug | info | warning | error | fatal
}
```

Default level: Warning. File sink: `arcana-YYYYMMDD.log`.
