# Arcana GUI — Plano de Implementação

Referência mestra da interface gráfica. Fases, decisões, specs de controles e arquitetura.

## 1. Conceito

**"Alma clássica, pele moderna"**

Fundir a nostalgia dos arquivadores clássicos (WinRAR/WinZip) com design moderno. Não é um skin retrô — é estrutura familiar (two-pane, colunas, status bar, wizards, atalhos) embalada em visual dark moderno com accent violeta.

| Elemento | Origem | Tratamento |
|---|---|---|
| Two-pane (árvore + tabela) | WinRAR | Mantido, árvore colapsável |
| Colunas Name/Size/Packed/Ratio/Type/Modified | WinRAR | Mantidas, sortáveis, com ícones |
| Status bar "N files · X MB" | WinRAR | Mantida + progresso |
| Toolbar de ações | WinRAR | Flat, ícones via provider (temas) |
| Wizards passo-a-passo | WinZip | StepperDialog moderno |
| Atalhos (F2, Del, Enter, Ctrl+A) | WinRAR | Mantidos |
| Dark mode, accent, animações | Moderno | Nativos |
| Command palette, preview inline | Moderno | Nativos |

## 2. Identidade visual

### Paleta dark (primária)

| Token | Valor | Uso |
|---|---|---|
| `AppBackground` | `#16161E` | fundo da janela |
| `AppSurface` | `#1E1E28` | árvore, tabela, painéis |
| `AppSurfaceRaised` | `#262631` | toolbar, status bar, header |
| `AppBorder` | `#33334A` | bordas, separadores |
| `AppTextPrimary` | `#E4E4EE` | texto principal |
| `AppTextSecondary` | `#9A9AB0` | texto secundário |
| `AppAccent` | `#8B5CF6` | violeta profundo (seleção, foco, links) |
| `AppAccentHover` | `#9F7AFF` | hover do accent |
| `AppSuccess` | `#34D399` | ok / verificado |
| `AppWarning` | `#FBBF24` | aviso |
| `AppError` | `#F87171` | erro |

### Paleta light (fase futura)

`#F7F7FB` fundo, `#FFFFFF` surface, accent violeta igual.

### Tipografia
- Segoe UI Variable (Windows) / Inter (Avalonia.Fluid default) — fallback automático
- Densidade compacta: rows 28px (espírito WinRAR, respiro moderno)

### Ícones
- **Default**: set moderno flat 24px (provider embutido)
- **WinRAR themes**: tamanho nativo do tema, zero escalonamento (ver seção 5)

## 3. Layout — MainWindow (two-pane)

```
┌───────────────────────────────────────────────────────────────┐
│ Toolbar: Open │ Add │ Extract │ Test │ View │ Delete │ Search │ Info │
│ Breadcrumb: Arcana › archive.zip › folder/    [filter box]      │
├──────────────┬────────────────────────────────┬────────────────┤
│ Árvore       │ DataGrid (colunas WinRAR)      │ Preview panel  │
│ de pastas    │ Name|Size|Packed|Ratio|Type|   │ (toggle, 320px)│
│ (240px)      │ Modified — sortável            │ text/hex/img   │
│ splitter     │                                │ splitter       │
├──────────────┴────────────────────────────────┴────────────────┤
│ Status bar: "12 files · 3.4 MB · 2 sel (1.2 MB)"  [progress bar]│
└───────────────────────────────────────────────────────────────┘
```

- Title bar nativa do SO (sem custom chrome)
- Splitter entre árvore e tabela; splitter antes do preview
- Breadcrumb = caminho dentro do archive (root › dirs › file)
- Filter box filtra entries da tabela por nome

## 4. Controles (`src/Arcana.App/Controls/`)

| Control | Tipo | Spec |
|---|---|---|
| `ToolBar.axaml` | UserControl | Row de botões ícone+texto (opcional texto), bound a `IconKey`, altura = tamanho nativo do tema, commands do MainViewModel |
| `StatusBar.axaml` | UserControl | TextBlock contagem + ProgressBar (indeterminado durante ops) |
| `FileTable.axaml` | UserControl + DataGrid | Colunas: Name (ícone+texto), Size, Packed, Ratio (%), Type, Modified. Sorting por coluna. Seleção múltipla. ContextMenu (Extract Here, Extract To…, Open, View, Test, Rename, Delete) |
| `FolderTree.axaml` | UserControl + TreeView | `TreeDataTemplate` → `ArchiveNode`, ícone pasta/arquivo, seleção navega breadcrumb + filtra tabela |
| `PreviewPanel.axaml` | UserControl | Header "Preview" + botão fechar. Content switch por tipo: texto (syntax-ish), hex dump, imagem (codec nativo). Edição inline de texto (fase Forge) |
| `StepperDialog.axaml` | Window template | Shell de wizard: coluna esquerda com passos numerados (estilo WinZip), área de conteúdo, barra inferior Back/Next/Finish/Cancel. Título + logo (tema) |

## 5. Icon engine (temas WinRAR)

### Conceito
Temas WinRAR = arquivos `.theme.rar` (RAR archive) com `winrar_theme_description.txt` + gráficos. Já temos engine RAR (`RarEngine`/`ArchiveFactory` detecta magic `52 61 72 21 1A 07`). Suportar formato = abrir tema que o usuário instala. **Não embutir temas no repo** (copyright de cada autor).

### Slots (`IconKey`)
`Open, Add, Extract, ExtractTo, Test, View, Delete, Find, Info, File, Rar, WizardLogo, SortUp, SortDown`

### Mapeamento tema → UI

| Arquivo do tema | Slot | Formato |
|---|---|---|
| `Toolbar/Add.bmp` | Add | BMP/PNG — também define `ToolbarSize` (dimensões) |
| `Toolbar/Extract.bmp` | Extract | BMP/PNG |
| `Toolbar/ExtractTo.bmp` | ExtractTo | BMP/PNG |
| `Toolbar/Test.bmp` | Test | BMP/PNG |
| `Toolbar/View.bmp` | View | BMP/PNG |
| `Toolbar/Find.bmp` | Find | BMP/PNG |
| `Toolbar/Info.bmp` | Info | BMP/PNG |
| `Toolbar/Delete.bmp` | Delete | BMP/PNG |
| `Toolbar/Convert.bmp` | (futuro) | BMP/PNG |
| `Toolbar/Benchmark.bmp` | (futuro) | BMP/PNG |
| `File.ico` | File | ICO |
| `RAR.ico` | Rar (ícone janela) | ICO |
| `SortUp/Down.bmp` | SortUp/SortDown | BMP (degrade opcional) |
| `WizardLogo.bmp` | WizardLogo | BMP |
| `winrar_theme_description.txt` | title/about | texto ASCII |

### Regras
- **Zero escalonamento**: ícones renderizam no tamanho nativo do bitmap. Tema 64x64 → toolbar 64px.
- `Add.bmp` presente = tamanho da toolbar (docs RARLAB: auto-detect via Add.bmp)
- Slots ausentes → fallback pro provider default (moderno 24px)
- BMP 24-bit: chroma-key (preto default, `background=255` no desc → branco) para transparência
- `.cur` (cursors): não suportado (Avalonia não decodifica) — pulado

### Arquitetura
```
IIconProvider.GetImage(IconKey) : IImage?
├─ DefaultIconProvider   — set moderno embutido (geometrias/paths 24px)
└─ WinRarThemeProvider   — carrega .theme.rar via ArchiveFactory, parseia desc,
                           extrai bitmaps, ToolbarSize do Add.bmp
IconThemeService         — singleton: varre %APPDATA%\Arcana\Themes,
                           instala (copy), troca provider, notifica ThemeChanged
```
- Menu **Tools → Themes**: submenu com temas instalados + "Install Theme…" + "Open Themes Folder"
- Pasta temas: `%APPDATA%\Arcana\Themes` (espelho `%APPDATA%\WinRAR\Themes`)

## 6. ViewModels + Services

### ViewModels
| VM | Responsabilidade |
|---|---|
| `MainViewModel` | Estado do archive, commands toolbar (Open/Add/Extract/Test/View/Delete/Search/Info), breadcrumb, filter, status, coordination |
| `ArchiveViewModel` | Entries da tabela, seleção, sorting, current path |
| `FileEntryItem` | Wrapper de `ArchiveEntry`+`ArchiveNode`: SizeText, PackedText, RatioText, ModifiedText, TypeText, IconKey/Image, IsDirectory |
| `PreviewViewModel` | ContentType, TextContent, HexContent, ImageSource, CanEdit |
| `ToolsViewModel` | Split/Join/Hash (wiring fase 4) |
| `SettingsViewModel` | theme, language, default format/level, threads (fase 4) |

### Services
| Service | API |
|---|---|
| `ArchiveService` | `OpenAsync(path)`, `SaveAsync(...)`, `ExtractAsync(dest, progress, ct)`, `TestAsync(progress, ct)`, `AddFiles(paths)`, `DeleteEntry(node)` |
| `DialogService` | `PickOpenFileAsync()`, `PickOpenFilesAsync()`, `PickFolderAsync()`, `PickSaveFileAsync()` (StorageProvider) |
| `PreviewService` | `DetectType(fileName)`, `LoadText(node)`, `LoadHex(node)`, `LoadImage(node)` |
| `IconThemeService` | `InstalledThemes`, `Current`, `Install(path)`, `Apply(name)` |
| `NavigationService` | (fase 2) breadcrumb, histórico |

### DI (App.axaml.cs)
Singletons: `ArchiveService`, `DialogService`, `PreviewService`, `IconThemeService`. Transients: VMs.

## 7. Wizards (nostalgia-moderna, fase 2)

Shell `StepperDialog`: coluna esquerda passos numerados (1,2,3... com título), conteúdo central, barra Back/Next/Finish/Cancel. Logo do tema no topo.

| Wizard | Passos |
|---|---|
| **New Archive** | 1. Formato (cards ZIP/7Z/ZST/TAR + nível) → 2. Arquivos (multi-pick + sumário) → 3. Opções (encrypt/password/split) → 4. Progresso |
| **Extract** | 1. Destino (tree browser estilo "Extract To" WinRAR) → 2. Opções (overwrite, filter, paths) → 3. Progresso |
| **Convert** | 1. Origem → 2. Formato alvo → 3. Destino → 4. Progresso |
| **Test** | lista de arquivos com progresso rolando (2004 feel) |

## 8. Fases

### Fase 1 — Shell + tema + ícones (ATUAL)
1. Pacotes: `Avalonia.Controls.DataGrid`; ícones (ver nota abaixo)
2. Theme system: `Colors.axaml`, `Controls.axaml`, App.axaml `RequestedThemeVariant=Dark`
3. Icon engine: `IconKey`, `IIconProvider`, `DefaultIconProvider`
4. Icon engine WinRAR: `WinRarThemeProvider`, `IconThemeService`, menu Themes
5. Controles: `ToolBar`, `StatusBar`, `FileTable`, `FolderTree`, `PreviewPanel`
6. ViewModels/Services: `MainViewModel`, `ArchiveViewModel`, `FileEntryItem`, `ArchiveService` (Extract/Test), `DialogService`, `PreviewService`
7. `MainWindow` two-pane completo
8. Verificação: build + 137 tests + smoke (abrir .zip/.rar, navegar, extrair, testar, deletar, trocar tema)

> **Nota ícones**: avaliação de `Icons.Avalonia` pendente (busca NuGet inconclusiva). Fallback: geometrias hand-written (paths Feather-style MIT) no `DefaultIconProvider`. Decisão registrada conforme resultado da instalação.

### Fase 2 — Wizards
`StepperDialog` + New Archive + Extract + Convert + Test. Logo do tema nos wizards.

### Fase 3 — Moderno
Command palette (Ctrl+K), preview inline completo (imagem via codec nativo), drag & drop (add/extrair), filter box reativo, recent archives, busca.

### Fase 4 — Settings + polish
Settings window (theme, language, defaults), light theme, Tools wired (split/join/hash reais), progress overlay com speed/ETA, shell extension (Windows).

### Fase 5 — i18n
EN + PT-BR, resource files, language switcher.

## 9. Checklist de verificação

- [ ] `dotnet build src/Arcana.slnx` 0 erros
- [ ] `dotnet test src/Arcana.slnx` 137 verdes
- [ ] Smoke: abrir `.zip` e `.rar` reais
- [ ] Navegar árvore → breadcrumb sincroniza
- [ ] Sort por coluna funciona
- [ ] Extrair para pasta com progresso
- [ ] Test archive passa/falha corretamente
- [ ] Delete remove do VFS (dirty)
- [ ] Instalar tema `.theme.rar` → toolbar troca tamanho nativo
- [ ] Trocar tema sem crash; fallback pra slots ausentes
- [ ] Dark mode consistente (menus, dialogs, splitter, scrollbar)
- [ ] App sem warnings de binding (Output/Debug logs)
