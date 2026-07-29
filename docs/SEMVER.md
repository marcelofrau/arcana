# Versioning Policy

Arcana follows [Semantic Versioning 2.0.0](https://semver.org/).

## Format

```
v<MAJOR>.<MINOR>.<PATCH>[-<PRE-RELEASE>[.<BUILD>]]
```

## Rules

| Increment | When | Example |
|---|---|---|
| **MAJOR** | Breaking change to public API, archive format incompatibility, UI paradigm shift | `v1.0.0` → `v2.0.0` |
| **MINOR** | New feature backward-compatible, new format support, new tool | `v0.1.0` → `v0.2.0` |
| **PATCH** | Bug fix, performance improvement, documentation | `v0.1.0` → `v0.1.1` |

## Pre-release Labels

| Label | Meaning | Example |
|---|---|---|
| `alpha` | Work in progress, unstable API, may break | `v0.1.0-alpha.1` |
| `beta` | Feature complete, testing phase, API stable | `v0.1.0-beta.1` |
| `rc` | Release candidate, final testing | `v0.1.0-rc.1` |

## v0.x Exceptions

While in initial development (major version 0), the following applies:

- **MINOR** increments can include breaking changes
- Public API is not considered stable until v1.0.0
- Pre-release labels strongly recommended for all v0.x releases

## Compatibility Policy

| Compatibility Type | Guarantee from v1.0.0+ |
|---|---|
| Source compatibility | Same public API compiles without changes |
| Binary compatibility | Same assembly works without recompilation |
| Archive format backward compatibility | New version reads files created by any older version |
| Archive format forward compatibility | Old versions may NOT read files created by newer version |

## Version Lifecycle

```
develop ──→ release/v0.1.0 ──→ main (v0.1.0-alpha.1)
                                        ↓
                              fix bugs, stabilize
                                        ↓
                              main (v0.1.0-rc.1)
                                        ↓
                              final testing
                                        ↓
                              main (v0.1.0) 🎉
                                        ↓
                              bug fixes → v0.1.1, v0.1.2, ...
                                        ↓
                              develop (v0.2.0-dev)
```

## Tagging

All releases on `main` branch are tagged:

```shell
git tag -s v0.1.0 -m "v0.1.0: MVP release"
git tag -s v0.1.1 -m "v0.1.1: Fix ZIP UTF-8 encoding"
```

Tags are signed with the developer's GPG key.
