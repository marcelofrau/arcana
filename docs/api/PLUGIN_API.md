# Plugin API (Future)

> **Status**: Planned for v2.0.0+. This document describes the intended design.

## Design Goals

- Third-party format plugins without modifying Arcana source
- Loaded at runtime via assembly scanning
- Sandboxed (limited to `IArchiveFormat` interface)
- Versioned and validated at load time

## Plugin Interface

```csharp
public interface IArchiveFormatPlugin
{
    // Identity
    string Id { get; }              // e.g., "com.example.myformat"
    string Name { get; }            // e.g., "My Custom Format"
    string Version { get; }         // e.g., "1.0.0"
    string Author { get; }          // e.g., "Jane Doe"

    // Format metadata
    string Extension { get; }       // e.g., ".myarc"
    string? MagicBytesHex { get; }  // e.g., "4D59415243"

    // Capabilities
    bool CanRead { get; }
    bool CanWrite { get; }
    bool CanEncrypt { get; }

    // Registration
    void Register(IPluginContext context);
}
```

## Plugin Context

```csharp
public interface IPluginContext
{
    void RegisterFormat(IArchiveFormat format);
    IReadOnlyDictionary<string, string> Settings { get; }
    ILogger Logger { get; }
}
```

## Packaging

Plugins are distributed as NuGet packages or standalone DLLs:

```
MyFormatPlugin/
├── MyFormatPlugin.csproj
├── MyFormat.cs              # implements IArchiveFormat
├── MyFormatPlugin.cs        # implements IArchiveFormatPlugin
└── plugin.json              # metadata
```

### plugin.json

```json
{
    "id": "com.example.myformat",
    "name": "My Custom Archive Format",
    "version": "1.0.0",
    "author": "Jane Doe",
    "apiVersion": "2.0",
    "entryPoint": "MyFormatPlugin.MyFormatPlugin, MyFormatPlugin"
}
```

## Plugin Discovery

1. Scan `~/.arcana/plugins/` directory
2. Scan `%LOCALAPPDATA%/Arcana/Plugins/` (Windows)
3. Scan directory specified by `--plugins` flag
4. Load assemblies, find `IArchiveFormatPlugin` implementations
5. Validate API version compatibility
6. Register with `ArchiveFactory`

## Sandbox Notes

- Plugins run in-process but in a separate `AssemblyLoadContext`
- Can be reloaded without restarting the app (future)
- Resource limits (memory, time) enforced at plugin level
