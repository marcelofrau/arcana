# Architecture

## System Context (C4 Level 1)

```mermaid
C4Context
  title System Context diagram for Arcana
  Person(user, "User", "Person who compresses, extracts, and manages archives")
  System_Boundary(arcana, "Arcana") {
    System(gui, "Arcana.App", "Desktop GUI application", "Avalonia UI")
    System(cli, "Arcana.Cli", "Command-line interface", "System.CommandLine")
    System(mobile, "Arcana.Mobile", "Mobile application", "Avalonia UI (future)")
  }
  System_Ext(fs, "File System", "Local files and directories")
  System_Ext(shell, "OS Shell", "File manager context menu integration")

  Rel(user, gui, "Uses", "GUI")
  Rel(user, cli, "Uses", "CLI")
  Rel(gui, fs, "Reads/Writes files")
  Rel(cli, fs, "Reads/Writes files")
  Rel(gui, shell, "Integrates with", "Context menu")
```

## Container Diagram (C4 Level 2)

```mermaid
C4Container
  title Container diagram for Arcana
  Person(user, "User")

  System_Boundary(arcana, "Arcana") {
    Container(gui, "Arcana.App", "Avalonia UI", "Desktop GUI")
    Container(cli, "Arcana.Cli", "System.CommandLine", "CLI tool")
    Container(mobile, "Arcana.Mobile", "Avalonia UI", "Mobile app (future)")
    Container(core, "Arcana.Core", ".NET Library", "Compression, crypto, VFS, tools")
    ContainerDb(vfs, "VirtualFileSystem", "In-memory", "Archive tree with dirty tracking")
  }

  Rel(user, gui, "Uses")
  Rel(user, cli, "Uses")
  Rel(gui, core, "Calls", "In-process")
  Rel(cli, core, "Calls", "In-process")
  Rel(mobile, core, "Calls", "In-process")
  Rel(core, vfs, "Manages")
```

## Layer Diagram

```mermaid
graph TD
    subgraph "Presentation"
        App[Arcana.App<br/>Avalonia UI]
        Cli[Arcana.Cli<br/>System.CommandLine]
        Mobile[Arcana.Mobile<br/>Avalonia UI]
    end

    subgraph "Services"
        ArchiveSvc[ArchiveService]
        PreviewSvc[PreviewService]
    end

    subgraph "Core"
        Compression[Compression Engine]
        Crypto[Cryptography]
        VFS[VirtualFileSystem]
        Tools[Utilities]
    end

    App --> ArchiveSvc
    App --> PreviewSvc
    Cli --> ArchiveSvc
    ArchiveSvc --> Compression
    ArchiveSvc --> Crypto
    ArchiveSvc --> VFS
    ArchiveSvc --> Tools
    PreviewSvc --> VFS
    Mobile --> ArchiveSvc
    Mobile --> PreviewSvc
```

## Class Diagram (Core)

```mermaid
classDiagram
    class IArchiveFormat {
        <<interface>>
        +string Name
        +bool CanRead
        +bool CanWrite
        +bool CanEncrypt
        +Task<Archive> OpenAsync(Stream, AccessMode, CancellationToken)
        +Task SaveAsync(Archive, Stream, CompressionOptions, IProgress, CancellationToken)
    }

    class ICompressionEngine {
        <<interface>>
        +Task CompressAsync(Stream, Stream, CompressionLevel, IProgress, CancellationToken)
        +Task DecompressAsync(Stream, Stream, IProgress, CancellationToken)
    }

    class VirtualFileSystem {
        +ArchiveNode Root
        +Task<ArchiveNode> AddFileAsync(string path, Stream content)
        +Task DeleteNodeAsync(ArchiveNode node)
        +Task RenameNodeAsync(ArchiveNode node, string newName)
        +IReadOnlyList<ArchiveNode> GetDirtyNodes()
        +void MarkClean()
    }

    class ArchiveNode {
        +string Name
        +string FullPath
        +NodeType Type
        +long OriginalSize
        +long CompressedSize
        +DateTime LastModified
        +bool IsDirty
        +Stream OpenRead()
        +Stream OpenWrite()
    }

    class ArchiveEntry {
        +string Path
        +long Size
        +long CompressedSize
        +uint Crc32
        +string? Comment
        +bool IsEncrypted
        +string? Method
    }

    class CompressionOptions {
        +CompressionFormat Format
        +CompressionLevel Level
        +EncryptionOptions? Encryption
        +bool EnableParallel
        +int ThreadCount
        +int SolidBlockSize
    }

    class EncryptionOptions {
        +CipherAlgorithm Algorithm
        +KeyDerivationFunction Kdf
        +byte[] Key
        +byte[]? Salt
        +int MemoryCost
        +int Iterations
        +int Parallelism
    }

    IArchiveFormat --> CompressionOptions
    IArchiveFormat --> VirtualFileSystem
    VirtualFileSystem --> ArchiveNode
    CompressionOptions --> EncryptionOptions
```

## Compression Pipeline

```mermaid
flowchart LR
    A[Source Files] --> B[FileReader]
    B --> C{Format?}
    C -->|ZIP| D[ZipEngine]
    C -->|7z| E[SevenZipEngine]
    C -->|Zstd| F[ZstdEngine]
    C -->|Brotli| G[BrotliEngine]
    D --> H[Parallel Chunk Processor]
    E --> H
    F --> H
    G --> H
    H --> I[Stream Encryptor]
    I --> J[Stream Writer]
    J --> K[Output Archive]
```

## Decompression Pipeline

```mermaid
flowchart LR
    A[Input Archive] --> B[Stream Reader]
    B --> C{Encrypted?}
    C -->|Yes| D[Stream Decryptor]
    C -->|No| E[Format Detector]
    D --> E
    E --> F{Format}
    F -->|ZIP| G[Zip Engine]
    F -->|7z| H[7z Engine]
    F -->|Zstd| I[Zstd Engine]
    G --> J[Parallel Decompressor]
    H --> J
    I --> J
    J --> K[VirtualFileSystem]
    K --> L[Extracted Files / Preview]
```

## Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET | 8.0 |
| UI Framework | Avalonia UI | 11.x |
| MVVM | CommunityToolkit.Mvvm | 8.4+ |
| DI | Microsoft.Extensions.DependencyInjection | 8.0 |
| CLI Parser | System.CommandLine | 2.0+ |
| Compression | SharpCompress | 0.38+ |
| Zstandard | ZstdNet | 1.5+ |
| Cryptography | System.Security.Cryptography | Built-in |
| Argon2 | Konscious.Security.Cryptography.Argon2 | 1.3+ |
| Image Processing | SixLabors.ImageSharp | 3.1+ |
| Testing | xUnit + FluentAssertions | Latest |

## Platform Support

| Platform | Desktop | Mobile (future) |
|---|---|---|
| Windows 10/11 | ✅ Native | N/A |
| macOS (Intel + Apple Silicon) | ✅ Native | N/A |
| Linux (GNOME, KDE, etc.) | ✅ Native | N/A |
| iOS | ❌ | ✅ Planned |
| Android | ❌ | ✅ Planned |

## Threading Model

- **Compression/Decompression**: Parallel.ForEach with configurable degree of parallelism
- **UI**: Always on Dispatcher.UIThread; background work via `Task.Run` + `IProgress<T>`
- **Cancellation**: `CancellationToken` through entire pipeline
- **I/O**: Async file streams with `ConfigureAwait(false)` in library code
