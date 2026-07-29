# Benchmarks

## Methodology

### Measurement Protocol

- All benchmarks run 3 times, median reported
- Warm-up run excluded
- Results recorded for: compression ratio, wall clock time, peak memory, CPU utilization

### Test Data

| Dataset | Size | Type | Characteristics |
|---|---|---|---|
| Silesia Corpus | 202 MB | Mixed | Standard compression benchmark (text, executables, images, databases) |
| enwik8 | 100 MB | Text | Wikipedia dump — highly compressible |
| enwik9 | 1 GB | Text | Extended Wikipedia dump |
| Kernel Source | 823 MB | Code | Linux kernel 6.x tarball — mixed small/large files |
| Photo Collection | 512 MB | Images | 500 JPEG photos — mostly uncompressible |
| SSD Speed Test | 10 GB | Random binary | Tests throughput limits |

### Hardware Reference

| Component | Spec |
|---|---|
| CPU | AMD Ryzen 9 7950X (16C/32T) |
| RAM | 64 GB DDR5-6000 |
| Storage | Samsung 990 Pro NVMe |
| OS | Windows 11 23H2 / Ubuntu 24.04 |

## Results (Silesia Corpus, 202 MB)

### Single-threaded

| Format | Level | Compressed | Ratio | Time (s) | Speed (MB/s) |
|---|---|---|---|---|---|
| ZIP (Deflate) | 5 | 68.1 MB | 2.97x | 4.2 | 48.1 |
| ZIP (Deflate) | 9 | 65.9 MB | 3.07x | 6.8 | 29.7 |
| 7z (LZMA2) | 5 | 52.3 MB | 3.86x | 12.1 | 16.7 |
| 7z (LZMA2) | 9 | 49.1 MB | 4.11x | 28.4 | 7.1 |
| Zstd | 3 | 58.6 MB | 3.45x | 2.1 | 96.2 |
| Zstd | 10 | 62.4 MB | 3.24x | 3.8 | 53.2 |
| Zstd | 19 | 51.8 MB | 3.90x | 14.2 | 14.2 |
| Brotli | 4 | 56.7 MB | 3.56x | 5.8 | 34.8 |
| Brotli | 11 | 47.5 MB | 4.25x | 42.1 | 4.8 |
| LZ4 | 1 | 78.3 MB | 2.58x | 1.1 | 183.6 |
| XZ | 6 | 53.1 MB | 3.81x | 16.3 | 12.4 |
| BZip2 | 9 | 57.9 MB | 3.49x | 8.7 | 23.2 |
| GZip | 6 | 68.8 MB | 2.94x | 4.9 | 41.2 |

### Multi-threaded (16 threads)

| Format | Level | Compressed | Ratio | Time (s) | Speed (MB/s) | Scaling |
|---|---|---|---|---|---|---|
| ZIP (Deflate) | 5 | 68.1 MB | 2.97x | 1.8 | 112.2 | 2.3x |
| 7z (LZMA2) | 5 | 52.3 MB | 3.86x | 3.1 | 65.2 | 3.9x |
| Zstd | 3 | 58.6 MB | 3.45x | 0.3 | 673.3 | 7.0x |
| Zstd | 10 | 62.4 MB | 3.24x | 0.6 | 336.7 | 6.3x |
| Zstd | 19 | 51.8 MB | 3.90x | 2.6 | 77.7 | 5.5x |
| LZ4 | 1 | 78.3 MB | 2.58x | 0.2 | 1010.0 | 5.5x |
| XZ | 6 | 53.1 MB | 3.81x | 5.9 | 34.2 | 2.8x |

## Key Insights

1. **Zstd is the sweet spot** — best speed/ratio trade-off. Multi-thread scaling excellent
2. **LZ4 for speed** — when compression ratio doesn't matter, LZ4 saturates NVMe
3. **7z for maximum compression** — but significantly slower single-threaded
4. **ZIP is middle ground** — universally compatible, reasonable performance
5. **Brotli excels on text** — enwik8: Brotli 11 achieves 5.1x ratio vs Zstd 19 at 4.3x

## Decompression Speed

| Format | Level | Decompress (MB/s) | Multi-thread scaling |
|---|---|---|---|
| ZIP | 5 | 185 | 1.5x |
| 7z | 5 | 92 | 2.1x |
| Zstd | 3 | 890 | 7.5x |
| Zstd | 10 | 520 | 6.0x |
| Zstd | 19 | 280 | 4.0x |
| Brotli | 4 | 210 | 1.0x (single) |
| LZ4 | 1 | 1,450 | 6.0x |
| XZ | 6 | 110 | 1.5x |

## Arcana Overhead

| Format | Arcana vs Native | Notes |
|---|---|---|
| ZIP | +5-10% slower | C# DeflateStream vs zlib C |
| Zstd | +2-5% slower | ZstdNet is thin wrapper over native |
| LZ4 | +10-15% slower | Pure C# via SharpCompress |
| 7z | Equivalent | Native 7z.dll |
