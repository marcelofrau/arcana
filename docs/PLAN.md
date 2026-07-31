# Execution Plan & Status

## Project Structure

```
arcana/
├── src/
│   ├── Arcana.Core/        # Library: 17 engines, VFS, crypto, tools, logging
│   ├── Arcana.Cli/         # CLI: 8 commands, Spectre.Console output
│   ├── Arcana.App/         # Avalonia GUI: MVVM, services, controls
│   └── Arcana.slnx         # Solution (Core, Cli, App, Mobile, both test projects)
├── tests/
│   ├── Arcana.Core.Tests/  # 134 tests
│   └── Arcana.App.Tests/   # 11 tests (headless Avalonia)
├── docs/                   # This documentation set
├── build/
│   ├── clean.ps1
│   └── increment-version.ps1
├── AGENTS.md
└── .gitignore
```

## Completed

### Core — Compression (17 engines)

| Engine | Backend | R/W | Encrypt |
|---|---|---|---|
| Zip | SharpCompress ZipArchive + Arcana AES-GCM | r/w | ✅ |
| SevenZip | SharpCompress SevenZipArchive + Arcana AES-GCM | r/w | ✅ |
| Zstd | ZstdNet | r/w | — |
| Tar (+Gz/Bz2/Xz/Zst) | SharpCompress + routing | r/w | — |
| RAR | SharpCompress (RAR4/RAR5) | r/o | — |
| ACE | Hawkynt | r/o | ⚠️ |
| ARJ | SharpCompress | r/o | ⚠️ |
| CAB | Hawkynt | r/o | — |
| LZH/LHA | Hawkynt | r/o | — |
| Brotli | System.IO.Compression | r/w | — |
| GZip / BZip2 / Xz / LZMA | SharpCompress | r/w | — |
| LZ4 | K4os.Compression.LZ4 | r/w | — |
| Snappy | Snappy.Sharp | r/w | — |
| HawkyntFallback | FormatRegistry (240+ formats) | r/o | ⚠️ |

### Core — Crypto

- [x] AES-256-GCM EncryptStream / DecryptStream with embedded salt
- [x] Argon2id key derivation (64 MB, 3 iterations, 4 parallelism)
- [x] `CipherAlgorithm.ChaCha20Poly1305` enum value — implementation pending
- [x] Password propagation via `ArchiveFactory.SetPassword`

### Core — Virtual File System

- [x] `ArchiveNode` tree with `ContentFactory` lazy loading
- [x] Add file / add directory / delete / rename (API)
- [x] Dirty tracking (`GetDirtyNodes`, `MarkAllClean`)
- [x] `SyncNodeMetadata` — copies entry sizes into VFS nodes (fixes 0-size display)
- [ ] Edit-in-place wired to GUI — pending

### Core — Tools

- [x] `FileSplitter` (custom sizes, HJSplit naming `.001`)
- [x] `FileJoiner` (auto part discovery)
- [x] `HashCalculator` (MD5, SHA-1, SHA-256, SHA-512, verify)
- [ ] `ImageConverter` — stub (`NotImplementedException`)
- [ ] `BatchProcessor` — stub (`NotImplementedException`)

### CLI (8 commands)

- [x] `compress` — writes ZIP (format flag accepted, currently ignored)
- [x] `extract` — password/overwrite flags accepted, currently not wired
- [x] `list` — table output
- [x] `convert` — `--format`, `--level`
- [x] `hash` — `--algorithm`, `--verify`
- [x] `split` — `--part-size`, `--output`, `--hjsplit`
- [x] `join` — `--output`, auto-discovery
- [x] `benchmark` — `--data` (tiny→10 MB), ZIP/7z/Zstd
- [x] Global `--no-color` (Spectre.Console)
- [x] Serilog `--log-level` (option present; CLI currently keeps Warning)
- [x] Exit codes 0/1

### Avalonia GUI

- [x] App scaffold, DI (Microsoft.Extensions.DependencyInjection), MVVM (CommunityToolkit.Mvvm)
- [x] MainWindow: menu, toolbar, breadcrumb + filter, status bar
- [x] FolderTree (folders only, expand, sync selection)
- [x] FileTable (DataGrid: Name/Size/Packed/Ratio/Type/Modified, sortable)
- [x] PreviewPanel (text / image / hex / binary placeholder)
- [x] Dialogs: Password, Convert, Split, Join, Hash, Info, Prompt, Settings
- [x] Icon themes: Papirus (default), Material, WinRAR `.theme.rar`
- [x] Favorites (pinned archives)
- [x] ArchiveService: open / extract / test (CRC32) / save
- [ ] Archive editing in GUI (rename/delete/add) — pending
- [ ] Drag & drop — not started
- [ ] ToolsViewModel (split/join/hash wiring) — stub
- [ ] Settings window wiring — partial
- [ ] Progress panel — status bar only

### Observability

- [x] Serilog across engines, factory, CLI commands, tools
- [x] `LogConfig` (console + rolling file, level switch)
- [x] `%AppData%\Arcana\logs\arcana-*.log`

## Not Started

- Archive editing in GUI
- GitHub Actions CI/CD
- ChaCha20-Poly1305 implementation
- Image conversion
- Batch processing
- i18n (EN + PT-BR)
- Light theme
- Windows shell extension
- Mobile (iOS/Android)
- Plugin API
- WebAssembly
- FUSE filesystem mount
- Distribution packages (winget, brew, apt)

## Architecture Decisions

See [DECISIONS.md](DECISIONS.md) for all ADRs.

## Next Priority

1. Wire archive editing (rename/delete/add) from VFS into GUI
2. Save modified archives
3. ChaCha20-Poly1305 implementation
4. GitHub Actions CI
5. Image conversion (unblock `ImageConverter`)
