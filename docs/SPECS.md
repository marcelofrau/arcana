# Specifications

## Functional Requirements

### FR-COMPRESS: Compression Operations

| ID | Requirement | Priority |
|---|---|---|
| FR-CMP-01 | User can compress files and directories into ZIP format | P0 |
| FR-CMP-02 | User can compress into 7z format | P0 |
| FR-CMP-03 | User can compress into Zstandard format | P0 |
| FR-CMP-04 | User can compress into Brotli, LZ4, LZMA, XZ, BZip2, GZip formats | P1 |
| FR-CMP-05 | User can create TAR archives (with or without compression) | P1 |
| FR-CMP-06 | User can set compression level (store/fast/normal/maximum/ultra) | P0 |
| FR-CMP-07 | User can enable parallel compression (multi-threaded) | P0 |
| FR-CMP-08 | User can set thread count for parallel operations | P1 |
| FR-CMP-09 | User can add password-based encryption during compression | P0 |
| FR-CMP-10 | User can add key-file-based encryption | P2 |
| FR-CMP-11 | User can split archive into volumes (span) | P1 |
| FR-CMP-12 | User can add file comments and archive comments | P2 |

### FR-EXTRACT: Extraction Operations

| ID | Requirement | Priority |
|---|---|---|
| FR-EXT-01 | User can extract archives in ZIP, 7z, Zstd, Tar, GZip, BZip2, XZ formats | P0 |
| FR-EXT-02 | User can extract RAR archives (read-only) | P1 |
| FR-EXT-03 | User can extract with password | P0 |
| FR-EXT-04 | User can extract single files from archive | P0 |
| FR-EXT-05 | User can extract to specific directory | P0 |
| FR-EXT-06 | User can test archive integrity | P1 |
| FR-EXT-07 | User can extract encrypted archives with key file | P2 |

### FR-BROWSE: Archive Browsing

| ID | Requirement | Priority |
|---|---|---|
| FR-BRW-01 | User can open archive and view its contents as a file tree | P0 |
| FR-BRW-02 | User can sort entries by name, size, date, ratio | P0 |
| FR-BRW-03 | User can search/filter entries by name | P1 |
| FR-BRW-04 | User can view detailed properties of each entry | P0 |
| FR-BRW-05 | User can preview file contents internally | P0 |

### FR-PREVIEW: Internal Preview

| ID | Requirement | Priority |
|---|---|---|
| FR-PRV-01 | User can preview text files with syntax highlighting | P0 |
| FR-PRV-02 | User can preview images (PNG, JPEG, WebP, BMP, GIF) | P0 |
| FR-PRV-03 | User can preview files as hexadecimal dump | P1 |
| FR-PRV-04 | User can preview Markdown files rendered | P1 |
| FR-PRV-05 | User can preview text files and edit them inline | P1 |
| FR-PRV-06 | User can preview file metadata (EXIF, format info) | P2 |

### FR-EDIT: Archive Editing

| ID | Requirement | Priority |
|---|---|---|
| FR-EDT-01 | User can rename files inside archive | P0 |
| FR-EDT-02 | User can delete files from archive | P0 |
| FR-EDT-03 | User can add new files to existing archive | P0 |
| FR-EDT-04 | User can edit text files inside archive and save changes | P1 |
| FR-EDT-05 | User can drag & drop files into archive | P1 |
| FR-EDT-06 | User can reorder files within archive | P2 |

### FR-TOOLS: Utility Tools

| ID | Requirement | Priority |
|---|---|---|
| FR-TLS-01 | User can split files into parts | P0 |
| FR-TLS-02 | User can join split files back | P0 |
| FR-TLS-03 | User can compute file hashes (SHA256, SHA512, BLAKE2, MD5) | P0 |
| FR-TLS-04 | User can verify file integrity via hash file | P1 |
| FR-TLS-05 | User can convert images between formats | P1 |
| FR-TLS-06 | User can batch compress multiple archives | P1 |
| FR-TLS-07 | User can convert archive between formats | P1 |
| FR-TLS-08 | User can benchmark compression performance | P2 |

### FR-CLI: Command-Line Interface

| ID | Requirement | Priority |
|---|---|---|
| FR-CLI-01 | User can run `arcana compress` with source, format, output args | P0 |
| FR-CLI-02 | User can run `arcana extract` with input and output args | P0 |
| FR-CLI-03 | User can run `arcana list` to view archive contents | P0 |
| FR-CLI-04 | User can run `arcana split` and `arcana join` | P0 |
| FR-CLI-05 | User can run `arcana hash` to compute checksums | P0 |
| FR-CLI-06 | User can run `arcana convert` to convert archives | P1 |
| FR-CLI-07 | User can pipe data via stdin/stdout | P2 |

## Non-Functional Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-PRF-01 | Compression speed comparable to 7-Zip on same format | Within 20% | P1 |
| NFR-PRF-02 | Decompression speed comparable to 7-Zip | Within 20% | P1 |
| NFR-PRF-03 | UI startup time | < 2 seconds | P0 |
| NFR-PRF-04 | Memory usage for 4GB archive | < 1GB | P1 |
| NFR-PRF-05 | UI responsiveness during background ops | < 100ms frame time | P0 |
| NFR-SEC-01 | Encryption uses authenticated encryption (AEAD) | AES-256-GCM or ChaCha20-Poly1305 | P0 |
| NFR-SEC-02 | Key derivation uses memory-hard function | Argon2id | P0 |
| NFR-SEC-03 | Sensitive data zeroed in memory after use | SecureClear | P1 |
| NFR-CPT-01 | Cross-platform on Windows, macOS, Linux | Same feature set | P0 |
| NFR-CPT-02 | Unicode filename support | Full | P0 |
| NFR-CPT-03 | Handle files > 4GB | Supported | P0 |
| NFR-CPT-04 | Handle archives with 10k+ entries | Usable UI | P1 |
| NFR-CPT-05 | Graceful handling of corrupted archives | Error message, not crash | P0 |

## Format Support Matrix

| Format | Read | Write | Encrypt | Solid | Volumes | Unicode | Max Size |
|---|---|---|---|---|---|---|---|
| ZIP | ✅ | ✅ | ✅ | ❌ | ❌ | ⚠️ (UTF-8) | 16EB (ZIP64) |
| 7z | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16EB |
| Zstandard | ✅ | ✅ | ✅ | ✅ | ✅ | N/A | Unlimited |
| Brotli | ✅ | ✅ | ❌ | ❌ | ❌ | N/A | Unlimited |
| LZ4 | ✅ | ✅ | ❌ | ❌ | ❌ | N/A | Unlimited |
| TAR | ✅ | ✅ | N/A | ❌ | ❌ | ✅ | 8GB (POSIX) / 8EB (GNU) |
| GZip | ✅ | ✅ | ❌ | ❌ | ❌ | N/A | Unlimited |
| BZip2 | ✅ | ✅ | ❌ | ❌ | ❌ | N/A | Unlimited |
| XZ | ✅ | ✅ | ❌ | ✅ | ❌ | N/A | Unlimited |
| RAR | ✅ (ro) | ❌ | N/A | ✅ | ✅ | ✅ | 16EB |

## Limits

| Parameter | Limit |
|---|---|
| Maximum individual file size | 16 EB (limited by filesystem) |
| Maximum archive entries | 2^31 - 1 (practical: 100k for UI) |
| Maximum path length | 32,767 characters |
| Maximum password length | 512 bytes (UTF-8) |
| Maximum thread count | Number of logical processors |
| Minimum supported file size | 1 byte |
| Supported filename encoding | UTF-8, ASCII, OEM (CP850) |
