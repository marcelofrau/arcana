# Specifications

Functional and non-functional requirements, aligned with the current implementation.

## Functional Requirements

### FR-COMPRESS: Compression Operations

| ID | Requirement | Status |
|---|---|---|
| FR-CMP-01 | Compress files and directories into ZIP | ✅ Implemented |
| FR-CMP-02 | Compress into 7z | ✅ Implemented |
| FR-CMP-03 | Compress into Zstandard (`.zst`) | ✅ Implemented |
| FR-CMP-04 | Compress into Brotli, LZ4, LZMA, XZ, BZip2, GZip, Snappy (single-stream) | ✅ Implemented |
| FR-CMP-05 | Create TAR archives, with optional GZip/BZip2/Zstd wrapping | ✅ Implemented (`.tar.xz` write not supported) |
| FR-CMP-06 | Set compression level (0=store … 10=insane) | ✅ Implemented (CLI clamps 0–9/0–10, GUI preset list) |
| FR-CMP-07 | Password encryption during compression | ⚠️ ZIP + 7z only (Arcana AES-256-GCM container) |
| FR-CMP-08 | Split archive into volumes during compression | ❌ Not implemented — use `split` tool |
| FR-CMP-09 | Key-file-based encryption | ❌ Future |
| FR-CMP-10 | Archive/file comments | ❌ Future |

### FR-EXTRACT: Extraction Operations

| ID | Requirement | Status |
|---|---|---|
| FR-EXT-01 | Extract ZIP, 7z, Zstd, Tar(+gz/bz2/xz/zst), GZip, BZip2, XZ, LZMA, LZ4, Snappy, Brotli | ✅ Implemented |
| FR-EXT-02 | Extract RAR (read-only) | ✅ Implemented (RAR4 + RAR5) |
| FR-EXT-03 | Extract ACE, ARJ, CAB, LZH/LHA (read-only) | ✅ Implemented |
| FR-EXT-04 | Extract 240+ formats via Hawkynt fallback — including MSIX, MSI, APPX, EXE/SFX/installers, CHM, WIM, DOCX/XLSX/PPTX, EPUB, APK, IPA, DEB, RPM, game packs | ✅ Best-effort, read-only |
| FR-EXT-05 | Extract with password | ✅ Implemented (Zip, 7z, Zstd, Rar, Ace, Arj, Cab, Lzh, Hawkynt via `SetPassword`) |
| FR-EXT-06 | Extract single files / specific directory | ✅ Implemented |
| FR-EXT-07 | Test archive integrity | ✅ Implemented in GUI (CRC32 check) |
| FR-EXT-08 | Key-file-based decryption | ❌ Future |

### FR-BROWSE: Archive Browsing

| ID | Requirement | Status |
|---|---|---|
| FR-BRW-01 | Open archive and view contents as folder tree + file list | ✅ Implemented |
| FR-BRW-02 | Sort entries by name, size, packed, ratio, type, modified | ✅ Implemented (DataGrid sorting) |
| FR-BRW-03 | Search/filter entries by name | ✅ Implemented (filter box) |
| FR-BRW-04 | View entry properties (size, packed, ratio, type, modified, CRC) | ✅ Implemented (columns + Info dialog) |
| FR-BRW-05 | Navigate with breadcrumb, back history, folder tree sync | ✅ Implemented |
| FR-BRW-06 | Favorites (pinned archives) | ✅ Implemented |

### FR-PREVIEW: Internal Preview

| ID | Requirement | Status |
|---|---|---|
| FR-PRV-01 | Preview text files (BOM/UTF-8/Latin-1 detection) | ✅ Implemented |
| FR-PRV-02 | Preview images (PNG, JPEG, BMP, GIF, etc. via Avalonia codecs) | ✅ Implemented |
| FR-PRV-03 | Preview files as hexadecimal dump (on demand) | ✅ Implemented |
| FR-PRV-04 | Syntax highlighting | ❌ Future |
| FR-PRV-05 | Inline text editing | ❌ Future |
| FR-PRV-06 | File metadata (EXIF, format info) | ❌ Future |

### FR-EDIT: Archive Editing

| ID | Requirement | Status |
|---|---|---|
| FR-EDT-01 | Rename files inside archive | ⚠️ VFS API exists; GUI wiring pending |
| FR-EDT-02 | Delete files from archive | ⚠️ VFS API exists; GUI wiring pending |
| FR-EDT-03 | Add new files to existing archive | ⚠️ VFS API exists; GUI wiring pending |
| FR-EDT-04 | Drag & drop files into archive | ❌ Future |
| FR-EDT-05 | Save modified archive in place / save copy as | ⚠️ `SaveCopyAsCommand` present |

### FR-TOOLS: Utility Tools

| ID | Requirement | Status |
|---|---|---|
| FR-TLS-01 | Split files into parts | ✅ Implemented (GUI + CLI, custom size, presets 100 MB–4 GB) |
| FR-TLS-02 | Join split files back | ✅ Implemented (auto part discovery) |
| FR-TLS-03 | HJSplit-compatible split naming (`.001`, `.002`…) | ✅ Implemented |
| FR-TLS-04 | Compute hashes MD5, SHA-1, SHA-256, SHA-512 | ✅ Implemented (GUI + CLI) |
| FR-TLS-05 | Verify hashes | ✅ Implemented (CLI `--verify`) |
| FR-TLS-06 | Convert archives between formats | ✅ Implemented (ZIP, 7z, Zstd in GUI; CLI `--format`) |
| FR-TLS-07 | Benchmark compression engines | ✅ Implemented (CLI `benchmark`, data sizes tiny→10 MB, ZIP/7z/Zstd) |
| FR-TLS-08 | Batch processing | ❌ Stub (`BatchProcessor` throws `NotImplementedException`) |
| FR-TLS-09 | Image conversion | ❌ Stub (`ImageConverter` throws `NotImplementedException`) |

### FR-CLI: Command-Line Interface

| ID | Requirement | Status |
|---|---|---|
| FR-CLI-01 | `arcana compress <source>... -o <output>` | ✅ Implemented (writes ZIP) |
| FR-CLI-02 | `arcana extract <archive> [dir]` | ✅ Implemented |
| FR-CLI-03 | `arcana list <archive>` | ✅ Implemented (table output) |
| FR-CLI-04 | `arcana split` / `arcana join` | ✅ Implemented |
| FR-CLI-05 | `arcana hash` with `--verify` | ✅ Implemented |
| FR-CLI-06 | `arcana convert` | ✅ Implemented |
| FR-CLI-07 | `arcana benchmark` | ✅ Implemented |
| FR-CLI-08 | Global `--no-color` | ✅ Implemented |
| FR-CLI-09 | Global `--log-level` | ⚠️ Option exists; currently unused by CLI (level fixed at Warning) |
| FR-CLI-10 | Pipe data via stdin/stdout | ❌ Future |

## Non-Functional Requirements

| ID | Requirement | Target | Status |
|---|---|---|---|
| NFR-PRF-01 | UI startup time | < 2 s | ✅ Achieved |
| NFR-PRF-02 | UI responsiveness during background ops | No frame drops | ✅ Background via `Task.Run` |
| NFR-PRF-03 | Memory guard for large archives | Lazy content loading | ✅ `ContentFactory` loads on demand |
| NFR-SEC-01 | Authenticated encryption (AEAD) | AES-256-GCM | ✅ Implemented |
| NFR-SEC-02 | Memory-hard key derivation | Argon2id | ✅ Implemented |
| NFR-SEC-03 | ChaCha20-Poly1305 alternative | AEAD, software-friendly | ❌ Future |
| NFR-CPT-01 | Cross-platform Windows, macOS, Linux | Same feature set | ✅ Avalonia |
| NFR-CPT-02 | Unicode filenames | Full | ✅ |
| NFR-CPT-03 | Files > 4 GB | Supported (ZIP64 etc.) | ✅ |
| NFR-CPT-04 | Graceful handling of corrupted archives | Error message, not crash | ✅ Best-effort |
| NFR-LOG-01 | Structured logging across engines/commands/tools | Serilog | ✅ |

## Format Support Matrix

See [compression/FORMATS.md](compression/FORMATS.md) for the full capability table.

| Format | Read | Write | Encrypt | Backend |
|---|---|---|---|---|
| ZIP | ✅ | ✅ | ✅ | SharpCompress + Arcana AES-GCM |
| 7z | ✅ | ✅ | ✅ | SharpCompress + Arcana AES-GCM |
| Zstandard | ✅ | ✅ | ❌ | ZstdNet |
| Brotli | ✅ | ✅ | ❌ | System.IO.Compression |
| LZ4 | ✅ | ✅ | ❌ | K4os.Compression.LZ4 |
| LZMA | ✅ | ✅ | ❌ | SharpCompress |
| XZ | ✅ | ✅ | ❌ | SharpCompress |
| BZip2 | ✅ | ✅ | ❌ | SharpCompress |
| GZip | ✅ | ✅ | ❌ | SharpCompress |
| Snappy | ✅ | ✅ | ❌ | Snappy.Sharp |
| TAR | ✅ | ✅ | ❌ | SharpCompress |
| TAR+GZ/BZ2/ZST | ✅ | ✅ | ❌ | TarEngine routing |
| TAR+XZ | ✅ | ⚠️ no write | ❌ | TarEngine |
| RAR | ✅ | ❌ | ❌ | SharpCompress (RAR4/RAR5) |
| ACE | ✅ | ❌ | ⚠️ password | Hawkynt |
| ARJ | ✅ | ❌ | ⚠️ password | SharpCompress |
| CAB | ✅ | ❌ | ❌ | Hawkynt |
| LZH/LHA | ✅ | ❌ | ❌ | Hawkynt |
| 240+ formats | ✅ best-effort | ❌ | ⚠️ password | Hawkynt FormatRegistry |

## Limits

| Parameter | Limit |
|---|---|
| Text preview size | 256 KiB (`PreviewService.MaxTextBytes`) |
| Hex preview size | 64 KiB (`PreviewService.MaxHexBytes`) |
| Compression level | 0 (store) … 10 (insane) |
| Split part sizes | CLI parses `K`/`M`/`G` suffixes; GUI presets 100 MB–4 GB |
| Password length | No explicit cap (UTF-8) |
| Hash algorithms | MD5, SHA-1, SHA-256, SHA-512 |
