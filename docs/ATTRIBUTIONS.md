# Attributions

Arcana uses third-party libraries and resources. All licenses are compatible with GPLv3.

## Libraries

| Library | Version | License | Source |
|---|---|---|---|
| [SharpCompress](https://github.com/adamhathcock/sharpcompress) | 0.50.1 | MIT | GitHub |
| [ZstdNet](https://github.com/skbkontur/ZstdNet) | 1.5.7 | BSD 2-Clause | GitHub |
| [Hawkynt.FileFormats.Archives](https://www.nuget.org/packages/Hawkynt.FileFormats.Archives) | 1.0.0.696 | MIT | NuGet |
| [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4) | 1.3.8 | MIT | GitHub |
| [Snappy.Sharp](https://github.com/jeffijoe/snappy-sharp) | 1.0.0 | MIT | GitHub |
| [Konscious.Security.Cryptography.Argon2](https://github.com/kmaragon/Konscious.Security.Cryptography) | 1.3.1 | MIT | GitHub |
| [Avalonia UI](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT | GitHub |
| [Avalonia.Controls.DataGrid](https://github.com/AvaloniaUI/Avalonia) | 12.1.0 | MIT | GitHub |
| [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT | GitHub |
| [Avalonia.Fonts.Inter](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT | GitHub |
| [AvaloniaUI.DiagnosticsSupport](https://github.com/AvaloniaUI/Avalonia.Diagnostics) | 2.2.3 | MIT | GitHub |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.4.2 | MIT | GitHub |
| [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime) | 10.0.10 | MIT | GitHub |
| [System.CommandLine](https://github.com/dotnet/command-line-api) | 2.0.10 | MIT | GitHub |
| [Spectre.Console](https://github.com/spectreconsole/spectre.console) | 0.57.2 | MIT | GitHub |
| [Serilog](https://github.com/serilog/serilog) | 4.4.0 | Apache 2.0 | GitHub |
| [Serilog.Sinks.Console](https://github.com/serilog/serilog-sinks-console) | 6.1.1 | Apache 2.0 | GitHub |
| [Serilog.Sinks.File](https://github.com/serilog/serilog-sinks-file) | 6.0.0 | Apache 2.0 | GitHub |
| [xUnit](https://github.com/xunit/xunit) | latest | Apache 2.0 | GitHub |
| [FluentAssertions](https://github.com/fluentassertions/fluentassertions) | latest | Apache 2.0 | GitHub |

## Icons

| Theme | License | Notes |
|---|---|---|
| [Papirus](https://github.com/PapirusDevelopmentTeam/papirus-icon-theme) | GPL-3.0 | Default icon provider; PNG assets bundled in `src/Arcana.App/Assets/Papirus/` |
| Material Design icons | Apache 2.0 | Built-in vector paths in `DefaultIconProvider` |
| WinRAR themes | user-provided | `.theme.rar` files are loaded from `%APPDATA%\Arcana\Themes` at runtime; not bundled (copyright of each theme author) |

Papirus/GPL-3.0 matches Arcana's own GPLv3 license.

## Fonts

| Font | License | Notes |
|---|---|---|
| Inter | SIL Open Font License 1.1 | Bundled via `Avalonia.Fonts.Inter` |

---

*This document is maintained alongside `src/*/*.csproj` and `src/Directory.Build.props`. Run `dotnet build src/Arcana.slnx` to verify the dependency set.*
