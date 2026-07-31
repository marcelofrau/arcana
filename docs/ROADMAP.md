# Roadmap

Status as of 2026-07-31. The core (engines, CLI, GUI) shipped quickly; remaining work is editing, polish, distribution and long-term features.

## Timeline

```mermaid
gantt
    title Arcana Roadmap
    dateFormat  YYYY-MM-DD
    axisFormat  %Y-%m

    section v0.1.0 — Foundation
    Project scaffold                  :done, 2026-07-29, 1d
    17 engines + Serilog + crypto     :done, 2026-07-29, 2d
    CLI: 8 commands                   :done, 2026-07-29, 2d
    GUI shell + icon themes           :done, 2026-07-31, 1d
    Explorer GUI + preview            :done, 2026-07-31, 1d
    Documentation complete            :done, 2026-07-31, 1d
    v0.1.0 release                    :milestone, 2026-08-03, 0d

    section v0.2.0 — Forge (archive editing)
    Rename / delete / add files (VFS -> GUI) :2026-08-04, 5d
    Save modified archive             :2026-08-06, 4d
    Drag & drop                       :2026-08-08, 5d
    Archive repair (RAR recovery)     :2026-08-10, 7d
    v0.2.0 release                    :milestone, 2026-08-18, 0d

    section v0.3.0 — Security & tools
    ChaCha20-Poly1305                 :2026-08-19, 5d
    Image converter (ImageConverter)   :2026-08-20, 5d
    Batch processor (BatchProcessor)   :2026-08-25, 5d
    Key-file encryption               :2026-08-27, 5d
    v0.3.0 release                    :milestone, 2026-09-02, 0d

    section v0.4.0 — CI & distribution
    GitHub Actions CI matrix          :2026-09-03, 5d
    Packaging (winget, brew, apt)      :2026-09-08, 10d
    v0.4.0 release                    :milestone, 2026-09-22, 0d

    section v1.0.0 — Polish
    Settings window + light theme     :2026-09-23, 7d
    i18n (EN + PT-BR)                 :2026-09-28, 10d
    Performance tuning + benchmarks   :2026-10-05, 7d
    v1.0.0 release                    :milestone, 2026-10-15, 0d

    section Future (v2.0+)
    Mobile (iOS/Android, Avalonia)    :2027-01-05, 60d
    Plugin API                        :2027-02-01, 30d
    WebAssembly (Avalonia.Web)        :2027-03-01, 45d
    FUSE filesystem mount             :2027-04-01, 30d
```

## Engine Status

| Engine | Backend | R/W | Encrypt | Tests | Status |
|---|---|---|---|---|---|
| Zip | SharpCompress ZipArchive + Arcana AES-GCM | r/w | ✅ | ✅ | stable |
| SevenZip | SharpCompress SevenZipArchive + Arcana AES-GCM | r/w | ✅ | ✅ | stable |
| Zstd | ZstdNet | r/w | — | ✅ | stable |
| Tar (+Gz/Bz2/Xz/Zst) | SharpCompress + routing (Xz write N/S) | r/w | — | ✅ | stable |
| RAR | SharpCompress (RAR4/RAR5) | r/o | — | ✅ | stable |
| ACE | Hawkynt AceFormatDescriptor | r/o | ⚠️ | ✅ | stable |
| ARJ | SharpCompress ArjReader | r/o | ⚠️ | ✅ | stable |
| CAB | Hawkynt CabFormatDescriptor | r/o | — | ✅ | stable |
| LZH/LHA | Hawkynt LzhFormatDescriptor | r/o | — | ✅ | stable |
| Brotli | System.IO.Compression | r/w | — | ✅ | stable |
| GZip | SharpCompress GZipStream | r/w | — | ✅ | stable |
| BZip2 | SharpCompress BZip2Stream | r/w | — | ✅ | stable |
| Xz | SharpCompress LZipStream | r/w | — | ✅ | stable |
| LZMA | SharpCompress LZipStream | r/w | — | ✅ | stable |
| LZ4 | K4os.Compression.LZ4 | r/w | — | ✅ | stable |
| Snappy | Snappy.Sharp | r/w | — | ✅ | stable |
| HawkyntFallback | FormatRegistry auto-detect (240+) | r/o | ⚠️ | ✅ | best-effort |
| **Total** | **17 engines** | | | **145 tests** (134 Core + 11 App) | |

## Milestone Summary

| Milestone | Version | Key Deliverables | Status |
|---|---|---|---|
| Foundation | v0.1.0 | Scaffold, 17 engines, CLI, GUI shell, explorer GUI, docs | ✅ 100% |
| Forge | v0.2.0 | Archive editing, drag & drop, repair | 🔶 0% |
| Security & Tools | v0.3.0 | ChaCha20-Poly1305, image converter, batch processor | 🔶 0% |
| CI & Distribution | v0.4.0 | GitHub Actions, winget/brew/apt | ❌ 0% |
| Polish | v1.0.0 | Settings, themes, i18n, perf | ❌ 0% |
| Mobile | v2.0.0 | iOS/Android, plugins, WASM, FUSE | ❌ 0% |

## Dependency Graph

```mermaid
flowchart LR
    A[v0.1.0 Foundation] --> B[v0.2.0 Forge]
    B --> C[v0.3.0 Security & Tools]
    C --> D[v0.4.0 CI & Distribution]
    D --> E[v1.0.0 Polish]
    E --> F[v2.0.0 Mobile & Plugins]
```

## Next Steps

1. Wire archive editing (rename/delete/add) from the VFS API into the GUI
2. Save modified archives (in-place and save-copy-as)
3. ChaCha20-Poly1305 implementation
4. GitHub Actions CI
5. Image conversion (unblock `ImageConverter` stub)
