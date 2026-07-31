# Versioning Policy

Arcana uses a semantic-prefix build scheme derived from `src/Directory.Build.props`.

## Format

```
{MAJOR}.{MINOR}.{PATCH}-build.{N}+{githash}
```

Example: `0.1.0-build.2+5d272a8bf6…`

| Component | Source |
|---|---|
| `MAJOR.MINOR.PATCH` | `Directory.Build.props` (`VersionMajor/Minor/Patch`) |
| `build.{N}` | `build/build-counter.txt` (`prefix\|counter`) |
| `+{githash}` | Auto-appended by the SDK (`SourceRevisionId`) |

## Build Counter

`build/build-counter.txt` stores `{prefix}|{counter}` (currently `0.1.0|2`).

- Increment: `powershell -File build/increment-version.ps1`
- Counter resets to 1 when the version prefix changes
- The prefix and counter are validated at build time against `Directory.Build.props`

## Assembly Versioning

| Attribute | Value |
|---|---|
| `AssemblyVersion` | `{prefix}.{N}` |
| `FileVersion` | `{prefix}.{N}` |
| `InformationalVersion` | `{prefix}-build.{N}` (+ git hash) |
| `Version` | `{prefix}-build.{N}` |

`arcana --version` is emitted automatically by System.CommandLine v2 from the assembly's informational version. The GUI About dialog reads the runtime assembly version (not hardcoded).

## When to Bump

| Change | Action |
|---|---|
| Breaking change to public API / format | Bump MAJOR (and increment counter) |
| New backward-compatible feature | Bump MINOR (and increment counter) |
| Bug fix / docs / perf | Bump PATCH (and increment counter) |
| Any release | Run `increment-version.ps1` + tag `v{prefix}-build.{N}` |

## v0.x Exceptions

While MAJOR = 0, the public API is not considered stable; minor bumps may include breaking changes.

## Tagging

Releases on `main` are tagged, e.g.:

```shell
git tag v0.1.0-build.2 -m "v0.1.0-build.2"
```

## Compatibility Policy

| Compatibility Type | Guarantee from v1.0.0+ |
|---|---|
| Source compatibility | Same public API compiles without changes |
| Binary compatibility | Same assembly works without recompilation |
| Archive format backward compatibility | New version reads files created by any older version |
| Archive format forward compatibility | Old versions may NOT read files created by newer version |
