# Production readiness

This is an active hardening release, not a claim of feature parity with WinRAR, 7-Zip or PeaZip.

## Implemented foundation

- Shared extraction with destination containment, link checks, conflict handling and atomic per-file writes.
- Atomic archive replacement; errors and cancellation preserve the previous destination.
- Selected files and empty directories; CRC/length checks before publishing extracted content.
- ZIP AES-256 (WinZip-compatible) writing using SharpZipLib; legacy Arcana encrypted ZIP/7z/Zstd reading retained.
- Explicit format selection, TAR compound extensions and rejection of multi-file input to a single-file codec.
- ZIP listing and content loading on demand; disk-backed decoded content for TAR, 7z, RAR and streaming codecs.
- Managed Zstandard streams and configurable worker count; no native ZstdNet dependency.
- Save/discard/cancel for pending edits, recursive folder imports, folder rename propagation, global search and external opening.
- CLI integration tests and packaged executable smoke tests.

## Current capability limits

- Encrypted **7z writing is disabled**. The installed managed writers do not provide a verified interoperable implementation. Reading native encrypted 7z and legacy Arcana envelopes remains supported.
- The managed 7z, XZ and LZMA writers buffer input. App/CLI writes using these encoders are limited to 256 MiB; use ZIP or TAR + Zstandard for larger jobs.
- Extraction/decoded caches are limited to 64 GiB per operation/archive, 100,000 VFS nodes and 128 path components. These are explicit resource ceilings, not advertised format limits.
- 7z/RAR/TAR opening may decode to temporary disk storage before browsing. ZIP and ISO avoid full decompression when listing.
- Single-file formats do not preserve a directory tree. Use TAR + compression for trees.
- Symlink/junction import and linked extraction destinations are rejected. Unix permissions, ACLs, alternate data streams and all legacy-format variations are not yet a certified backup-preservation contract.
- Per-file extraction is atomic; cancellation can leave already completed files and created directories. Incomplete output files are removed.
- Existing filesystem links are checked before writing and publishing. A separate process with write access racing to replace parent directories is outside the current path-based extraction protection.

## Distribution

`build/build-release.ps1 -Version 0.1.0 -Installer` publishes Windows portable ZIP and a per-user installer (requires Inno Setup 6). Installer tasks offer file associations and Explorer context-menu entries without replacing the user's default ZIP application.

`bash build/build-release.sh 0.1.0 x64` publishes the portable ZIP plus a Debian package on Linux or an application bundle/DMG on macOS. Architecture may be `x64` or `arm64`.

The GUI accepts `archive-path`, `--extract archive-path`, and `--create file-or-folder ...`. Linux installs `arcana` (CLI) and `arcana-gui` (desktop).

Public Windows signing and macOS Developer ID/notarization require the publisher's certificates and credentials. macOS packaging currently uses ad-hoc signing for local testing. Cross-platform installers and file associations must be exercised on their target systems before a stable release.

## Release acceptance

1. `dotnet build src/Arcana.slnx -c Release` and all solution tests pass.
2. The published CLI passes `build/smoke-test.ps1` on the target architecture.
3. Native desktop smoke tests cover create, open, password retry, save/discard/cancel, extraction conflicts, keyboard navigation, high DPI and assistive technology.
4. External fixture tests and representative large-archive measurements support every format/capability advertised as stable.
5. Signed/notarized installers, third-party notices and reproducible version information are verified before public stable distribution.
