# Architecture Decision Records

## ADR-0001: Use C# as the Sole Programming Language

**Status**: Accepted
**Context**: Multi-language could bring functional advantages (F# for crypto, Rust for perf).
**Decision**: C# only across all projects. Modern C# (records, pattern matching, Span<T>) covers functional patterns adequately. Single language reduces onboarding friction.
**Consequences**: Some performance-critical paths may need P/Invoke to native libs later.

---

## ADR-0002: Use MIT-licensed Dependencies Only

**Status**: Accepted
**Context**: GPLv3 requires compatible licenses for linked libraries.
**Decision**: All third-party dependencies must be MIT, Apache 2.0, BSD, or LGPL. GPL-licensed libraries are avoided unless unavoidable.
**Consequences**: SharpCompress (MIT), ZstdNet (BSD), ImageSharp (MIT) all compatible.

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
**Decision**: In-memory tree structure (VirtualFileSystem) that lazy-loads original data and tracks dirty nodes. On save, only modified entries are re-compressed.
**Consequences**: Higher memory usage for large archives. Mitigated by lazy loading.

---

## ADR-0005: Async-First API Design

**Status**: Accepted
**Context**: Compression is I/O-bound and CPU-bound. UI must remain responsive.
**Decision**: All public APIs return `Task`/`Task<T>`. `CancellationToken` throughout. Progress via `IProgress<T>`.
**Consequences**: Slightly more complex API surface, but correct behavior for UI and CLI.

---

## ADR-0006: AES-GCM as Default Encryption

**Status**: Accepted
**Context**: Need authenticated encryption (AEAD) for security.
**Decision**: AES-256-GCM is default. ChaCha20-Poly1305 available as alternative (better on mobile/ARM). Both are AEAD.
**Consequences**: No support for legacy AES-CBC. Files encrypted with Arcana require Arcana to decrypt.

---

## ADR-0007: Argon2id for Key Derivation

**Status**: Accepted
**Context**: Password-based encryption needs memory-hard KDF to resist GPU/ASIC attacks.
**Decision**: Argon2id (hybrid approach, resistant to both side-channel and GPU attacks). Default parameters: 64MB memory, 3 iterations, 4 parallelism.
**Consequences**: Slower key derivation than PBKDF2, but significantly more secure.

---

## ADR-0008: CLI Built with System.CommandLine

**Status**: Accepted
**Context**: Need robust CLI parser with auto-generated help, tab completion, and POSIX conventions.
**Decision**: System.CommandLine v2.0+. Supports subcommands, aliases, validation, and `dotnet-suggest`.
**Consequences**: Larger dependency than manual parsing, but better developer and user experience.

---

## ADR-0009: GitHub Actions for CI/CD

**Status**: Accepted
**Context**: Cross-platform build and test matrix required.
**Decision**: GitHub Actions with matrix strategy (windows-latest, ubuntu-latest, macos-latest). Release workflow triggered by tags.
**Consequences**: Free for open source. Limited to 2,000 min/month (sufficient for project scale).

---

## ADR-0010: Compression Engines as Pluggable Adapters

**Status**: Accepted
**Context**: Each compression format has unique parameters and capabilities.
**Decision**: Each format implements `IArchiveFormat`. Engine selection via factory + options. New formats add a new class + registration.
**Consequences**: Easy to add new formats. Consistent API across formats with different capabilities.

---

## ADR-0011: Standalone Compression Formats (GZip, BZip2, Xz, Brotli, LZ4, LZMA, Snappy)

**Status**: Superseded — all implemented in v0.2.x
**Context**: Standalone compressors (single-stream, no metadata, no multi-file) were initially deemed low-value since TarEngine already handles `.tar.gz`, `.tar.bz2`, `.tar.xz`, `.tar.zst`.
**Original Decision**: Skip all standalone engines.
**Reversal**: Implemented all 7 engines plus Snappy. Rationale: (1) CLI users expect `arcana extract file.gz` to work, (2) SharpCompress provides readers/writers for all of them with minimal code (~30 lines each), (3) effort-to-value ratio inverted once the pattern was established.
**Engines implemented**:
- BrotliEngine (.br) — System.IO.Compression
- GzipEngine (.gz) — SharpCompress
- BZip2Engine (.bz2) — SharpCompress
- XzEngine (.xz) — SharpCompress
- LzmaEngine (.lzma) — SharpCompress
- Lz4Engine (.lz4) — K4os.Compression.LZ4
- SnappyEngine (.snappy) — Snappy.Sharp
**Consequences**: All standalone formats can be compressed/extracted. ZPAQ remains unsupported (no mature C# library).
