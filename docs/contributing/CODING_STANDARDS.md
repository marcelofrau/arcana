# Coding Standards

## Language

C# 12/13, .NET 10. All projects use modern language features (records, pattern matching, primary constructors where idiomatic).

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Namespaces | PascalCase, file-scoped | `Arcana.Core.Compression` |
| Classes/Structs | PascalCase, noun | `ArchiveEntry`, `CompressionOptions` |
| Interfaces | PascalCase, `I` prefix | `IArchiveFormat`, `IIconProvider` |
| Methods | PascalCase, verb | `OpenAsync()`, `SyncNodeMetadata()` |
| Properties | PascalCase, noun | `IsEncrypted`, `CompressedSize` |
| Private fields | `_camelCase` | `_disposed`, `_currentFile` |
| Parameters | camelCase | `sourcePath`, `outputStream` |
| Local variables | camelCase | `archive`, `entry` |
| Constants | PascalCase | `DefaultBufferSize` |
| Enums | PascalCase (singular) | `CompressionFormat`, `NodeType` |
| Boolean properties | Affirmative | `IsDirty`, `CanRead`, `HasDirtyNodes` |

## File Organization

```csharp
// License header: GPL-3.0-or-later preamble

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

## Async Patterns

- I/O-bound methods return `Task`/`Task<T>`
- `CancellationToken` as last parameter with default `default`
- `IProgress<T>` for progress reporting
- `ConfigureAwait(false)` in library code (not in UI code)

## Error Handling

- Exceptions for exceptional cases only
- Wrap external library exceptions in Arcana-specific exceptions where a caller needs a stable contract (`NotSupportedException` for write-unsupported formats)
- Always provide meaningful error messages

## XML Documentation

All public API members in `Arcana.Core` should have XML doc comments.

## MVVM (Arcana.App)

- ViewModels use CommunityToolkit.Mvvm source generators: `[ObservableProperty]`, `[RelayCommand]`
- Bindings use compiled bindings (`x:DataType`) — enabled project-wide
- No business logic in code-behind; code-behind only wires view concerns
- UI work on `Dispatcher.UIThread`; background work via `Task.Run` + `IProgress<T>`

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>

[optional body]

[optional footer]
```

| Type | Usage |
|---|---|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation |
| `style` | Formatting only |
| `refactor` | Code change without feature/fix |
| `perf` | Performance improvement |
| `test` | Adding/fixing tests |
| `chore` | Build, CI, deps |
| `release` | Release commit |

See [BRANCH_STRATEGY.md](../BRANCH_STRATEGY.md) for the workflow.

## Code Analyzers

- `.editorconfig` — formatting rules
- Roslyn analyzers (implicit via .NET SDK)
- No StyleCop

Run before committing:

```shell
dotnet build src/Arcana.slnx
dotnet test  src/Arcana.slnx
```
