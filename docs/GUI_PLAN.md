# Arcana GUI — Implementation Guide

Master reference for the graphical interface: controls, view models, services, icon themes, and current implementation status.

## 1. Concept

**"Classic soul, modern skin."**

Merge the nostalgia of classic archivers (WinRAR/WinZip) with modern design. Not a retro skin — a familiar structure (two-pane, columns, status bar, wizards, shortcuts) wrapped in a modern dark visual with a violet accent.

| Element | Origin | Treatment |
|---|---|---|
| Two-pane (tree + table) | WinRAR | Kept; collapsible tree |
| Columns Name/Size/Packed/Ratio/Type/Modified | WinRAR | Kept; sortable, with icons |
| Status bar "N files · X MB" | WinRAR | Kept + progress |
| Toolbar of actions | WinRAR | Flat, icons via providers |
| Shortcuts (Enter, Back, Del, Ctrl+A, F2) | WinRAR | Kept |
| Dark mode, accent, animations | Modern | Native |

## 2. Visual Identity

| Token | Value | Use |
|---|---|---|
| Theme | FluentTheme, `RequestedThemeVariant="Dark"` | window / controls |
| Accent | `#8B5CF6` (SystemAccentColor) | selection, focus, links |
| DataGrid | Dark brush overrides in `App.axaml` | keeps the grid readable in dark mode |

Typography and density come from the Fluent theme; rows use compact sizing. A light palette is a future item (see [ROADMAP](ROADMAP.md)).

## 3. Layout — MainWindow

```
┌───────────────────────────────────────────────────────────────┐
│ Menu: File | Commands | Tools | Favorites | Options | Help    │
│ Toolbar (ItemsControl of ToolBarButton)                        │
│ Breadcrumb: Arcana › archive.zip › folder/   [filter box]      │
├──────────────┬──────────────────────────────┬─────────────────┤
│ FolderTree   │ DataGrid (WinRAR columns)    │ PreviewPanel    │
│ (240px)      │ Name|Size|Packed|Ratio|Type| │ (320px, toggle) │
│ folders only │ Modified — sortable          │ text/image/hex  │
├──────────────┴──────────────────────────────┴─────────────────┤
│ Status bar: "N files · X MB"  [progress]                      │
└───────────────────────────────────────────────────────────────┘
```

Grid columns: `240, 4, *, 4, 320` — folder tree, splitter, center pane, splitter, preview. The tree and preview panes can be hidden (FileListVisible / CommentsVisible toggles).

## 4. Controls (`src/Arcana.App/Controls/`)

| Control | Type | Spec |
|---|---|---|
| `FolderTree` | UserControl + TreeView | `ItemsSource = Archive.TreeNodes`, `SelectedItem = Archive.CurrentNode` (TwoWay). Template binds `ChildFolders` — **folders only**. Root auto-expands; `SelectCurrentNode` scrolls selection into view |
| `FileTable` | UserControl + DataGrid | Columns: Name (icon + name), Size, Packed, Ratio (%), Type, Modified. Sorting via `SortMemberPath`. Extended selection, double-tap opens, `OnSelectAllRequested` for Ctrl+A. No context menu yet |
| `PreviewPanel` | UserControl | Header (file name + info), loading bar. Content switch: text (`Kind=Text`), image (`Kind=Image`), hex dump, or binary placeholder (icon + name + "Binary Preview" button that loads hex on demand) |
| Toolbar / StatusBar | Inline in MainWindow | Toolbar: `ItemsControl` over `ToolBarButton` items (icon, label, tooltip, command). Status bar: text + `ProgressBar` bound to `IsBusy`/`BusyText` |

### Keyboard shortcuts

| Key | Action |
|---|---|
| Enter / Double-click | Open selected entry (navigate or preview) |
| Backspace | Navigate up |
| Del | Delete selected (VFS dirty; GUI wiring pending) |
| Ctrl+A | Select all rows |
| Alt+Enter | Info dialog |

## 5. Icon Engine

### Icon keys (`IconKey`, 33 values)

`Open, Add, Extract, Test, View, Delete, Find, Info, FileGeneric, FileArchive, FileImage, FileCode, FileMedia, FileDoc, Folder, Rar, SortUp, SortDown, Save, Close, Settings, Help, Split, Convert, Password, Rename, SelectAll, Compare, Favorite, Join, Hash, Optimize`

### Providers

```
IIconProvider: Name, ToolbarSize, GetIcon(IconKey) : IImage?
├─ DefaultIconProvider   — Material Design path data (vector DrawingImages), default fallback
├─ PapirusIconProvider   — PNG assets (GPL-3.0, Papirus icon theme), ToolbarSize 48, DEFAULT
└─ WinRarThemeProvider   — loads .theme.rar via ArchiveFactory, maps toolbar bitmaps
```

| Provider | Source | Size |
|---|---|---|
| Papirus (default) | bundled `Assets/Papirus/*.png` (GPL-3.0) | 48px |
| Material | built-in vector paths | 24px |
| WinRAR theme | user `.theme.rar` in `%APPDATA%\Arcana\Themes` | native to the theme |

`IconThemeService` manages built-ins + installed themes; menu **Options → Icon Theme** lists `ThemeMenuItems`. `ThemeBitmapLoader` decodes BMP strips with magenta (`#FF00FF`) chroma-key → transparency. Window icon follows the active theme (`RAR.ico`/`File.ico`).

`IconResolver` maps `ArchiveNode` → key (`root`→FileArchive, dir→Folder, else by extension: archive/image/media/code/doc maps).

## 6. View Models + Services

### View models

| VM | Responsibility |
|---|---|
| `MainViewModel` | Toolbar commands, open/new/extract/test/delete/find/info, favorites, theme menu, `RunBusyAsync`, status text. Commands are `[RelayCommand]` |
| `ArchiveViewModel` | `LoadArchive`, `TreeNodes`/`Entries`, nav history (back/up), `NavigateTo`, breadcrumb, filter (`ApplyFilter`), selection, `BuildEntries` (folders first, ordinal) |
| `FileEntryItem` | Wraps an entry for the table: `Name`, `IsDirectory`, `Ext`, `TypeText`, `SizeText`, `PackedText`, `RatioText`, `ModifiedText`, sort values, `Icon` |
| `PreviewViewModel` | `Show(node)`, `Clear`, `Kind` (Text/Image/Hex), `IsBinaryPlaceholder`, `LoadBinaryCommand`, `PlaceholderIcon` |
| `ToolsViewModel` | Split / Join / Hash commands — **stub** (`Task.Delay(100)`, TODO) |
| Dialog VMs | `InfoViewModel`, `SettingsViewModel`, `PasswordViewModel`, `PromptViewModel`, `ConvertViewModel`, `SplitFileViewModel`, `JoinFileViewModel`, `HashFileViewModel` — plain `ObservableObject` with `Confirmed` flag |

### Services

| Service | API |
|---|---|
| `ArchiveService` | `OpenAsync`, `SaveAsync`, `ExtractAsync(dest, progress, ct)`, `TestAsync` (CRC32), `Close` |
| `DialogService` | StorageProvider pickers (`PickArchiveAsync`, `PickThemeAsync`, `PickDirectoryAsync`, `PickFilesAsync`, `PickSaveArchiveAsync`, `PickSaveCopyAsync`) + dialogs (`ShowInfoAsync`, `ShowPromptAsync`, `ShowPasswordAsync`, `ShowSettingsAsync`, `ShowConvertAsync`, `ShowSplitFileAsync`, `ShowJoinFileAsync`, `ShowHashFileAsync`) |
| `PreviewService` | `DetectKind` (text/image/hex), `LoadPreview`, `LoadText` (256 KiB cap), `LoadHex` (64 KiB cap), `LoadImage` (Bitmap, fallback hex) |
| `SettingsService` | `%APPDATA%\Arcana\settings.json` — format, level, threads, parallel, log level |
| `FavoritesService` | `%APPDATA%\Arcana\favorites.json` — pinned archives |

### DI (`App.axaml.cs`)

Singletons: `ArchiveService`, `PreviewService`, `DialogService`, `SettingsService`, `FavoritesService`, `DefaultIconProvider`, `IconThemeService`. Transients: `MainViewModel`, `ArchiveViewModel`, `PreviewViewModel`, `ToolsViewModel`, `SettingsViewModel`. Dialog VMs are constructed inside `DialogService`.

## 7. Dialogs

| Dialog | Purpose |
|---|---|
| Password | Capture + confirm password (`CanConfirm`: non-empty and match) |
| Convert | Pick target format (zip / 7z / zstd) + level |
| Split File | Part size presets (100 MB–4 GB) + HJSplit naming toggle |
| Join File | First part, output path, part count |
| Hash File | Algorithm (MD5 / SHA-1 / SHA-256 / SHA-512) |
| Info | Title + message |
| Prompt | Text input |
| Settings | Defaults: format, level, log level |

## 8. Implementation Status

| Area | Status |
|---|---|
| Project scaffold, DI, MVVM toolkit | ✅ |
| MainWindow: menu, toolbar, status bar | ✅ |
| FolderTree (folders only, sync, expand) | ✅ |
| FileTable (DataGrid, sorting, navigation) | ✅ |
| PreviewPanel (text / image / hex / placeholder) | ✅ |
| Dialogs (password, convert, split, join, hash, info, prompt, settings) | ✅ |
| Icon themes (Papirus / Material / WinRAR) | ✅ |
| Favorites | ✅ |
| ArchiveService (open / extract / test / save) | ✅ |
| Toolbar context menu (right-click) | ❌ |
| Archive editing (rename / delete / add → GUI) | ❌ |
| Drag & drop | ❌ |
| Command palette (Ctrl+K) | ❌ |
| ToolsViewModel wiring (split/join/hash in GUI) | ❌ stub |
| Settings window wiring | ⚠️ dialog exists, Apply minimal |
| Light theme | ❌ |
| i18n (EN + PT-BR) | ❌ |

## 9. Verification Checklist

- [x] `dotnet build src/Arcana.slnx` — 0 errors
- [x] `dotnet test src/Arcana.slnx` — 145 tests green
- [x] Smoke: open real `.zip` and `.rar`
- [x] Navigate tree → breadcrumb syncs
- [x] Column sorting works
- [x] Preview text / image / hex placeholder
- [x] Extract to folder with progress
- [x] Theme switch without crash; fallback for missing slots
- [ ] Delete removes from VFS (dirty) → GUI
- [ ] App without binding warnings (Output/Debug logs)
