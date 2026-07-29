# Coding Standards

## Language

C# 12 (.NET 8). All projects use the latest language features.

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Namespaces | PascalCase, file-scoped | `Arcana.Core.Compression` |
| Classes/Structs | PascalCase, noun | `ArchiveEntry`, `CompressionOptions` |
| Interfaces | PascalCase, `I` prefix | `IArchiveFormat`, `ICompressionEngine` |
| Methods | PascalCase, verb | `OpenAsync()`, `GetDirtyNodes()` |
| Properties | PascalCase, noun | `IsEncrypted`, `CompressedSize` |
| Public fields | PascalCase | `MaxRetries` |
| Private fields | `_camelCase` | `_disposed`, `_currentFile` |
| Parameters | camelCase | `sourcePath`, `outputStream` |
| Local variables | camelCase | `archive`, `entry` |
| Constants | PascalCase | `DefaultBufferSize` |
| Enums | PascalCase (singular) | `CompressionFormat`, `NodeType` |
| Boolean properties | Affirmative | `IsDirty`, `HasChildren`, `CanRead` |

## File Organization

```csharp
// Copyright header (if any)
// License header (GPLv3 preamble)

namespace Arcana.Core.Compression;

public class ZipEngine : IArchiveFormat
{
    // Constants
    private const int DefaultBufferSize = 81920;

    // Private fields
    private bool _disposed;

    // Properties
    public string Name => "ZIP";
    public string Extension => ".zip";

    // Constructor
    public ZipEngine()
    {
    }

    // Public methods
    public async Task<Archive> OpenAsync(...)
    {
    }

    // Private methods
    private static int DeflateBufferSize(long fileSize)
    {
    }
}
```

## Formatting

- Indentation: 4 spaces (no tabs)
- Braces: Allman style (next line)
- `csharp_new_line_before_open_brace = all`
- `csharp_prefer_braces = true`
- Maximum line length: 120 characters (soft limit)
- One blank line between members, two between types

Example:

```csharp
public async Task<Archive> OpenAsync(
    string path,
    Stream stream,
    AccessMode mode,
    CancellationToken ct = default)
{
    if (string.IsNullOrEmpty(path))
    {
        throw new ArgumentException("Path cannot be empty", nameof(path));
    }

    ct.ThrowIfCancellationRequested();

    var entries = await Task.Run(
        () => SharpCompressReader.ReadEntries(stream),
        ct);

    return new Archive
    {
        Format = CompressionFormat.Zip,
        FormatEngine = this,
        Entries = entries.ToList()
    };
}
```

## Async Patterns

- All I/O-bound methods return `Task`/`Task<T>`
- All CPU-bound parallel work uses `Parallel.ForEach` or `Task.Run`
- `CancellationToken` as last parameter with default `default`
- `IProgress<T>` for progress reporting
- `ConfigureAwait(false)` in library code (not in UI code)

```csharp
public async Task CompressAsync(
    Stream source,
    Stream destination,
    CompressionLevel level,
    IProgress<ProgressReport>? progress = null,
    CancellationToken ct = default)
{
    await Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        // Synchronous compression work
        InternalCompress(source, destination, level, progress);
    }, ct).ConfigureAwait(false);
}
```

## Error Handling

- Use exceptions for exceptional cases only
- Use `Result<T>` pattern for expected failures (future)
- Always provide meaningful error messages
- Wrap external library exceptions in Arcana-specific exceptions

```csharp
public class ArchiveException : Exception
{
    public ArchiveException(string message) : base(message) { }
    public ArchiveException(string message, Exception inner) : base(message, inner) { }
}

public class CorruptArchiveException : ArchiveException { }
public class WrongPasswordException : ArchiveException { }
public class UnsupportedFormatException : ArchiveException { }
```

## XML Documentation

All public API members must have XML doc comments:

```csharp
/// <summary>
/// Opens an archive from the specified stream.
/// </summary>
/// <param name="path">Path to the archive file (for reference only).</param>
/// <param name="stream">Stream containing the archive data.</param>
/// <param name="mode">Access mode (Read, Write, or ReadWrite).</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>An <see cref="Archive"/> instance representing the opened archive.</returns>
/// <exception cref="CorruptArchiveException">Thrown when the archive data is invalid.</exception>
/// <exception cref="UnsupportedFormatException">Thrown when the format is not recognized.</exception>
public Task<Archive> OpenAsync(
    string path,
    Stream stream,
    AccessMode mode,
    CancellationToken ct = default);
```

## Commit Messages

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <short description>

[optional body]

[optional footer]
```

See [BRANCH_STRATEGY.md](../BRANCH_STRATEGY.md) for commit type reference.

## Code Analyzers

The project uses:

- `.editorconfig` — formatting rules (enforced in CI)
- Roslyn analyzers (implicit via .NET SDK)
- No StyleCop — prefer `.editorconfig` built-in rules

Run before committing:

```shell
dotnet build src/Arcana.sln
dotnet test src/Arcana.sln
```
