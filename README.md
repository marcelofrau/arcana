![Arcana](docs/assets/arcana-banner.png)

# Arcana

**Modern, cross-platform compression toolkit for the next generation.**

Arcana is a fast, open-source file archiver built with C# and Avalonia UI. It supports modern compression formats, strong encryption, and a rich toolset — all wrapped in a responsive, native UI for Windows, macOS, and Linux.

```shell
# Coming soon
arcana compress source/*.txt --format zstd --output archive.arc
arcana extract archive.7z --password-file secrets.key
arcana list archive.zip
```

## Why Arcana?

| Feature | Arcana | WinRAR | 7-Zip | PeaZip |
|---|---|---|---|---|
| Cross-platform | ✅ Native (Win/Mac/Linux) | ❌ Windows only | ❌ Windows + wine | ✅ Java |
| Modern formats (zstd, brotli) | ✅ | ❌ | ❌ | ⚠️ Partial |
| Multi-threaded compression | ✅ | ❌ | ⚠️ Limited | ⚠️ Limited |
| Modern encryption (AES-GCM, ChaCha20) | ✅ | ❌ AES-CBC only | ❌ AES-CBC only | ❌ AES-CBC only |
| Internal file preview & edit | ✅ | ❌ | ❌ | ❌ |
| Built-in tools (split, hash, convert) | ✅ | ❌ | ❌ | ⚠️ Partial |
| Open source (GPLv3) | ✅ | ❌ | ✅ LGPL | ✅ LGPL |
| Modern UI (Avalonia) | ✅ | ❌ | ❌ | ❌ |

## Features

- **Compression**: ZIP, 7z, Zstandard, Brotli, LZ4, LZMA, XZ, BZip2, GZip, Tar
- **Encryption**: AES-256-GCM, ChaCha20-Poly1305, Argon2id key derivation
- **Archive editing**: Open, browse, edit, add, delete files inside archives
- **Preview**: Text (syntax highlighted), images, hex dump, metadata
- **Tools**: File split/join, hash calculator (SHA, BLAKE2), image converter, batch processor
- **Performance**: Multi-threaded compression, SSD-aware I/O, async everywhere
- **CLI + GUI**: Full command-line interface and rich desktop application

## Supported Formats

| Format | Read | Write | Encrypt | Multi-thread |
|---|---|---|---|---|
| ZIP | ✅ | ✅ | ✅ | ✅ |
| 7z | ✅ | ✅ | ✅ | ✅ |
| Zstandard | ✅ | ✅ | ✅ | ✅ |
| Brotli | ✅ | ✅ | ❌ | ✅ |
| LZ4 | ✅ | ✅ | ❌ | ✅ |
| TAR | ✅ | ✅ | N/A | ✅ |
| GZip | ✅ | ✅ | ❌ | ❌ |
| BZip2 | ✅ | ✅ | ❌ | ❌ |
| XZ | ✅ | ✅ | ❌ | ❌ |
| RAR | ✅ (read) | ❌ | N/A | N/A |

## Quick Start

```shell
# Prerequisites: .NET 8 SDK
git clone https://github.com/yourusername/arcana
cd arcana

# Run the desktop app
dotnet run --project src/Arcana.App

# Run the CLI
dotnet run --project src/Arcana.Cli -- --help
```

## Documentation

| Document | Description |
|---|---|
| [Architecture](docs/ARCHITECTURE.md) | System design, layers, data flow |
| [Specifications](docs/SPECS.md) | Functional and non-functional requirements |
| [Roadmap](docs/ROADMAP.md) | Milestones and timeline |
| [Formats](docs/compression/FORMATS.md) | Supported compression formats details |
| [API Reference](docs/api/CORE_API.md) | Public API documentation |
| [Contributing](docs/contributing/CODING_STANDARDS.md) | Coding guidelines and PR workflow |

## Building

```shell
# Debug build
dotnet build src/Arcana.sln

# Release build
powershell -File build/build-release.ps1 -Version 0.1.0 -Arch win-x64
```

## License

Arcana is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

See [LICENSE](LICENSE) for the full license text.
