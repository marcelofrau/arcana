# Execution Plan

## Prerequisites

- .NET 8 SDK (`dotnet --version` ≥ 8.0.x)
- PowerShell 7+ (Windows) or bash (macOS/Linux)
- Git

## Setup Steps

### 1. Project Initialization

```shell
cd arcana

# Create solution
dotnet new sln -n Arcana -o src
dotnet sln src/Arcana.sln add src/Arcana.Core
dotnet sln src/Arcana.sln add src/Arcana.Cli
dotnet sln src/Arcana.sln add src/Arcana.App
dotnet sln src/Arcana.sln add tests/Arcana.Core.Tests
dotnet sln src/Arcana.sln add tests/Arcana.App.Tests
```

### 2. Project Dependencies

| Project | References |
|---|---|
| Arcana.Core | (none) — pure library |
| Arcana.Cli | Arcana.Core, System.CommandLine |
| Arcana.App | Arcana.Core, Avalonia.Desktop, CommunityToolkit.Mvvm |
| Arcana.Core.Tests | Arcana.Core, xUnit, FluentAssertions |
| Arcana.App.Tests | Arcana.App, xUnit, FluentAssertions |

### 3. Package Installation

```shell
# Arcana.Core
dotnet add src/Arcana.Core package SharpCompress
dotnet add src/Arcana.Core package ZstdNet
dotnet add src/Arcana.Core package Konscious.Security.Cryptography.Argon2

# Arcana.Cli
dotnet add src/Arcana.Cli package System.CommandLine

# Arcana.App
dotnet add src/Arcana.App package Avalonia
dotnet add src/Arcana.App package Avalonia.Desktop
dotnet add src/Arcana.App package CommunityToolkit.Mvvm
dotnet add src/Arcana.App package Microsoft.Extensions.DependencyInjection

# Tests
dotnet add tests/Arcana.Core.Tests package xUnit
dotnet add tests/Arcana.Core.Tests package FluentAssertions
```

### 4. Build Verification

```shell
dotnet restore src/Arcana.sln
dotnet build src/Arcana.sln -c Debug
```

## Short-term Tasks (Weeks 1-4)

### Week 1: Foundation

- [ ] Solution + project files created
- [ ] CI workflow (GitHub Actions) — build + test on push
- [ ] Core interfaces defined (`IArchiveFormat`, `ICompressionEngine`)
- [ ] `ArchiveEntry`, `CompressionOptions`, `ProgressReport` models
- [ ] Basic `ZipEngine` (read-only: list + extract)
- [ ] Unit tests for ZipEngine

### Week 2: CLI MVP

- [ ] `arcana list` command (list archive contents)
- [ ] `arcana extract` command (extract archive)
- [ ] `arcana compress` command (basic ZIP creation)
- [ ] Integration tests for CLI

### Week 3: UI Shell

- [ ] Avalonia project with MainWindow
- [ ] File system tree view (non-archive browsing)
- [ ] Open archive dialog
- [ ] Archive contents display (grid + tree)
- [ ] Extract button + dialog

### Week 4: Polish + Release

- [ ] Error handling across all layers
- [ ] Progress reporting (IProgress<T>)
- [ ] Cancellation support (CancellationToken)
- [ ] v0.1.0-alpha release

## Architecture Decisions to Make

| Decision | Options | Target Decision |
|---|---|---|
| Async model for IArchiveFormat | `Task<IAsyncEnumerable<...>>` vs callback | Callback + IProgress |
| VFS storage mode | Memory-mapped files vs in-memory | In-memory with lazy load |
| Encryption key storage | Key file format (JSON vs binary) | Libsodium sealed box |
| Plugin discovery | Convention-based vs attribute-based | Attribute-based (future) |
| Shell extension | Native (COM) vs registry | Native (Windows only, v0.4+) |

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| SharpCompress 7z write not complete | Medium | High | Fallback to 7z CLI or 7zipSharp |
| Avalonia mobile stability | Medium | Medium | Delay mobile milestone |
| Cross-platform I/O differences | Low | Medium | Abstract file system early |
| Performance overhead of C# vs C++ | Medium | Medium | Critical paths in native, parallel where possible |
