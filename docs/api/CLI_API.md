# Arcana CLI Reference

## Global Usage

```shell
arcana [command] [options] [arguments]
```

### Global Options

| Option | Short | Description |
|---|---|---|
| `--version` | | Show version information |
| `--help` | `-h` | Show help |
| `--verbose` | `-v` | Verbose output |
| `--quiet` | `-q` | Suppress output (exit code only) |

## Commands

### `arcana compress`

Compress files and directories into an archive.

```shell
arcana compress <source>... -o <output> [options]
```

| Argument | Description |
|---|---|
| `source` | Files/directories to compress (one or more, glob supported) |

| Option | Short | Default | Description |
|---|---|---|---|
| `--output` | `-o` | (required) | Output archive path |
| `--format` | `-f` | `zip` | Archive format (`zip`, `7z`, `zstd`, `brotli`, `lz4`, `tar`, `tar.gz`, `tar.bz2`, `tar.xz`, `tar.zst`) |
| `--level` | `-l` | `5` | Compression level (0=store, 1=fastest, 5=normal, 9=ultra) |
| `--password` | `-p` | | Encryption password |
| `--encryption` | `-e` | `aes-256-gcm` | Cipher algorithm (`aes-256-gcm`, `chacha20-poly1305`) |
| `--parallel` | | `true` | Enable parallel compression |
| `--threads` | `-t` | *CPU count* | Number of worker threads |
| `--solid` | | `false` | Enable solid archive (7z only) |
| `--solid-block` | | `64` | Solid block size in MB (7z only) |
| `--volume-size` | | | Split archive into volumes (e.g., `100M`, `1G`) |
| `--include-hidden` | | `true` | Include hidden files |
| `--exclude` | | | Glob pattern to exclude |
| `--store-json` | | | Store JSON metadata in archive |

**Examples:**

```shell
arcana compress *.txt -o backup.zip
arcana compress ./docs -o docs.7z -f 7z -l 9 --password s3cret
arcana compress . -o archive.tar.zst -f tar.zst -l 3 --parallel
arcana compress ./photos -o photos.7z --volume-size 100M
arcana compress ./src -o src.zip --exclude "**/bin/**" --exclude "**/obj/**"
```

### `arcana extract`

Extract files from an archive.

```shell
arcana extract <archive> [output-directory] [options]
```

| Argument | Description |
|---|---|
| `archive` | Path to archive |
| `output-directory` | Extraction target (default: current directory) |

| Option | Short | Default | Description |
|---|---|---|---|
| `--password` | `-p` | | Decryption password |
| `--password-file` | | | Path to password file |
| `--overwrite` | `-o` | `prompt` | Overwrite mode (`yes`, `no`, `prompt`, `rename`) |
| `--filter` | `-f` | | Extract only matching files (glob) |
| `--flat` | | `false` | Extract without directory structure |
| `--strip-components` | | `0` | Remove leading path components |
| `--test` | | `false` | Test integrity without extracting |
| `--list` | | `false` | List contents and exit |

**Examples:**

```shell
arcana extract backup.zip
arcana extract archive.7z ./output -p s3cret
arcana extract archive.zip -f "**/*.txt" --flat
arcana extract archive.7z --test
arcana extract archive.zip --list
```

### `arcana list`

List archive contents.

```shell
arcana list <archive> [options]
```

| Option | Short | Default | Description |
|---|---|---|---|
| `--detailed` | `-l` | `false` | Detailed listing (size, ratio, date, method) |
| `--sort` | `-s` | `name` | Sort field (`name`, `size`, `date`, `ratio`) |
| `--reverse` | `-r` | `false` | Reverse sort order |
| `--filter` | `-f` | | Filter entries (glob) |
| `--json` | `-j` | `false` | JSON output |
| `--tree` | `-t` | `false` | Tree view output |

**Examples:**

```shell
arcana list archive.zip
arcana list archive.7z -l -s size --reverse
arcana list archive.zip -f "**/docs/**" --tree
arcana list archive.zip --json
```

### `arcana split`

Split a file into parts.

```shell
arcana split <file> [options]
```

| Option | Short | Default | Description |
|---|---|---|---|
| `--part-size` | `-s` | `100M` | Part size (`10M`, `100M`, `1G`, `650M` for CD, `4480M` for DVD) |
| `--output` | `-o` | *same dir as file* | Output directory |
| `--delete-after` | | `false` | Delete original after split |

### `arcana join`

Join split file parts.

```shell
arcana join <parts>... [options]
```

| Option | Short | Default | Description |
|---|---|---|---|
| `--output` | `-o` | *current directory* | Output file path |
| `--delete-after` | | `false` | Delete parts after join |

### `arcana hash`

Compute file checksums.

```shell
arcana hash <file>... [options]
```

| Option | Short | Default | Description |
|---|---|---|---|
| `--algorithm` | `-a` | `sha256` | Hash algorithm (`md5`, `sha1`, `sha256`, `sha512`, `blake2b`, `blake2s`) |
| `--output` | `-o` | | Write checksums to file |
| `--verify` | | | Verify checksums from file |
| `--format` | `-f` | `text` | Output format (`text`, `json`, `bsd`, `sun`) |

### `arcana convert`

Convert between archive formats.

```shell
arcana convert <source> -o <output> [options]
```

| Option | Short | Default | Description |
|---|---|---|---|
| `--output` | `-o` | (required) | Output archive path |
| `--format` | `-f` | *auto (from extension)* | Output format |
| `--level` | `-l` | `5` | Compression level |
| `--password` | `-p` | | Encryption password (output) |
| `--source-password` | | | Decryption password (source) |
| `--delete-source` | | `false` | Delete source after conversion |

### `arcana benchmark`

Run compression benchmarks.

```shell
arcana benchmark [options]
```

| Option | Short | Default | Description |
|---|---|---|---|
| `--data` | `-d` | `silesia` | Test data set (`silesia`, `enwik8`, `random`) |
| `--formats` | `-f` | `all` | Formats to benchmark |
| `--threads` | `-t` | *all* | Thread count(s) to test |
| `--output` | `-o` | | Save results to JSON |

## Exit Codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | File not found |
| 4 | Access denied |
| 5 | Wrong password |
| 6 | Corrupted archive |
| 7 | Unsupported format |
| 8 | Cancelled by user |
