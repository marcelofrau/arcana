# Architecture

This document describes the current architecture of Arcana as implemented. Diagrams use [Mermaid](https://mermaid.js.org/).

## System Context (C4 Level 1)

```mermaid
C4Context
  title System Context diagram for Arcana
  Person(user, "User", "Person who compresses, extracts, and manages archives")

  System_Boundary(arcana, "Arcana") {
    System(gui, "Arcana.App", "Desktop GUI application", "Avalonia UI")
    System(cli, "Arcana.Cli", "Command-line interface", "System.CommandLine")
  }

  System_Ext(fs, "File System", "Local files and directories")

  Rel(user, gui, "Uses", "GUI")
  Rel(user, cli, "Uses", "CLI")
  Rel(gui, fs, "Reads/Writes files")
  Rel(cli, fs, "Reads/Writes files")
```

## Container Diagram (C4 Level 2)

```mermaid
C4Container
  title Container diagram for Arcana
  Person(user, "User")

  System_Boundary(arcana, "Arcana") {
    Container(gui, "Arcana.App", "Avalonia 12.1", "Desktop GUI")
    Container(cli, "Arcana.Cli", "System.CommandLine 2.0", "CLI tool")
    Container(core, "Arcana.Core", ".NET 10 Library", "Engines, VFS, crypto, tools, logging")
    ContainerDb(vfs, "VirtualFileSystem", "In-memory", "Archive node tree with lazy content + dirty tracking")
  }

  Rel(user, gui, "Uses")
  Rel(user, cli, "Uses")
  Rel(gui, core, "Calls", "In-process")
  Rel(cli, core, "Calls", "In-process")
  Rel(core, vfs, "Manages")
```

## Layer Diagram

```mermaid
graph TD
    subgraph "Presentation"
        App[Arcana.App<br/>Avalonia UI]
        Cli[Arcana.Cli<br/>System.CommandLine]
    end

    subgraph "App Services"
        ArchiveSvc[ArchiveService]
        PreviewSvc[PreviewService]
        DialogSvc[DialogService]
        SettingsSvc[SettingsService]
        FavoritesSvc[FavoritesService]
        IconThemeSvc[IconThemeService]
    end

    subgraph "Core"
        Factory[ArchiveFactory]
        Engines[17 Engines + Hawkynt fallback]
        Crypto[Cryptography]
        VFS[VirtualFileSystem]
        Tools[FileSplitter / FileJoiner / HashCalculator]
        Logging[Serilog]
    end

    App --> ArchiveSvc
    App --> PreviewSvc
    App --> DialogSvc
    App --> IconThemeSvc
    Cli --> Factory
    ArchiveSvc --> Factory
    PreviewSvc --> VFS
    Factory --> Engines
    Factory --> Crypto
    Engines --> VFS
    Engines --> Crypto
    Tools --> Logging
    Engines --> Logging
```

## Project Structure

```
arcana/
├── src/
│   ├── Arcana.Core/            # Library: engines, VFS, crypto, tools, logging
│   ├── Arcana.Cli/             # CLI: 8 commands, Spectre.Console output
│   └── Arcana.App/             # Avalonia GUI: MVVM, services, controls
├── tests/
│   ├── Arcana.Core.Tests/      # 134 tests (engines, crypto, tools, factory)
│   └── Arcana.App.Tests/       # 11 tests (headless Avalonia, binding/VM tests)
├── docs/                       # This documentation set
└── build/                      # clean.ps1, increment-version.ps1
```

Solution file: `src/Arcana.slnx` (Core, Cli, App, Mobile placeholder, Core.Tests, App.Tests).

## Core Class Diagram

```mermaid
classDiagram
    class IArchiveFormat {
        <<interface>>
        +string Name
        +string Extension
        +bool CanRead
        +bool CanWrite
        +bool CanEncrypt
        +bool SupportsSolid
        +bool SupportsVolumes
        +Archive Open(string, Stream, AccessMode, CancellationToken)
        +Task~Archive~ OpenAsync(...)
        +void Save(Archive, Stream, CompressionOptions, IProgress~ProgressReport~, CancellationToken)
        +Task SaveAsync(...)
    }

    class Archive {
        +CompressionFormat Format
        +IArchiveFormat FormatEngine
        +IReadOnlyList~ArchiveEntry~ Entries
        +VirtualFileSystem Vfs
        +bool IsEncrypted
        +string? Comment
        +ArchiveEntry? FindEntry(string)
        +IEnumerable~ArchiveEntry~ FindEntries(string)
        +void SyncNodeMetadata()
    }

    class ArchiveEntry {
        +string Path
        +string Name
        +long Size
        +long CompressedSize
        +double CompressionRatio
        +uint Crc32
        +string? Comment
        +bool IsEncrypted
        +bool IsDirectory
        +DateTime LastModified
        +string? Method
        +string? HostOS
        +uint Attributes
    }

    class ArchiveFactory {
        <<static>>
        +IArchiveFormat GetFormat(CompressionFormat)
        +IArchiveFormat GetFormatFromExtension(string)
        +IArchiveFormat GetFormatFromFileHeader(Stream)
        +Archive Open(string path, string? password)
        +Task~Archive~ OpenAsync(string path, string? password, CancellationToken)
        +void SetPassword(Archive, string?)
    }

    class VirtualFileSystem {
        +ArchiveNode Root
        +bool HasDirtyNodes
        +ArchiveNode AddFile(string, Stream)
        +ArchiveNode AddDirectory(string)
        +void DeleteNode(ArchiveNode)
        +void RenameNode(ArchiveNode, string)
        +IReadOnlyList~ArchiveNode~ GetDirtyNodes()
        +void MarkAllClean()
        +ArchiveNode? FindNode(string)
        +void ImportFromArchive(Archive)
    }

    class ArchiveNode {
        +string Name
        +string FullPath
        +NodeType Type
        +long OriginalSize
        +long CompressedSize
        +bool IsDirty
        +ArchiveNode? Parent
        +IReadOnlyList~ArchiveNode~ Children
        +IReadOnlyList~ArchiveNode~ ChildFolders
        +DateTime LastModified
        +Stream OpenRead()
        +Stream OpenWrite()
    }

    IArchiveFormat <.. Archive
    Archive *-- ArchiveEntry
    Archive *-- VirtualFileSystem
    VirtualFileSystem *-- ArchiveNode
    ArchiveFactory ..> IArchiveFormat
```

### Engine Matrix

```mermaid
graph LR
    subgraph Read-Write
        Zip[ZipEngine]
        SevenZip[SevenZipEngine]
        Zstd[ZstdEngine]
        Tar[TarEngine]
        Brotli[BrotliEngine]
        Gzip[GzipEngine]
        BZip2[BZip2Engine]
        Xz[XzEngine]
        Lzma[LzmaEngine]
        Lz4[Lz4Engine]
        Snappy[SnappyEngine]
    end
    subgraph Read-Only
        Rar[RarEngine]
        Ace[AceEngine]
        Arj[ArjEngine]
        Cab[CabEngine]
        Lzh[LzhEngine]
        Hawkynt[HawkyntFallbackEngine<br/>240+ formats]
    end
```

## Open Pipeline (browse / extract / preview)

```mermaid
flowchart TD
    A[Archive file path] --> B[ArchiveFactory.OpenAsync]
    B --> C{Detect format}
    C -->|extension ends .tar*| D[TarEngine routing<br/>.tar / .tar.gz / .tar.bz2 / .tar.xz / .tar.zst]
    C -->|8-byte magic header| E[Native engine match]
    C -->|no match| F[HawkyntFallbackEngine<br/>FormatRegistry auto-detect]
    D --> G[engine.OpenAsync]
    E --> G
    F --> G
    G --> H[Populate Archive.Entries]
    H --> I[Populate VirtualFileSystem tree]
    I --> J[Archive.SyncNodeMetadata<br/>copies sizes into VFS nodes]
    J --> K[GUI / CLI consumer]
```

Format detection (`GetFormatFromPathOrHeader`) checks the extension first, then the first 8 bytes. When a native engine cannot open a stream, the Hawkynt `FormatRegistry` is tried as a last resort — this is how MSIX, MSI, EXE/SFX, CHM, WIM, game packs and other hidden archive formats are read.

## Compress Pipeline

```mermaid
flowchart TD
    A[Source files] --> B[engine.SaveAsync]
    B --> C{Password?}
    C -->|no| D[SharpCompress / ZstdNet writer]
    C -->|yes| E[EncryptionProvider.CreateEncryptingStream<br/>AES-256-GCM]
    E --> F[Salt embedded in stream header]
    D --> G[Output archive]
    F --> G
    B --> H[IProgress~ProgressReport~]
    H --> I[CLI progress / GUI status bar]
```

Only `ZipEngine` and `SevenZipEngine` support writing encrypted archives (via the Arcana crypto stream wrapper). `AceEngine`/`ArjEngine` accept a password that is forwarded to the underlying reader. Reading encrypted archives unwraps with `CreateDecryptingStream` (see [CIPHERS.md](compression/CIPHERS.md)).

## GUI Architecture

```mermaid
graph TD
    subgraph Views
        MainWindow[MainWindow.axaml]
        FolderTree[FolderTree<br/>TreeView folders only]
        FileTable[FileTable<br/>DataGrid columns]
        PreviewPanel[PreviewPanel<br/>text / image / hex / placeholder]
        Dialogs[Dialogs<br/>Password, Convert, Split, Join, Hash, Info, Prompt, Settings]
    end

    subgraph ViewModels
        MainVM[MainViewModel<br/>toolbar, commands, status]
        ArchiveVM[ArchiveViewModel<br/>tree, entries, nav history, filter]
        PreviewVM[PreviewViewModel]
        ToolsVM[ToolsViewModel<br/>stub]
        DialogVMs[Dialog ViewModels]
    end

    subgraph Services
        ArchiveSvc[ArchiveService]
        PreviewSvc[PreviewService]
        DialogSvc[DialogService]
        SettingsSvc[SettingsService]
        FavoritesSvc[FavoritesService]
    end

    MainWindow --> MainVM
    FolderTree --> ArchiveVM
    FileTable --> ArchiveVM
    PreviewPanel --> PreviewVM
    MainVM --> ArchiveSvc
    MainVM --> PreviewSvc
    MainVM --> DialogSvc
    ArchiveVM --> ArchiveSvc
    PreviewVM --> PreviewSvc
    DialogSvc --> DialogVMs
```

- **MVVM**: CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`). No ReactiveUI.
- **DI**: Microsoft.Extensions.DependencyInjection — services registered as singletons, ViewModels as transients (`App.axaml.cs`).
- **Compiled bindings**: `x:DataType` enabled project-wide; converters (Equals, Invert) registered in `App.axaml`.
- **Threading**: all UI work on `Dispatcher.UIThread`; background work via `Task.Run` (`RunBusyAsync`) + `IProgress<T>`.

### Preview pipeline

```mermaid
flowchart TD
    A[FileTable selection changed] --> B[PreviewViewModel.Show]
    B --> C{node has content?}
    C -->|no| D[Clear placeholder]
    C -->|yes| E[PreviewService.DetectKind<br/>text / image / hex]
    E -->|text| F[LoadText<br/>max 256 KiB, BOM/UTF-8]
    E -->|image| G[LoadImage<br/>Avalonia Bitmap, fallback hex]
    E -->|hex / other| H[Binary placeholder<br/>icon + name + button]
    H --> I[LoadBinaryCommand<br/>LoadHex on demand, 64 KiB cap]
```

## Icon Architecture

```mermaid
graph LR
    IconRuntime[IconRuntime<br/>static Current] --> IIconProvider
    IIconProvider[IIconProvider<br/>Name, ToolbarSize, GetIcon]
    IIconProvider --> Default[DefaultIconProvider<br/>Material Design paths]
    IIconProvider --> Papirus[PapirusIconProvider<br/>PNG assets, GPL-3.0]
    IIconProvider --> WinRar[WinRarThemeProvider<br/>.theme.rar files]
    IconThemeService[IconThemeService<br/>built-ins + %APPDATA%\\Arcana\\Themes] --> IconRuntime
    IconResolver[IconResolver<br/>ForNode / ForExtension] --> IconRuntime
    NodeIconConverter[NodeIconConverter] --> IconResolver
```

## Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET | 10.0 (C# 12/13) |
| UI Framework | Avalonia UI | 12.1 |
| DataGrid | Avalonia.Controls.DataGrid | 12.1 |
| MVVM | CommunityToolkit.Mvvm | 8.4 |
| DI | Microsoft.Extensions.DependencyInjection | 10.0 |
| CLI Parser | System.CommandLine | 2.0.10 |
| Console UI | Spectre.Console | 0.57.2 |
| Compression | SharpCompress | 0.50.1 |
| Zstandard | ZstdNet | 1.5.7 |
| LZ4 | K4os.Compression.LZ4 | 1.3.8 |
| Snappy | Snappy.Sharp | 1.0.0 |
| Legacy fallback | Hawkynt.FileFormats.Archives | 1.0.0.696 |
| Argon2 | Konscious.Security.Cryptography.Argon2 | 1.3.1 |
| Logging | Serilog | 4.4.0 |
| Testing | xUnit + FluentAssertions | Latest |

## Platform Support

| Platform | Desktop | Mobile |
|---|---|---|
| Windows 10/11 | ✅ Native | N/A |
| macOS (Intel + Apple Silicon) | ✅ Native (Avalonia) | N/A |
| Linux (GNOME, KDE, etc.) | ✅ Native (Avalonia) | N/A |
| iOS / Android | ❌ | Planned (Avalonia) |

## Threading Model

- **I/O and compression**: async streams + `Task.Run` in engines; `CancellationToken` through the pipeline.
- **UI**: always `Dispatcher.UIThread`; long operations via `RunBusyAsync` (status bar progress).
- **Logging**: Serilog console + rolling file sink (`%AppData%\Arcana\logs\arcana-*.log`).
