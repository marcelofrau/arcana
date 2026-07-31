# Themes

Arcana has **two independent theme layers**, both selectable from dropdowns in the
main window:

| Layer | Menu | Persisted setting | Default |
| --- | --- | --- | --- |
| Icon theme | Tools → **Select an Icon Theme…** | `IconTheme` | `Papirus` |
| Color theme | Tools → **Select a Color Theme…** | `ColorTheme` | `brewerviridis` |

Each layer can be changed without affecting the other (e.g. WinRAR-style icons on
top of a BeOS color scheme).

---

## Color themes

A color theme is a `ColorTheme` (`src/Arcana.App/Themes/ColorTheme.cs`): a stable
`Id`, a `DisplayName`, a light/dark `Variant`, and 15 semantic color tokens:

| Token | Role |
| --- | --- |
| `Background` | Window background |
| `Surface` | Panels, preview, file list |
| `SurfaceRaised` | Toolbar, status bar, menus |
| `Border` | Dividers, splitters, borders |
| `TextPrimary` | Primary text |
| `TextSecondary` | Muted text, column headers |
| `Accent` | Highlights, progress, focus |
| `AccentHover` | Accent on hover |
| `Success` / `Warning` / `Error` | Status semantics |
| `Hover` | Hover highlight |
| `Selection` / `SelectionUnfocused` | Row/item selection |

Every token is pushed into `Application.Resources` as both a `Color` and a
`…Brush` `SolidColorBrush`, plus `SystemAccentColor`, the DataGrid override
brushes, and the `RequestedThemeVariant`. Because `Themes/Controls.axaml` and the
views reference the tokens with `DynamicResource`, switching themes recolors the
whole UI live with no restart. That is all orchestrated by
`ColorThemeService` (`src/Arcana.App/Services/ColorThemeService.cs`).

### Where themes come from

`ColorThemeCatalog` (`src/Arcana.App/Themes/ColorThemeCatalog.cs`) is the single
registry. It builds themes from three sources:

1. **Arcana Mystic** — the original hand-tuned palette, hardcoded in the catalog.
2. **Palette themes** — one per file in `docs/palletes/*.hex`, auto-derived by
   `PaletteThemeFactory`. **Adding a theme is just dropping a `.hex` file into
   `docs/palletes/` and rebuilding.**
3. **Retro themes** — hand-crafted light themes: **Windows 2000**, **Windows XP**,
   **BeOS**.

### Palette file format (`docs/palletes/*.hex`)

- Plain text, one hex color per line.
- Colors may be `RRGGBB` (6 digits) or `AARRGGBB` (8 digits).
- Blank lines and lines starting with `#` are ignored.
- The filename becomes the theme `Id`; a friendly name can be registered in the
  `PaletteEntries` table in `ColorThemeCatalog`.

Example — `docs/palletes/brewerviridis.hex` (the default theme):

```
000000
402859
353763
275f77
388771
5a9e5c
ffea63
d89544
8d844a
bcb26f
ffe5bf
ffffff
```

### How a palette becomes a theme

The `.hex` files are embedded into `Arcana.App` as resources
(`Arcana.App.csproj`). `PaletteThemeFactory` derives the 15 tokens
deterministically, so any palette produces a usable theme without hand-tuning:

- Background / Surface / SurfaceRaised ← the **darkest** palette colors.
- TextPrimary / TextSecondary ← the **lightest** palette colors.
- Border / Hover ← subtle lightening of the raised surface.
- Accent ← the **most saturated** color whose luma is in `[0.18, 0.85]`.
- AccentHover ← accent blended toward the primary text.
- Success / Warning / Error ← most saturated palette color in the green /
  amber / red hue bands; falls back to the Arcana defaults blended with the
  accent when the palette has no match.
- Selection / SelectionUnfocused ← accent blended into the surface.

Palette themes are always dark (`Variant = Dark`). Retro themes are light.

### Changing the default

`ColorThemeCatalog.DefaultId` selects the initial theme; it is currently
`brewerviridis`. The user's choice is persisted in `%APPDATA%\Arcana\settings.json`
under `ColorTheme`.

---

## Icon themes

Icon themes follow the WinRAR model and are managed by `IconThemeService`
(`src/Arcana.App/Icons/`):

- **Papirus** (default) — bundled PNG set, GPL-3.0 (matches the app license).
- **Material** — bundled vector fallback (`DefaultIconProvider`), used for any
  slot a WinRAR theme does not supply.
- **WinRAR themes** — user-installed `.theme.rar` files, extracted to
  `%APPDATA%\Arcana\Themes`. Toolbar bitmaps use the magenta chroma key (`#FF00FF`)
  which is converted to transparency. A matching window icon is applied when the
  theme ships a `RAR.ico`/`File.ico`.

Install a WinRAR theme via Tools → **Install Theme…**; open the themes folder via
the same menu. Selections persist in `settings.json` under `IconTheme`.

---

## Adding a theme (cheat sheet)

- **Palette color theme**: add `docs/palletes/<name>.hex`, optionally register a
  display name in `ColorThemeCatalog.PaletteEntries`, rebuild.
- **Hand-crafted color theme**: add a `ColorTheme` to `ColorThemeCatalog`.
- **Icon theme**: install a WinRAR `.theme.rar` at runtime, or add a new
  `IIconProvider` and register it in `IconThemeService`.
