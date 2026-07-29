# Execution Plan & Status

## Project Structure

```
arcana/
├── src/
│   ├── Arcana.Core/       # Engines, VFS, crypto, tools
│   ├── Arcana.Cli/        # System.CommandLine CLI (8 commands)
│   └── Arcana.App/        # Avalonia GUI (basic shell)
├── tests/
│   ├── Arcana.Core.Tests/ # 134 tests → 137 total
│   └── Arcana.App.Tests/  # 3 tests
├── docs/
│   ├── PLAN.md
│   ├── ROADMAP.md
│   └── DECISIONS.md
├── build/
│   ├── clean.ps1
│   └── increment-version.ps1
├── AGENTS.md
└── .gitignore
```

## Completed

### Core — Compression Engines (17 total)

| Engine | Backend | R/W | Status |
|---|---|---|---|
| Zip | SharpCompress ZipArchive + ZipWriter | r/w | ✅ |
| Zstd | ZstdNet | r/w | ✅ |
| 7-Zip | SharpCompress 7z r/w | r/w | ✅ |
| Tar | SharpCompress TarArchive + WriterFactory | r/w | ✅ |
| Tar.Gz | TarEngine routing | r/w | ✅ |
| Tar.Bz2 | TarEngine routing | r/w | ✅ |
| Tar.Xz | TarEngine (Xz write NotSupported) | r/w | ✅ |
| Tar.Zst | TarEngine via ZstdNet | r/w | ✅ |
| RAR | SharpCompress RarArchive | r/o | ✅ |
| ACE | Hawkynt AceFormatDescriptor | r/o | ✅ |
| ARJ | SharpCompress ArjReader | r/o | ✅ |
| CAB | Hawkynt CabFormatDescriptor | r/o | ✅ |
| LZH/LHA | Hawkynt LzhFormatDescriptor | r/o | ✅ |
| Brotli | System.IO.Compression | r/w | ✅ |
| GZip | SharpCompress GZip | r/w | ✅ |
| BZip2 | SharpCompress BZip2 | r/w | ✅ |
| Xz | SharpCompress Xz (write NotSupported) | r/w | ✅ |
| LZMA | SharpCompress LZMA (write NotSupported) | r/w | ✅ |
| LZ4 | K4os.Compression.LZ4 | r/w | ✅ |
| Snappy | Snappy.Sharp | r/w | ✅ |
| HawkyntFallback | FormatRegistry auto-detect (240+ formats) | r/o | ✅ |

### Core — Crypto

- [x] AES-256-GCM encryption/decryption (EncryptStream/DecryptStream)
- [x] Argon2id key derivation (64MB, 3 iterations, 4 parallelism)
- [x] ChaCha20-Poly1305 enum (implementation pending)
- [x] EncryptionProvider with salt embedding
- [x] Password propagation via engine.Password property

### Core — Virtual File System

- [x] ArchiveNode tree with ContentFactory lazy loading
- [x] Entry management (add file, add directory)
- [x] Dirty tracking for modified files
- [ ] Edit-in-place (rename, delete, add) — pending

### Core — Tools

- [x] FileSplitter (with HJSplit-compatible naming `--hjsplit`)
- [x] FileJoiner (auto-detect single `.001` or directory scan)
- [x] HashCalculator (MD5, SHA1, SHA256, SHA512 + --verify)
- [ ] ImageConverter — stub (NotImplementedException)
- [ ] BatchProcessor — stub (NotImplementedException)

### CLI (8 commands)

- [x] `arcana compress` (StartProgressAsync per file)
- [x] `arcana extract` (StartStatusAsync per file)
- [x] `arcana list` (Table output)
- [x] `arcana convert`
- [x] `arcana hash` (with --verify)
- [x] `arcana split` (with --hjsplit)
- [x] `arcana join`
- [x] `arcana benchmark`
- [x] `--no-color` global option
- [x] `--log-level` (via Serilog)
- [x] Spectre.Console output wrapper (Output.cs)

### Avalonia App

- [x] Project scaffold (App.axaml, Program.cs)
- [x] MainWindow with Menu (File, Tools, Help)
- [x] TreeView for archive contents
- [x] Status bar
- [x] DI setup (MainViewModel, ArchiveViewModel, ToolsViewModel, PreviewViewModel, SettingsViewModel)
- [ ] Open archive dialog — stub
- [ ] New archive dialog — stub
- [ ] Archive browsing (grid + tree wired) — stub
- [ ] Extract dialog — not started
- [ ] Image preview — not started
- [ ] Text/Hex preview — stub
- [ ] Tools panel — stub
- [ ] Settings window — stub
- [ ] Progress panel — not started
- [ ] Drag & drop — not started

### Observability

- [x] Serilog across all 17 engines, ArchiveFactory, all 8 CLI commands, core tools
- [x] LogConfig.cs with configurable level
- [x] `--log-level` CLI option

## Not Started

- GitHub Actions CI/CD workflow
- ChaCha20-Poly1305 implementation
- Image conversion
- Batch processing
- i18n (EN + PT-BR)
- Theme support (dark/light)
- Shell extension (Windows)
- Mobile (iOS/Android)
- Plugin API
- WebAssembly
- FUSE filesystem mount
- Package distribution (winget, brew, apt)

## Architecture Decisions

See [DECISIONS.md](DECISIONS.md) for all ADRs.

## Next Priority

1. Avalonia GUI: wire archive open, extract, preview
2. GitHub Actions CI
3. ChaCha20-Poly1305 implementation
4. Image preview + conversion
5. v0.2.0 release
