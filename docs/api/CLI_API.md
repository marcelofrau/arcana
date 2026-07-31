# Arcana CLI Reference

## Global Usage

```shell
arcana [command] [options] [arguments]
arcana --version   # implicit via System.CommandLine v2
arcana --help
```

### Global Options

| Option | Description |
|---|---|
| `--no-color` | Disable ANSI colors (Spectre.Console) — also detected pre-parse |
| `--log-level <level>` | Serilog level (trace/debug/info/warning/error/fatal). ⚠️ Option is registered but currently **unused** by the CLI — logging stays at `Warning` |

### Exit Codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Error (exception in command, or validation failure) |

There is no richer exit-code scheme yet. Several commands re-throw inside `Output.Status(...)`, which surfaces as an unhandled exception.

## Commands

### `arcana compress`

Compress files/directories into a **ZIP** archive.

```shell
arcana compress <source>... -o <output> [options]
```

| Option | Alias | Default | Notes |
|---|---|---|---|
| `-o, --output` | | (required) | Output path |
| `-f, --format` | | `zip` | ⚠️ Accepted but **ignored** — output is always ZIP |
| `-l, --level` | | `5` | Clamped 0–9 |
| `-p, --password` | | | Encryption password |

```shell
arcana compress release/ -o release.zip -l 9
```

### `arcana extract`

Extract any supported archive.

```shell
arcana extract <archive> [output-directory] [options]
```

| Argument | Description |
|---|---|
| `archive` | Path to archive (any supported format) |
| `output-directory` | Extraction target (default `.`) |

| Option | Alias | Default | Notes |
|---|---|---|---|
| `-p, --password` | | | ⚠️ Accepted but **not wired** to `OpenAsync` yet |
| `--overwrite` | | `false` | ⚠️ Accepted but **not used** |

```shell
arcana extract backup.7z
arcana extract archive.zip ./out
```

### `arcana list`

List archive contents as a table.

```shell
arcana list <archive> [options]
```

| Option | Alias | Description |
|---|---|---|
| `-l, --detailed` | | Detailed listing (size, ratio, date, method) |

```shell
arcana list archive.zip -l
```

### `arcana convert`

Convert between archive formats (ZIP / 7z / Zstandard).

```shell
arcana convert <source> -o <output> [options]
```

| Option | Alias | Default | Description |
|---|---|---|---|
| `-o, --output` | | (required) | Output path |
| `-f, --format` | | auto | Output format (zip / 7z / zstd) |
| `-l, --level` | | `5` | Clamped 0–10 |

```shell
arcana convert backup.7z -f zip -o backup.zip
```

### `arcana hash`

Compute file checksums.

```shell
arcana hash <file>... [options]
```

| Option | Alias | Default | Description |
|---|---|---|---|
| `-a, --algorithm` | | `SHA256` | MD5, SHA1, SHA256, SHA512 (case-insensitive) |
| `--verify <file>` | | | File containing expected checksums to verify against |

```shell
arcana hash setup.exe -a sha256
arcana hash setup.exe -a sha256 --verify setup.exe.sha256
```

### `arcana split`

Split a file into parts.

```shell
arcana split <file> [options]
```

| Option | Alias | Default | Description |
|---|---|---|---|
| `-s, --part-size` | | `100M` | Size with `K`/`M`/`G` suffix (e.g. `10M`, `100M`, `1G`) |
| `-o, --output` | | `.` | Output directory |
| `--hjsplit` | | `false` | HJSplit-compatible naming (`.001`, `.002`, …) |

```shell
arcana split movie.iso -s 100M --hjsplit -o parts/
```

### `arcana join`

Join split parts back.

```shell
arcana join <parts>... [options]
```

| Option | Alias | Default | Description |
|---|---|---|---|
| `-o, --output` | | `output` | Output file path |

Parts are auto-discovered when one part is given (`AutoDiscoverParts`).

```shell
arcana join "parts/movie.iso.001" -o movie.iso
```

### `arcana benchmark`

Run compression benchmarks (ZIP, 7z, Zstd).

```shell
arcana benchmark [options]
```

| Option | Alias | Default | Description |
|---|---|---|---|
| `-d, --data` | | `tiny` | Payload size: `tiny`, `1k`, `small`, `1m`, `medium`, `10m` |

Data is generated with a fixed random seed (`Random(42)`), so runs are reproducible.

```shell
arcana benchmark -d 10m
```

## Unimplemented Options (roadmap)

Options listed in earlier documentation that do **not** exist in the current CLI: `--verbose`, `--quiet`, `--solid`, `--solid-block`, `--volume-size`, `--exclude`, `--store-json`, `--password-file`, `--filter`, `--flat`, `--strip-components`, `--test`, `--list`, `--json`, `--tree`, `--delete-after`, `--source-password`, `--delete-source`, `--format bsd/sun`, stdin/stdout piping.

## Notes

- **`--format` on `compress`** is accepted but ignored (always ZIP). Use `convert` for other output formats.
- **`extract --password` / `--overwrite`** are accepted but not wired; use the GUI for password-protected extraction today.
- **Logging** level is fixed at `Warning` in the CLI regardless of `--log-level` (see `LogConfig`).
