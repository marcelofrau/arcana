# Supported Compression Formats

## Format Details

### ZIP

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full |
| Encryption | ✅ AES-256 (WinZip AE-2), ✅ Legacy ZipCrypto |
| Solid | ❌ |
| Volumes | ❌ |
| Unicode | ✅ UTF-8 flag (bit 11) |
| Max file size | 4GB (ZIP32) / 16EB (ZIP64) |
| Max entries | 65,535 (ZIP32) / 2^31-1 (ZIP64) |
| Implementation | SharpCompress |
| Notes | Default format if no other specified |

### 7z

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full (via 7z DLL wrapper) |
| Encryption | ✅ AES-256 (7z native) |
| Solid | ✅ Configurable block size |
| Volumes | ✅ .7z.001, .7z.002, ... |
| Unicode | ✅ Full |
| Max file size | 16EB |
| Max entries | 2^31-1 |
| Implementation | SharpCompress (read) + P/Invoke 7z.dll (write) |

### Zstandard (Zstd)

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full |
| Encryption | ✅ Wrapped in Arcana container |
| Solid | ✅ Single frame or multiple frames |
| Volumes | ❌ (use split at file level) |
| Unicode | N/A (single stream) |
| Max file size | Unlimited |
| Implementation | ZstdNet (native zstd library) |
| Notes | Best speed/ratio balance. Multi-threaded natively |

### Brotli

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full |
| Encryption | ❌ |
| Solid | ❌ |
| Volumes | ❌ |
| Implementation | System.IO.Compression.Brotli (built-in .NET) |
| Notes | Great for text. Single-thread only |

### LZ4

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full |
| Encryption | ❌ |
| Solid | ❌ |
| Implementation | SharpCompress |
| Notes | Extremely fast. Moderate compression |

### TAR

| Property | Value |
|---|---|
| Read | ✅ Full (POSIX, GNU, USTAR) |
| Write | ✅ Full |
| Encryption | N/A (wrap with GZip, Zstd, etc.) |
| Solid | ❌ |
| Volumes | ❌ |
| Unicode | ✅ (GNU extensions) |
| Max file size | 8GB (POSIX) / 8EB (GNU) |
| Implementation | SharpCompress |
| Notes | Always used with external compressor |

### GZip

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full |
| Encryption | ❌ |
| Solid | ❌ |
| Implementation | SharpCompress |
| Notes | Commonly wraps TAR (.tar.gz / .tgz) |

### BZip2

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full |
| Encryption | ❌ |
| Solid | ❌ |
| Implementation | SharpCompress |
| Notes | Better ratio than GZip, slower |

### XZ

| Property | Value |
|---|---|
| Read | ✅ Full |
| Write | ✅ Full |
| Encryption | ❌ |
| Solid | ✅ |
| Implementation | SharpCompress |
| Notes | High compression, moderate speed |

### RAR

| Property | Value |
|---|---|
| Read | ✅ (RAR4 + RAR5) |
| Write | ❌ (patent-encumbered) |
| Encryption | ✅ Detected (AES, decryption via SharpCompress with password) |
| Solid | ✅ |
| Volumes | ✅ |
| Unicode | ✅ |
| Implementation | SharpCompress (read-only) |

## Compression Level Mapping

| Level | Meaning | Example (Zstd) | Example (ZIP) |
|---|---|---|---|
| 0 | Store (no compression) | Store | Store |
| 1 | Fastest | --fast | Level 1 |
| 3 | Fast | Level 1 | Level 3 |
| 5 | Normal (default) | Level 3 | Level 5 |
| 7 | Maximum | Level 10 | Level 7 |
| 9 | Ultra | Level 16 | Level 9 |
| 10+ | Insane | Level 22 | N/A |

## Algorithm Speed Comparison (approximate)

```
Fastest ──────────────────────────────────────────────→ Slowest
LZ4 → Zstd(1) → GZip → Brotli(1) → ZIP → Zstd(10) → XZ → BZip2 → 7z(LZMA2) → Zstd(22) → 7z(PPMd)

Ratio ────────────────────────────────────────────────→ Best
LZ4 → GZip → Zstd(1) → ZIP → Brotli(1) → BZip2 → XZ → Zstd(10) → 7z(LZMA2) → Zstd(22) → 7z(PPMd)
```
