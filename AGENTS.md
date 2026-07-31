Build env
- SDK: dotnet 10.0 (C# 12)
- Solution: `src/Arcana.slnx` (Core/Cli/App/Mobile/Core.Tests/App.Tests)
- Build: `dotnet build src/Arcana.slnx`
- Test: `dotnet test src/Arcana.slnx` — 155 tests (152 Core + 3 App)
- Run CLI: `dotnet run --project src/Arcana.Cli -- <args>`
- Build scripts: `build/clean.ps1`, `build/increment-version.ps1`, `build/build-counter.txt` (`prefix|counter`, resets to 1 on prefix bump)

Packages
- SharpCompress 0.50.1, ZstdNet, Konscious.Argon2
- System.CommandLine 2.0.10, Spectre.Console 0.57.2
- Avalonia 12.1 + CommunityToolkit.Mvvm
- Hawkynt.FileFormats.Archives 1.0.0.696 (pure C#, ~240 format DLLs via FormatRegistry)
- Serilog + Serilog.Sinks.Console
- K4os.Compression.LZ4 1.3.8, Snappy.Sharp 1.0.0
- No ReactiveUI, no SixLabors.ImageSharp

Projects
- Arcana.Core — 17 engines, VFS, crypto (AES-256-GCM + Argon2id), tools
- Arcana.Cli — System.CommandLine v2 API, 8 commands, Serilog
- Arcana.App — Avalonia (basic shell, stub VMs)
- Arcana.Core.Tests — xUnit + FluentAssertions, 134 tests
- Arcana.App.Tests — xUnit + FluentAssertions, 3 tests

CLI (8 commands)
- `compress`, `extract`, `list`, `convert`, `hash` (--verify), `split` (--hjsplit), `join`, `benchmark`
- `--no-color`, `--log-level` globals
- Output.cs: static Spectre wrapper

Core (Arcana.Core/)
- 17 engines: Zip, SevenZip, Zstd, Tar (+Gz/Bz2/Xz/Zst), Rar, Ace, Arj, Cab, Lzh, Brotli, Gzip, BZip2, Xz, Lzma, Lz4, Snappy, HawkyntFallback
- ArchiveFactory: magic bytes + extension detection, SetPassword, Tar.gz routing, Hawkynt fallback last
- VirtualFileSystem: ArchiveNode tree, ContentFactory lazy loading
- Crypto: EncryptionProvider (AES-256-GCM EncryptStream/DecryptStream, salt embedded), Argon2KeyDerivation
- Tools: FileSplitter, FileJoiner, HashCalculator
- Logging: LogConfig.cs, Serilog across all engines/commands/tools

Engines — Password via engine.Password, wired SaveAsync (EncryptStream) / OpenAsync (DecryptStream or native SharpCompress).

Logging — MANDATE: every code path logs.
- Pattern: `private static readonly ILogger Log = Serilog.Log.ForContext<X>();` (static classes: `ForContext(typeof(X))`).
- Levels: Verbose = inner hot-path detail (icon cache build, format descriptor register); Debug = flow/progress (navigate, preview load, settings load); Information = user-visible milestones (archive open/extract/test/save, add/remove favorite, theme switch, command completion); Warning = recoverable (corrupted settings/favorites, image decode fail, in-place update failure); Error = failure with exception (operation failed, format assembly load fail).
- NEVER log passwords, keys, or archive contents — log "password set" / "encrypting" intent, never the secret value. File paths OK.
- Log location: console + `%APPDATA%\Arcana\logs\arcana-YYYYMMDD.log`. Level: CLI `--log-level`, GUI Settings → LogLevel.
- Rule: any new class with behavior gets an `_log`/`Log` field; when editing code add logs for new branches; no silent empty catches — always log the exception.
- CLI Program.cs logs invocation, duration, exit code; App.axaml.cs logs startup (console bridge + log level).

Git
- github.com/marcelofrau/arcana (private)
- Author: Marcelo Frau <marcelofrau@gmail.com>
- Initial commit: 1e0e77e (project scaffold)

Docs
- ADRs in docs/DECISIONS.md (ADR-0011 superseded: standalone engines implemented)
- Mermaid diagrams
- Version: `{prefix}-build.{N}+{githash}`; CLI `--version` auto (System.CommandLine v2 RootCommand reads assembly info); git hash auto-appended by SDK via SourceRevisionId; About dialog reads runtime assembly version (not hardcoded)

Next
- Avalonia GUI: wire archive open, extract, preview
- GitHub Actions CI
- ChaCha20-Poly1305 implementation
- Image preview + conversion
