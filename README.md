<p align="center">
  <img src="docs/social-preview.jpg" alt="Arcana — modern, cross-platform archiver"/>
</p>

# Arcana

<p align="center">
  <b>The versatile, professional archiver.</b><br/>
  One tool for every archive you'll ever touch — open, create, convert, encrypt, split, hash and preview.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-8B5CF6" alt="Platforms"/>
  <img src="https://img.shields.io/badge/.NET-10-512BD4" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/engines-17-10B981" alt="17 archive engines"/>
  <img src="https://img.shields.io/badge/formats-240%2B-0EA5E9" alt="240+ formats via fallback engine"/>
  <img src="https://img.shields.io/badge/encryption-AES--256--GCM%20%2B%20Argon2id-F43F5E" alt="Encryption"/>
  <img src="https://img.shields.io/badge/license-GPLv3-64748B" alt="GPLv3"/>
</p>

---

**Arcana** is a fast, open-source file archiver built with C# and Avalonia. It combines the format coverage of a classic archiver with a modern, clean UI — and a full command-line toolkit underneath. Whether you're packing a release, opening a legacy archive from 1995, splitting a file for upload, or checking a checksum, Arcana is built to handle it.

- 🖥️ **One app for Windows, macOS and Linux** — native Avalonia UI, dark theme included
- 🗂️ **17 built-in engines + fallback support for 240+ formats** — from ZIP and 7z to ACE, ARJ, CAB, LZH, and everything in between
- 🚀 **Modern compression** — Zstandard, Brotli, LZ4, LZMA, XZ, Snappy alongside the classics
- 🔐 **Strong encryption** — AES-256-GCM authenticated encryption, Argon2id key derivation
- 🧰 **Built-in tools** — split, join, hash, convert and benchmark, no extra downloads
- 👀 **Instant preview** — text, images and hex dumps right inside the app
- 🧩 **GUI + CLI** — a desktop app for day-to-day work, a scriptable CLI for automation

---

## ✨ Highlights

**Explore like a pro.** Folders in the sidebar, files in the list, double-click to navigate, enter/back to go in and out. Classic explorer-style navigation, done right.

**Preview without extracting.** Select a file — see its contents instantly. Text and images load automatically; binary files wait for your "Binary Preview" click instead of flooding the screen with hex.

**Do everything in one place.**
- Split big files into parts (with optional HJSplit-compatible naming) and join them back
- Calculate and verify MD5, SHA-1, SHA-256 and SHA-512 hashes
- Convert archives between formats
- Benchmark engines to pick the fastest for your workload

## 🗜️ Supported Formats

| Format | Read | Write | Encrypt |
|---|---|---|---|
| ZIP | ✅ | ✅ | ✅ |
| 7z | ✅ | ✅ | ✅ |
| Zstandard | ✅ | ✅ | — |
| Tar (+ gz / bz2 / xz / zst) | ✅ | ✅ | — |
| GZip | ✅ | ✅ | — |
| BZip2 | ✅ | ✅ | — |
| XZ | ✅ | ✅ | — |
| LZMA | ✅ | ✅ | — |
| LZ4 | ✅ | ✅ | — |
| Snappy | ✅ | ✅ | — |
| Brotli | ✅ | ✅ | — |
| RAR | ✅ | — | — |
| ACE | ✅ | — | — |
| ARJ | ✅ | — | — |
| CAB | ✅ | — | — |
| LZH / LHA | ✅ | — | — |

Plus a **fallback engine** that extends read support to 240+ archive formats.

## 🔐 Security

Arcana encrypts with **AES-256-GCM** — authenticated encryption that detects any tampering — and derives keys with **Argon2id**, the memory-hard password hash that resists GPU cracking. Your data stays private and verifiable.

## 🧰 Tools

| Tool | What it does |
|---|---|
| **Split** | Split any file into parts; presets from 100 MB to 4 GB, or custom size. Optional HJSplit-compatible naming (`.001`, `.002`…) |
| **Join** | Reassemble parts back into the original file; auto-discovers part sequences |
| **Hash** | MD5, SHA-1, SHA-256, SHA-512 — calculate and verify |
| **Convert** | Transcode archives between supported formats |
| **Benchmark** | Measure engine speed and pick the right format for your data |

## 🚀 Quick Start

```shell
# Prerequisites: .NET 10 SDK
git clone https://github.com/marcelofrau/arcana
cd arcana

# Desktop app
dotnet run --project src/Arcana.App

# CLI help
dotnet run --project src/Arcana.Cli -- --help
```

### CLI examples

```shell
# Compress with Zstandard
arcana compress release/ --format zstd --output release.arc

# Extract a 7z (or any supported archive)
arcana extract backup.7z

# List archive contents
arcana list archive.zip

# Split a file into 100 MB parts (HJSplit-compatible naming)
arcana split movie.iso -s 100M --hjsplit -o parts/

# Join parts back
arcana join "parts/movie.iso.001" -o .

# Calculate a hash
arcana hash setup.exe --algorithm sha256

# Verify a hash
arcana hash setup.exe --algorithm sha256 --verify "ab12cd34..."

# Convert formats
arcana convert backup.7z --format zip

# Benchmark engines
arcana benchmark
```

## 🛠️ Building

```shell
dotnet build src/Arcana.slnx
dotnet test  src/Arcana.slnx   # 145 tests
```

## 📚 Documentation

| Document | Description |
|---|---|
| [Architecture](docs/ARCHITECTURE.md) | System design, layers, data flow |
| [Specifications](docs/SPECS.md) | Functional and non-functional requirements |
| [Roadmap](docs/ROADMAP.md) | Milestones and timeline |
| [Formats](docs/compression/FORMATS.md) | Supported compression formats details |
| [Contributing](docs/contributing/CODING_STANDARDS.md) | Coding guidelines and PR workflow |

## 🧭 Roadmap

- GUI polish: drag-and-drop, in-app archive editing
- **ChaCha20-Poly1305** encryption alongside AES-GCM
- GitHub Actions CI
- Image preview & conversion

## 📄 License

Arcana is free software, released under the **GNU General Public License v3**.

See [LICENSE](LICENSE) for the full text.
