# Arcana Documentation

Technical documentation for Arcana — a cross-platform archiver with a desktop GUI, a full CLI, 17 built-in engines and fallback read support for 240+ formats.

## Architecture & Design

| Document | Description |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | System architecture, C4 diagrams, layers, data flow |
| [GUI_PLAN.md](GUI_PLAN.md) | GUI design: controls, view models, services, icon themes |
| [DECISIONS.md](DECISIONS.md) | Architecture Decision Records (ADRs) |
| [SPECS.md](SPECS.md) | Functional and non-functional requirements |

## Project Management

| Document | Description |
|---|---|
| [ROADMAP.md](ROADMAP.md) | Milestones, timeline, deliverables (mermaid gantt) |
| [PLAN.md](PLAN.md) | Execution plan, completed/in-progress status |
| [BRANCH_STRATEGY.md](BRANCH_STRATEGY.md) | Git workflow, branching model |
| [SEMVER.md](SEMVER.md) | Versioning policy |

## Compression & Security

| Document | Description |
|---|---|
| [compression/FORMATS.md](compression/FORMATS.md) | Supported formats, capabilities, limitations, engine matrix |
| [compression/CIPHERS.md](compression/CIPHERS.md) | Encryption algorithms, KDF, security model |
| [compression/BENCHMARKS.md](compression/BENCHMARKS.md) | Benchmark methodology and the `benchmark` command |

## API Reference

| Document | Description |
|---|---|
| [api/CORE_API.md](api/CORE_API.md) | Public interfaces of Arcana.Core |
| [api/CLI_API.md](api/CLI_API.md) | CLI commands, arguments, examples |
| [api/PLUGIN_API.md](api/PLUGIN_API.md) | Plugin model (future) |

## Contributing

| Document | Description |
|---|---|
| [contributing/CODING_STANDARDS.md](contributing/CODING_STANDARDS.md) | Code style, analyzers, commit conventions |
| [contributing/TESTING.md](contributing/TESTING.md) | Testing strategy, frameworks, coverage targets |
| [contributing/REVIEW_GUIDE.md](contributing/REVIEW_GUIDE.md) | Code review checklist and process |

## Meta

| Document | Description |
|---|---|
| [FUTURE.md](FUTURE.md) | Long-term ideas and wishlist |
| [ATTRIBUTIONS.md](ATTRIBUTIONS.md) | Third-party library attributions and licenses |
