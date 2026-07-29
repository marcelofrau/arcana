# Future Ideas

Long-term wishlist and research topics. Not committed — ideas for inspiration.

## Mobile

- **iOS head**: Avalonia iOS with touch-optimized archive browser
- **Android head**: Same codebase, Android-specific file picker + share sheet
- **Mobile-specific features**: Camera-to-archive (compress photos on-device), cloud import

## Platform Integration

- **Windows Shell Extension**: Context menu "Compress with Arcana" / "Extract here"
- **macOS Quick Action**: Automator/Shortcuts integration
- **Linux Nautilus Plugin**: File manager integration
- **FUSE Mount**: Mount archive as virtual filesystem directory (read-only + write)
- **Arcana Daemon**: File watcher + auto-compression rules (compress *.log after 24h)

## Cloud & Remote

- **S3/Azure Blob/GCS**: Open and edit archives directly from cloud storage
- **SFTP/SSH**: Remote archive operations via SSH.NET
- **WebDAV**: Mount remote storage
- **Arcana Sync**: Sync folder with automatic differential compression

## Format Expansion

- **WIM**: Windows Imaging Format
- **DMG**: macOS disk images
- **ISO**: Optical disc images (read + create from directory)
- **SquashFS**: Linux filesystem compression
- **ZPAQ**: Maximum compression ratio (extremely slow)
- **PAQ8 variants**: Research-grade compression
- **Custom Arcana Format**: `.arc` — tailored container with fast random access, built-in encryption, error correction

## AI Features

- **Compression ratio prediction**: ML model to recommend best format for given file type
- **Smart compression**: Auto-detect best algorithm based on content analysis
- **Content classification**: Detect file types inside archives without extension

## Advanced Tools

- **Deduplication**: Find and remove duplicate files across archives
- **Archive repair**: Recover corrupted archives (RAR recovery record style)
- **Benchmark suite**: Built-in speed/ratio benchmark across all formats
- **Compression comparison**: Side-by-side comparison of formats for same data
- **Password recovery**: GPU-accelerated dictionary attack (ethical only, with consent)
- **Secure shred**: Overwrite deleted files from archives before compaction

## Developer Experience

- **Plugin API**: Third-party format plugins via MEF or source generators
- **WebAssembly**: Run compression in browser via Avalonia.Web
- **Language bindings**: Python bindings via pythonnet for scripting
- **REST API**: HTTP interface for headless operations

## Performance Research

- **SIMD-optimized checksums**: Hardware-accelerated CRC32, SHA, BLAKE2
- **GPU decompression**: OpenCL/CUDA for parallel decompression of solid blocks
- **io_uring**: Linux native async I/O
- **Memory-mapped archives**: Directly map archive for instant random access
