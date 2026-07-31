# Architecture Decision Records

Format: [MADR](https://adr.github.io/madr/)-style. Statuses: Accepted / Superseded / Proposed.

## ADR-0001: Use C# as the Sole Programming Language

**Status**: Accepted
**Context**: Multi-language could bring functional advantages (F# for crypto, Rust for perf).
**Decision**: C# only across all projects. Modern C# (records, pattern matching, `Span<T>`) covers functional patterns adequately. Single language reduces onboarding friction.
**Consequences**: Some performance-critical paths may need P/Invoke to native libs later.

---

## ADR-0002: Use GPL-Compatible Dependencies Only

**Status**: Accepted
**Context**: GPLv3 requires compatible licenses for linked libraries.
**Decision**: All third-party dependencies must be MIT, Apache 2.0, BSD, or LGPL. GPL-licensed libraries are avoided unless unavoidable.
**Consequences**: SharpCompress (MIT), ZstdNet (BSD), Hawkynt (MIT), K4os (MIT), Snappy.Sharp (MIT), Serilog (Apache 2.0) all compatible. Papirus icons are GPL-3.0 — same license as Arcana, so compatible.

---

## ADR-0003: CommunityToolkit.Mvvm over ReactiveUI

**Status**: Accepted
**Context**: Both are viable MVVM frameworks for Avalonia.
**Decision**: CommunityToolkit.Mvvm with source generators (`[ObservableProperty]`, `[RelayCommand]`). Simpler API, less boilerplate, better AOT compatibility.
**Consequences**: No ReactiveUI dependency. Source generators catch errors at compile time.

---

## ADR-0004: Virtual File System for Archive Editing

**Status**: Accepted
**Context**: Editing files inside archives (rename, delete, add, modify) requires tracking changes before commit.
**Decision**: In-memory tree structure (`VirtualFileSystem`) that lazy-loads original data via `ContentFactory` and tracks dirty nodes. On save, only modified entries are re-compressed.
**Consequences**: Higher memory usage for large archives. Mitigated by lazy loading.

---

## ADR-0005: Async-First API Design

**Status**: Accepted
**Context**: Compression is I/O-bound and CPU-bound. UI must remain responsive.
**Decision**: All public APIs return `Task`/`Task<T>`. `CancellationToken` throughout. Progress via `IProgress<T>`.
**Consequences**: Slightly more complex API surface, but correct behavior for UI and CLI.

---

## ADR-0006: AES-GCM as Default Encryption

**Status**: Accepted (ChaCha20-Poly1305 planned, not implemented)
**Context**: Need authenticated encryption (AEAD) for security.
**Decision**: AES-256-GCM is the default and only implemented cipher (`EncryptionProvider`). `CipherAlgorithm.ChaCha20Poly1305` is declared in the enum but **unimplemented**; targeted at mobile/ARM platforms without AES hardware acceleration.
**Consequences**: No support for legacy AES-CBC. Files encrypted with Arcana require Arcana to decrypt.

---

## ADR-0007: Argon2id for Key Derivation

**Status**: Accepted
**Context**: Password-based encryption needs a memory-hard KDF to resist GPU/ASIC attacks.
**Decision**: Argon2id (hybrid approach, resistant to both side-channel and GPU attacks). Defaults: 64 MB memory, 3 iterations, 4 parallelism (`Argon2KeyDerivation`, `EncryptionOptions`).
**Consequences**: Slower key derivation than PBKDF2, but significantly more secure.

---

## ADR-0008: CLI Built with System.CommandLine

**Status**: Accepted
**Context**: Need robust CLI parser with auto-generated help and POSIX conventions.
**Decision**: System.CommandLine v2.0+. Supports subcommands, options, aliases, and implicit `--version`.
**Consequences**: Larger dependency than manual parsing, but better developer and user experience. Output is rendered through a Spectre.Console wrapper (`Output.cs`).

---

## ADR-0009: GitHub Actions for CI/CD

**Status**: Accepted (workflow pending)
**Context**: Cross-platform build and test matrix required.
**Decision**: GitHub Actions with matrix strategy (windows-latest, ubuntu-latest, macos-latest). Release workflow triggered by tags.
**Consequences**: Free for open source. No workflow committed yet — see ROADMAP v0.4.0.

---

## ADR-0010: Compression Engines as Pluggable Adapters

**Status**: Accepted
**Context**: Each compression format has unique parameters and capabilities.
**Decision**: Each format implements `IArchiveFormat`. Engine selection via `ArchiveFactory` (magic bytes + extension). New formats add a new class + registration.
**Consequences**: Easy to add new formats. Consistent API across formats with different capabilities.

---

## ADR-0011: Standalone Compression Formats (GZip, BZip2, Xz, Brotli, LZ4, LZMA, Snappy)

**Status**: Accepted — implemented in v0.1.0
**Context**: Standalone compressors (single-stream, no metadata, no multi-file) were initially deemed low-value since TarEngine already handles `.tar.gz`, `.tar.bz2`, `.tar.xz`, `.tar.zst`.
**Original Decision**: Skip all standalone engines.
**Reversal**: Implemented all 7 engines. Rationale: (1) CLI users expect `arcana extract file.gz` to work, (2) SharpCompress provides readers/writers for all of them with minimal code, (3) effort-to-value ratio inverted once the pattern was established.
**Engines implemented**: Brotli (System.IO.Compression), Gzip, BZip2, Xz, Lzma (SharpCompress), Lz4 (K4os), Snappy (Snappy.Sharp).
**Consequences**: All standalone formats can be compressed/extracted.

---

## ADR-0012: Hawkynt Fallback for 240+ Legacy Formats

**Status**: Accepted
**Context**: Users expect to open obscure and "hidden" archives (MSIX, MSI, EXE/SFX, CHM, WIM, DOCX, EPUB, APK, DEB, game packs, retro formats) that have no mature first-party engine.
**Decision**: Add `HawkyntFallbackEngine` backed by Hawkynt.FileFormats.Archives `FormatRegistry`. It runs last in `ArchiveFactory.GetFormatFromPathOrHeader` when magic-byte detection fails. Read-only; writes throw `NotSupportedException`. Password is forwarded when present.
**Consequences**: Huge format surface at the cost of best-effort quality per format. Detection by content, not extension.

---

## ADR-0013: DataGrid Theme Must Be Registered Explicitly

**Status**: Accepted
**Context**: After the Avalonia 12 upgrade, the DataGrid rendered no rows — the DataGrid Fluent theme is no longer part of the default FluentTheme.
**Decision**: Add `<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>` in the production app **and** the test app (`TestApp.cs`). Verified with a row-realization test (`ApplyTemplate`).
**Consequences**: Both apps must stay in sync; forgetting either side makes DataGrid content invisible.

---

## ADR-0014: Icon Providers (Papirus / Material / WinRAR themes)

**Status**: Accepted
**Context**: Need a themed, extensible icon system matching GPLv3.
**Decision**: `IIconProvider` abstraction with three implementations: `PapirusIconProvider` (default, PNG assets, GPL-3.0), `DefaultIconProvider` (Material vector paths), `WinRarThemeProvider` (user `.theme.rar` files from `%APPDATA%\Arcana\Themes`). `IconThemeService` manages built-ins + installed themes. Windows icon follows the theme.
**Consequences**: No hand-drawn geometry; icons come from licensed sets. WinRAR themes are loaded at runtime, not bundled.

---

## ADR-0015: SyncNodeMetadata — Sizes Live in Entries, Mirrored to VFS

**Status**: Accepted
**Context**: Engines populate `Archive.Entries` with sizes, but VFS nodes displayed 0-byte sizes in the UI.
**Decision**: `Archive.SyncNodeMetadata()` copies entry Size / CompressedSize / LastModified into the matching VFS nodes via `Vfs.FindNode`. Called after open in `ArchiveFactory` and defensively in `ArchiveViewModel.LoadArchive`.
**Consequences**: Correct Size/Packed/Ratio columns without duplicating source-of-truth.

---

## ADR-0016: Binary Preview Placeholder (Lazy Hex)

**Status**: Accepted
**Context**: Hex-dumping large or unknown binaries on selection floods the UI and wastes I/O.
**Decision**: Unknown/hex previews render a placeholder (icon + name + "Binary Preview" button). Content loads on demand via `LoadBinaryCommand` (64 KiB cap).
**Consequences**: Fast, predictable preview; one extra click for binary dumps.

---

## ADR-0017: Trunk-Based Development (single `main`)

**Status**: Accepted
**Context**: Single-developer project; GitFlow (`develop`/`release`/`hotfix`) added ceremony without benefit.
**Decision**: Work directly on `main`; short-lived `feat/*`/`fix/*`/`docs/*` branches for larger changes, squash-merged. Tags on `main` for releases.
**Consequences**: Simple history. Branch protection + CI to be added later (see ADR-0009).

---

## ADR-0018: Versioning via Build Counter

**Status**: Accepted
**Context**: Need per-build version identity for a pre-1.0 project.
**Decision**: Version format `{prefix}-build.{N}+{githash}`. Prefix from `Directory.Build.props`; counter from `build/build-counter.txt` (`build/increment-version.ps1`); git hash auto-appended via `SourceRevisionId`. CLI `--version` and GUI About dialog read assembly info — never hardcoded.
**Consequences**: Every build is uniquely identifiable; counter resets when the prefix bumps.
