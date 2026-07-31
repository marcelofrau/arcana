using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Arcana.App.Icons;
using Arcana.App.Localization;
using static Arcana.App.Localization.LocalizationManager;
using Arcana.App.Services;
using Arcana.App.Themes;
using Arcana.Core.Compression;
using Arcana.Core.Cryptography;
using Arcana.Core.Filesystem;
using Arcana.Core.Logging;
using Arcana.Core.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Arcana.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly ILogger Log = Serilog.Log.ForContext<MainViewModel>();

    private readonly ArchiveService _archiveService;
    private readonly DialogService _dialogs;
    private readonly IconThemeService _themes;
    private readonly ColorThemeService _colorThemes;
    private readonly SettingsService _settingsService;
    private readonly FavoritesService _favorites;
    private readonly DefaultIconProvider _defaultIcons;

    private AppSettings _settings;

    public ArchiveViewModel Archive { get; }
    public PreviewViewModel Preview { get; }
    public ObservableCollection<ToolBarButton> Toolbar { get; } = [];
    public ObservableCollection<ThemeMenuItem> ThemeMenuItems { get; } = [];
    public ObservableCollection<ThemeMenuItem> ColorThemeMenuItems { get; } = [];
    public IReadOnlyList<FavoriteItem> Favorites => _favorites.Items;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _archiveName = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyText = "";

    [ObservableProperty]
    private bool _previewVisible = true;

    [ObservableProperty]
    private bool _menuBarVisible = true;

    [ObservableProperty]
    private bool _toolbarVisible = true;

    [ObservableProperty]
    private bool _fileListVisible = true;

    [ObservableProperty]
    private bool _commentsVisible = true;

    private string? _currentPath;

    public IconThemeService IconThemes => _themes;
    public ColorThemeService ColorThemes => _colorThemes;

    public MainViewModel(ArchiveService archiveService, PreviewService previewService,
                         DialogService dialogs, IconThemeService themes,
                         DefaultIconProvider defaultIcons,
                         SettingsService settingsService, FavoritesService favoritesService,
                         ColorThemeService colorThemes)
    {
        _archiveService = archiveService;
        _dialogs = dialogs;
        _themes = themes;
        _settingsService = settingsService;
        _favorites = favoritesService;
        _defaultIcons = defaultIcons;
        _colorThemes = colorThemes;
        _settings = _settingsService.Load();
        Archive = new ArchiveViewModel();
        Preview = new PreviewViewModel(previewService);
        IconRuntime.Current = themes.Current;

        Archive.SelectionChanged += (_, item) =>
        {
            Preview.Show(item);
            RefreshSelectionCommands();
        };
        Archive.ContentsChanged += (_, _) => UpdateStatus();

        themes.Changed += (_, _) =>
        {
            IconRuntime.Current = themes.Current;
            RefreshToolbarIcons();
            Archive.RefreshCurrent();
            RefreshThemes();
        };

        _colorThemes.Changed += (_, _) =>
        {
            _defaultIcons.UpdateGlyphColor(_colorThemes.Current.TextPrimary);
            RefreshColorThemes();
        };
        _defaultIcons.UpdateGlyphColor(_colorThemes.Current.TextPrimary);

        _favorites.RebindCommands(_ => OpenFavoriteCommand);

        MenuBarVisible = _settings.ShowMenuBar;
        ToolbarVisible = _settings.ShowToolbar;
        FileListVisible = _settings.ShowFileList;
        CommentsVisible = _settings.ShowComments;

        BuildToolbar();
        RefreshThemes();
        RefreshColorThemes();
        Log.Debug("MainViewModel initialized (icon theme {IconTheme}, color theme {ColorTheme})",
            themes.Current.Name, _colorThemes.Current.Id);
    }

    // ---- Toolbar / theme helpers ----

    private void BuildToolbar()
    {
        Toolbar.Clear();
        AddToolButton(IconKey.Open, T("toolbar.open"), T("toolbar.openTooltip"), OpenArchiveCommand);
        AddToolButton(IconKey.Add, T("toolbar.new"), T("toolbar.newTooltip"), NewArchiveCommand);
        AddToolButton(IconKey.Extract, T("toolbar.extract"), T("toolbar.extractTooltip"), ExtractCommand);
        AddToolButton(IconKey.Test, T("toolbar.test"), T("toolbar.testTooltip"), TestCommand);
        AddToolButton(IconKey.View, T("toolbar.view"), T("toolbar.viewTooltip"), TogglePreviewCommand);
        AddToolButton(IconKey.Delete, T("toolbar.delete"), T("toolbar.deleteTooltip"), DeleteCommand);
        AddToolButton(IconKey.Find, T("toolbar.find"), T("toolbar.findTooltip"), FindCommand);
        AddToolButton(IconKey.Info, T("toolbar.info"), T("toolbar.infoTooltip"), InfoCommand);
        RefreshToolbarIcons();
    }

    private void AddToolButton(IconKey key, string label, string toolTip, System.Windows.Input.ICommand command)
    {
        var button = new ToolBarButton { Icon = key, Label = label, ToolTip = toolTip, Command = command };
        Toolbar.Add(button);
    }

    private void RefreshToolbarIcons()
    {
        var size = _themes.Current.ToolbarSize;
        foreach (var button in Toolbar)
        {
            button.Image = _themes.Current.GetIcon(button.Icon);
            button.Size = size;
        }
    }

    private void RefreshThemes()
    {
        ThemeMenuItems.Clear();
        foreach (var name in _themes.BuiltInThemes)
        {
            AddThemeItem(name);
        }
        foreach (var theme in _themes.InstalledThemes)
        {
            var title = theme.Title;
            ThemeMenuItems.Add(new ThemeMenuItem
            {
                Name = title,
                ApplyCommand = new RelayCommand(() => ApplyTheme(title)),
                IsCurrent = _themes.Current.Name == title,
            });
        }
    }

    private void AddThemeItem(string name)
    {
        ThemeMenuItems.Add(new ThemeMenuItem
        {
            Name = name,
            ApplyCommand = new RelayCommand(() => ApplyTheme(name)),
            IsCurrent = _themes.Current.Name == name,
        });
    }

    private void RefreshColorThemes()
    {
        ColorThemeMenuItems.Clear();
        foreach (var theme in _colorThemes.Themes)
        {
            ColorThemeMenuItems.Add(new ThemeMenuItem
            {
                Name = theme.Id,
                Label = theme.DisplayName,
                ApplyCommand = new RelayCommand(() => ApplyColorTheme(theme.Id)),
                IsCurrent = _colorThemes.Current.Id == theme.Id,
            });
        }
    }

    // ---- Commands ----

    [RelayCommand]
    private async Task OpenArchive()
    {
        var path = await _dialogs.PickArchiveAsync();
        if (path == null)
            return;
        await OpenPathAsync(path);
    }

    [RelayCommand]
    private async Task NewArchive()
    {
        var path = await _dialogs.PickSaveArchiveAsync(T("msg.newArchiveName"));
        if (path == null)
            return;

        await RunBusyAsync(T("msg.busy.creating"), async () =>
        {
            var engine = ArchiveFactory.GetFormat(CompressionFormat.Zip);
            var archive = new Archive
            {
                Format = CompressionFormat.Zip,
                FormatEngine = engine,
                Entries = Array.Empty<ArchiveEntry>(),
                Vfs = new VirtualFileSystem(),
            };
            await _archiveService.SaveAsync(archive, path, new CompressionOptions());
            archive.Dispose();
        });

        await OpenPathAsync(path);
    }

    private async Task OpenPathAsync(string path)
    {
        Log.Information("Opening archive {Path}", path);
        await RunBusyAsync(T("msg.busy.opening", Path.GetFileName(path)), async () =>
        {
            var archive = await _archiveService.OpenAsync(path);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Archive.LoadArchive(archive, Path.GetFileName(path));
                _currentPath = path;
                ArchiveName = Path.GetFileName(path);
                RefreshSelectionCommands();
            });
        });
    }

    [RelayCommand(CanExecute = nameof(CanExtract))]
    private async Task Extract()
    {
        var nodes = Archive.SelectedNodes;
        if (nodes.Count == 0)
            nodes = Archive.CurrentNode != null ? new[] { Archive.CurrentNode } : Array.Empty<ArchiveNode>();
        if (nodes.Count == 0)
            return;

        var dest = await _dialogs.PickDirectoryAsync(T("msg.selectDest"));
        if (dest == null)
            return;

        var folderName = Path.GetFileNameWithoutExtension(_currentPath ?? "archive");
        var targetDir = Path.Combine(dest, string.IsNullOrEmpty(folderName) ? "extracted" : folderName);
        var archive = Archive.Archive;
        if (archive == null)
            return;

        await RunBusyAsync(T("msg.busy.extracting"), async () =>
        {
            var progress = new Progress<ProgressReport>(r =>
                BusyText = T("msg.busy.extractProgress", r.CurrentFile, r.FilesProcessed, r.TotalFiles));
            foreach (var node in nodes)
                await _archiveService.ExtractAsync(archive, node, targetDir, progress);
        });

        Log.Information("Extracted {Count} node(s) to {Target}", nodes.Count, targetDir);
        await _dialogs.ShowInfoAsync(T("msg.extractComplete"),
            T("msg.extractedTo", targetDir));
    }

    private bool CanExtract() => Archive.HasArchive;

    [RelayCommand(CanExecute = nameof(CanExtract))]
    private async Task ExtractToCurrent()
    {
        var nodes = Archive.SelectedNodes;
        if (nodes.Count == 0)
            nodes = Archive.CurrentNode != null ? new[] { Archive.CurrentNode } : Array.Empty<ArchiveNode>();
        if (nodes.Count == 0 || _currentPath == null)
            return;

        var dest = Path.GetDirectoryName(_currentPath) ?? Environment.CurrentDirectory;
        var folderName = Path.GetFileNameWithoutExtension(_currentPath);
        var targetDir = Path.Combine(dest, string.IsNullOrEmpty(folderName) ? "extracted" : folderName);
        var archive = Archive.Archive;
        if (archive == null)
            return;

        await RunBusyAsync(T("msg.busy.extracting"), async () =>
        {
            var progress = new Progress<ProgressReport>(r =>
                BusyText = T("msg.busy.extractProgress", r.CurrentFile, r.FilesProcessed, r.TotalFiles));
            foreach (var node in nodes)
                await _archiveService.ExtractAsync(archive, node, targetDir, progress);
        });
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task Test()
    {
        var archive = Archive.Archive;
        var node = Archive.CurrentNode ?? Archive.Root;
        if (archive == null || node == null)
            return;

        IReadOnlyList<TestResult> results = Array.Empty<TestResult>();
        await RunBusyAsync(T("msg.busy.testing"), async () =>
        {
            var progress = new Progress<ProgressReport>(r =>
                BusyText = T("msg.busy.testProgress", r.CurrentFile, r.FilesProcessed, r.TotalFiles));
            results = await _archiveService.TestAsync(archive, node, progress);
        });

        var ok = results.Count(r => r.Success);
        var failed = results.Count - ok;
        Log.Information("Archive test finished: {Ok} OK, {Failed} failed", ok, failed);
        var detail = failed > 0
            ? T("msg.failedDetail", string.Join("\n", results.Where(r => !r.Success).Take(10).Select(r => r.Path)))
            : "";
        await _dialogs.ShowInfoAsync(T("msg.testResult"),
            T("msg.testResultDetail", ok, failed) + detail);
    }

    private bool CanTest() => Archive.HasArchive;

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task AddToArchive()
    {
        var archive = Archive.Archive;
        if (archive == null)
            return;

        var files = await _dialogs.PickFilesAsync(T("msg.addFilesPicker"), allowMultiple: true);
        if (files.Count == 0)
            return;

        Log.Debug("Adding {Count} file(s) to archive", files.Count);

        var inPlace = _currentPath != null && IsWritableFormat(archive.Format);
        var targetPath = inPlace
            ? _currentPath!
            : await _dialogs.PickSaveCopyAsync(ArchiveName.Length > 0 ? ArchiveName : T("msg.newArchiveName"));
        if (targetPath == null)
            return;

        var targetFormat = inPlace ? archive.Format : CompressionFormat.Zip;
        var prefix = Archive.CurrentNode?.FullPath.TrimStart('/') ?? "";
        var tempPath = targetPath + ".tmp";

        await RunBusyAsync(T("msg.busy.adding"), async () =>
        {
            foreach (var file in files)
            {
                using var content = File.OpenRead(file);
                var ms = new MemoryStream();
                await content.CopyToAsync(ms);
                ms.Position = 0;
                var rel = string.IsNullOrEmpty(prefix) ? Path.GetFileName(file) : $"{prefix}/{Path.GetFileName(file)}";
                archive.Vfs.AddFile(rel, ms);
            }

            var engine = ArchiveFactory.GetFormat(targetFormat);
            await using var stream = File.Create(tempPath);
            await engine.SaveAsync(archive, stream, BuildOptions(targetFormat));
        });

        await SwapInPlace(tempPath, targetPath, inPlace && targetPath == _currentPath);
    }

    private async Task SwapInPlace(string tempPath, string targetPath, bool reopenSame)
    {
        if (!reopenSame)
        {
            await OpenPathAsync(targetPath);
            return;
        }

        Archive.Close();
        _archiveService.Close();
        try
        {
            File.Move(tempPath, targetPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not update archive in place (temp {Temp}, target {Target})", tempPath, targetPath);
            await _dialogs.ShowInfoAsync(T("msg.error"), T("msg.couldNotUpdate", ex.Message));
            try { File.Delete(tempPath); } catch { /* best effort */ }
            return;
        }
        await OpenPathAsync(targetPath);
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task SaveCopyAs()
    {
        var archive = Archive.Archive;
        if (archive == null)
            return;

        var path = await _dialogs.PickSaveCopyAsync(ArchiveName);
        if (path == null)
            return;

        await RunBusyAsync(T("msg.busy.savingCopy"), async () =>
        {
            var targetFormat = IsWritableFormat(archive.Format) ? archive.Format : CompressionFormat.Zip;
            var engine = ArchiveFactory.GetFormat(targetFormat);
            await using var stream = File.Create(path);
            await engine.SaveAsync(archive, stream, BuildOptions(targetFormat));
        });

        await _dialogs.ShowInfoAsync(T("msg.saveComplete"), T("msg.savedCopy", path));
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task SplitArchive()
    {
        if (_currentPath == null)
            return;

        var dest = await _dialogs.PickDirectoryAsync(T("msg.selectFolderForSplit"));
        if (dest == null)
            return;

        var sizeMb = await _dialogs.ShowPromptAsync(T("dialog.split.partSize"), "100");
        if (sizeMb == null || !double.TryParse(sizeMb, out var mb) || mb <= 0)
            return;

        await RunBusyAsync(T("msg.busy.splitting"), async () =>
        {
            var splitter = new FileSplitter();
            var progress = new Progress<ProgressReport>(r =>
                BusyText = T("msg.busy.splitProgress", r.CurrentFile, ByteFormat.Format(r.BytesProcessed), ByteFormat.Format(r.TotalBytes)));
            await splitter.SplitAsync(_currentPath, dest, (long)(mb * 1024 * 1024), progress);
        });
    }

    [RelayCommand]
    private async Task SplitFile()
    {
        var files = await _dialogs.PickFilesAsync(T("msg.selectFileToSplit"), false);
        if (files.Count == 0)
            return;

        var choice = await _dialogs.ShowSplitFileAsync(files[0]);
        if (choice == null)
            return;

        await RunBusyAsync(T("msg.busy.splitting"), async () =>
        {
            var splitter = new FileSplitter();
            var progress = new Progress<ProgressReport>(r =>
                BusyText = T("msg.busy.splitProgress", r.CurrentFile, ByteFormat.Format(r.BytesProcessed), ByteFormat.Format(r.TotalBytes)));
            await splitter.SplitAsync(choice.SourcePath, choice.DestinationDir,
                (long)(choice.PartSizeMb * 1024 * 1024), progress, hjsplitMode: choice.HjsplitMode);
        });
        Log.Information("Split {Source} into {PartSize}MB parts at {Dest}",
            choice.SourcePath, choice.PartSizeMb, choice.DestinationDir);
    }

    [RelayCommand]
    private async Task JoinFile()
    {
        var choice = await _dialogs.ShowJoinFileAsync();
        if (choice == null)
            return;

        var parts = FileJoiner.AutoDiscoverParts(choice.FirstPart);
        await RunBusyAsync(T("msg.busy.joining"), async () =>
        {
            var joiner = new FileJoiner();
            var progress = new Progress<ProgressReport>(r =>
                BusyText = T("msg.busy.joinProgress", r.CurrentFile, ByteFormat.Format(r.BytesProcessed), ByteFormat.Format(r.TotalBytes)));
            await joiner.JoinAsync(parts, choice.OutputPath, progress);
        });
        Log.Information("Joined {Count} part(s) into {Output}", parts.Count, choice.OutputPath);
    }

    [RelayCommand]
    private async Task HashFile()
    {
        var files = await _dialogs.PickFilesAsync(T("msg.selectFileToHash"), false);
        if (files.Count == 0)
            return;

        var choice = await _dialogs.ShowHashFileAsync(files[0]);
        if (choice == null)
            return;

        var algorithm = choice.Algorithm switch
        {
            "MD5" => HashAlgorithm.Md5,
            "SHA-1" => HashAlgorithm.Sha1,
            "SHA-512" => HashAlgorithm.Sha512,
            _ => HashAlgorithm.Sha256,
        };

        var hash = "";
        await RunBusyAsync(T("msg.busy.hashing"), async () =>
        {
            var calc = new HashCalculator();
            await using var stream = File.OpenRead(choice.FilePath);
            hash = await calc.ComputeHashAsync(stream, algorithm);
        });

        if (hash.Length > 0)
        {
            Log.Information("Computed {Algorithm} hash for {File}", choice.Algorithm, choice.FilePath);
            await _dialogs.ShowInfoAsync($"{choice.Algorithm} hash", hash);
        }
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task ConvertArchive()
    {
        var archive = Archive.Archive;
        if (archive == null)
            return;

        var choice = await _dialogs.ShowConvertAsync(ArchiveName);
        if (choice == null)
            return;

        var ext = choice.Format == "7z" ? ".7z" : choice.Format == "zstd" ? ".zst" : ".zip";
        var suggested = Path.GetFileNameWithoutExtension(ArchiveName) + ext;
        var path = await _dialogs.PickSaveCopyAsync(suggested);
        if (path == null)
            return;

        await RunBusyAsync(T("msg.busy.converting"), async () =>
        {
            var targetFormat = ParseFormat(choice.Format);
            var engine = ArchiveFactory.GetFormat(targetFormat);
            await using var stream = File.Create(path);
            await engine.SaveAsync(archive, stream, BuildOptions(targetFormat, choice.Level));
        });
        Log.Information("Converted archive to {Target}", path);
        await _dialogs.ShowInfoAsync(T("msg.convertComplete"), T("msg.converted", path));
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task SetPassword()
    {
        var archive = Archive.Archive;
        if (archive == null)
            return;

        var password = await _dialogs.ShowPasswordAsync(T("dialog.password.title"));
        if (string.IsNullOrEmpty(password))
            return;

        var inPlace = _currentPath != null && archive.Format == CompressionFormat.Zip;
        var targetPath = inPlace
            ? _currentPath!
            : await _dialogs.PickSaveCopyAsync(ArchiveName);
        if (targetPath == null)
            return;

        var tempPath = targetPath + ".tmp";
        Log.Information("Setting password on archive (output {Target}, in-place {InPlace})",
            targetPath, inPlace);
        await RunBusyAsync(T("msg.busy.encrypting"), async () =>
        {
            var engine = ArchiveFactory.GetFormat(CompressionFormat.Zip);
            var options = new CompressionOptions
            {
                Format = CompressionFormat.Zip,
                Level = CompressionLevel.Normal,
                Encryption = new EncryptionOptions { Password = password },
            };
            await using var stream = File.Create(tempPath);
            await engine.SaveAsync(archive, stream, options);
        });

        await SwapInPlace(tempPath, targetPath, inPlace);
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task CompareArchives()
    {
        var first = Archive.Archive;
        if (first == null)
            return;

        var path = await _dialogs.PickArchiveAsync();
        if (path == null)
            return;

        Archive? second = null;
        try
        {
            second = await _archiveService.OpenAsync(path);
            var a = IndexEntries(first);
            var b = IndexEntries(second);

            var onlyA = a.Keys.Where(k => !b.ContainsKey(k)).OrderBy(k => k).ToList();
            var onlyB = b.Keys.Where(k => !a.ContainsKey(k)).OrderBy(k => k).ToList();
            var different = a.Keys.Where(k => b.ContainsKey(k) &&
                (a[k].Size != b[k].Size || a[k].Packed != b[k].Packed)).OrderBy(k => k).ToList();

            var lines = new List<string>
            {
                $"{Path.GetFileName(_currentPath)}: {a.Count} files",
                $"{Path.GetFileName(path)}: {b.Count} files",
                "",
                $"Only in first: {onlyA.Count}",
                $"Only in second: {onlyB.Count}",
                $"Different size/packed: {different.Count}",
            };
            Log.Debug("Compared archives: {First} vs {Second} ({OnlyA} only first, {OnlyB} only second, {Diff} different)",
                Path.GetFileName(_currentPath), Path.GetFileName(path), onlyA.Count, onlyB.Count, different.Count);
            if (onlyA.Count > 0)
                lines.Add("\nOnly in first:\n  " + string.Join("\n  ", onlyA.Take(10)));
            if (onlyB.Count > 0)
                lines.Add("\nOnly in second:\n  " + string.Join("\n  ", onlyB.Take(10)));
            if (different.Count > 0)
                lines.Add("\nDifferent:\n  " + string.Join("\n  ", different.Take(10)));

            await _dialogs.ShowInfoAsync(T("msg.compare"), string.Join("\n", lines));
        }
        finally
        {
            second?.Dispose();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectAll))]
    private void SelectAll()
    {
        Archive.SelectAllCommand.Execute(null);
    }

    private bool CanSelectAll() => Archive.HasArchive;

    [RelayCommand]
    private async Task OpenSettings()
    {
        var vm = await _dialogs.ShowSettingsAsync();
        if (vm == null)
            return;

        _settings = new AppSettings
        {
            DefaultFormat = vm.DefaultFormat,
            DefaultCompressionLevel = vm.DefaultCompressionLevel,
            ThreadCount = vm.ThreadCount,
            EnableParallel = vm.EnableParallel,
            LogLevel = vm.LogLevel,
            Language = vm.Language?.Code ?? "en",
            ShowMenuBar = MenuBarVisible,
            ShowToolbar = ToolbarVisible,
            ShowFileList = FileListVisible,
            ShowComments = CommentsVisible,
        };
        _settingsService.Save(_settings);
        LocalizationManager.Instance.SetCulture(vm.Language?.Code ?? "en");
        LogConfig.SetLevel(vm.LogLevel);
        Log.Information("Settings saved (default format {Format}, log level {LogLevel})",
            _settings.DefaultFormat, vm.LogLevel);
    }

    [RelayCommand]
    private async Task AddFavorite()
    {
        if (_currentPath == null)
            return;

        var name = await _dialogs.ShowPromptAsync(T("msg.addToFavoritesPrompt"), ArchiveName);
        if (string.IsNullOrWhiteSpace(name))
            return;

        _favorites.Add(name.Trim(), _currentPath);
        Log.Debug("Added favorite {Name}", name.Trim());
    }

    [RelayCommand]
    private async Task OpenFavorite(FavoriteItem item)
    {
        if (string.IsNullOrEmpty(item.Path) || !File.Exists(item.Path))
        {
            Log.Warning("Favorite {Name} points to missing file {Path}", item.Name, item.Path);
            await _dialogs.ShowInfoAsync(T("msg.favoriteNotFound"), T("msg.favoriteMissing", item.Path));
            return;
        }
        await OpenPathAsync(item.Path);
    }

    [RelayCommand]
    private async Task OrganizeFavorites()
    {
        if (_favorites.Items.Count == 0)
        {
            await _dialogs.ShowInfoAsync(T("msg.favorites"), T("msg.noFavorites"));
            return;
        }

        var list = _favorites.Items.Select(f => f.Name).ToList();
        await _dialogs.ShowInfoAsync(T("msg.favorites"),
            string.Join("\n", list) + "\n\nRemove via Favorites menu → right-click not supported yet.");
    }

    [RelayCommand]
    private void ToggleMenuBar() => ToggleSetting(nameof(MenuBarVisible), MenuBarVisible = !MenuBarVisible);
    [RelayCommand]
    private void ToggleToolbar() => ToggleSetting(nameof(ToolbarVisible), ToolbarVisible = !ToolbarVisible);
    [RelayCommand]
    private void ToggleFileList() => ToggleSetting(nameof(FileListVisible), FileListVisible = !FileListVisible);
    [RelayCommand]
    private void ToggleComments() => ToggleSetting(nameof(CommentsVisible), CommentsVisible = !CommentsVisible);

    [RelayCommand]
    private async Task HelpTopics()
    {
        await _dialogs.ShowInfoAsync(T("msg.helpTitle"), T("msg.helpDetail"));
    }

    [RelayCommand]
    private void OpenArchiveWith() { }
    [RelayCommand]
    private void RepairArchive() { }
    [RelayCommand]
    private void ProtectArchive() { }
    [RelayCommand]
    private void LockArchive() { }
    [RelayCommand]
    private void Wizard() { }
    [RelayCommand]
    private void BackupWizard() { }
    [RelayCommand]
    private void OrganizeDefaults() { }
    [RelayCommand]
    private void ChangeDrive() { }
    [RelayCommand]
    private void Browse() { }
    [RelayCommand]
    private void OptimizeImages() { }
    [RelayCommand]
    private void DuplicateFinder() { }
    [RelayCommand]
    private void SecureErase() { }

    private void ToggleSetting(string prop, bool value)
    {
        switch (prop)
        {
            case nameof(MenuBarVisible): _settings.ShowMenuBar = value; break;
            case nameof(ToolbarVisible): _settings.ShowToolbar = value; break;
            case nameof(FileListVisible): _settings.ShowFileList = value; break;
            case nameof(CommentsVisible): _settings.ShowComments = value; break;
        }
        _settingsService.Save(_settings);
    }

    private static CompressionFormat ParseFormat(string name) => name.ToLowerInvariant() switch
    {
        "7z" => CompressionFormat.SevenZip,
        "zstd" or "zst" => CompressionFormat.Zstandard,
        _ => CompressionFormat.Zip,
    };

    private static bool IsWritableFormat(CompressionFormat format) => format switch
    {
        CompressionFormat.Zip or CompressionFormat.SevenZip or CompressionFormat.Zstandard
            or CompressionFormat.Tar or CompressionFormat.TarGz or CompressionFormat.TarBz2
            or CompressionFormat.TarXz or CompressionFormat.TarZstd or CompressionFormat.GZip
            or CompressionFormat.BZip2 or CompressionFormat.Xz or CompressionFormat.Lzma
            or CompressionFormat.Brotli or CompressionFormat.Lz4 or CompressionFormat.Snappy => true,
        _ => false,
    };

    private static CompressionOptions BuildOptions(CompressionFormat format, int level = 5)
    {
        return new CompressionOptions
        {
            Format = format,
            Level = (CompressionLevel)Math.Clamp(level, 0, 10),
        };
    }

    private static Dictionary<string, (long Size, long Packed)> IndexEntries(Archive archive)
    {
        return archive.Entries
            .Where(e => !e.IsDirectory)
            .GroupBy(e => e.Path)
            .ToDictionary(g => g.Key, g => (g.First().Size, g.First().CompressedSize));
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        Archive.DeleteCommand.Execute(null);
    }

    private bool CanDelete() => Archive.SelectedItem != null;

    [RelayCommand(CanExecute = nameof(CanRename))]
    private async Task Rename()
    {
        var item = Archive.SelectedItem;
        if (item == null)
            return;

        var newName = await _dialogs.ShowPromptAsync(T("msg.rename"), item.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == item.Name)
            return;

        Log.Debug("Renamed {Old} to {New}", item.Name, newName);
        Archive.Archive?.Vfs.RenameNode(item.Node, newName);
        Archive.RefreshCurrent();
    }

    private bool CanRename() => Archive.SelectedItem != null;

    [RelayCommand(CanExecute = nameof(CanInfo))]
    private async Task Info()
    {
        var item = Archive.SelectedItem;
        if (item == null)
            return;

        var n = item.Node;
        var type = n.Type == NodeType.Directory ? T("msg.typeFolder") : T("msg.typeFile");
        var message =
            $"{T("msg.propertyName")}  {n.FullPath}\n" +
            $"{T("msg.propertyType")}  {type}\n" +
            $"{T("msg.propertySize")}  {ByteFormat.Format(n.OriginalSize)}\n" +
            $"{T("msg.propertyPacked")}  {ByteFormat.Format(n.CompressedSize)}\n" +
            $"{T("msg.propertyModified")} {n.LastModified:yyyy-MM-dd HH:mm:ss}";

        await _dialogs.ShowInfoAsync(T("msg.properties"), message);
    }

    private bool CanInfo() => Archive.SelectedItem != null;

    [RelayCommand]
    private void TogglePreview()
    {
        PreviewVisible = !PreviewVisible;
        Preview.IsVisible = PreviewVisible;
    }

    [RelayCommand]
    private async Task Find()
    {
        var term = await _dialogs.ShowPromptAsync(T("msg.findInFolder"), Archive.Filter);
        if (term == null)
            return;
        Archive.Filter = term;
    }

    private void ApplyTheme(string name)
    {
        Log.Debug("Applying icon theme {Theme}", name);
        _themes.ApplyTheme(name);
    }

    private void ApplyColorTheme(string id)
    {
        Log.Debug("Applying color theme {Theme}", id);
        _colorThemes.Apply(id);
    }

    [RelayCommand]
    private async Task InstallTheme()
    {
        var path = await _dialogs.PickThemeAsync();
        if (path == null)
            return;

        if (_themes.InstallTheme(path))
            StatusText = T("msg.themeInstalled");
        else
            await _dialogs.ShowInfoAsync(T("msg.installFailed"), T("msg.installFailedDetail"));
    }

    [RelayCommand]
    private void OpenThemesFolder()
    {
        _themes.OpenThemesFolder();
    }

    [RelayCommand]
    private async Task About()
    {
        await _dialogs.ShowInfoAsync(T("msg.about"), T("msg.aboutDetail", VersionInfo, "zip, 7z, rar, tar, gz, bz2, xz, zst, cab, arj, lzh, lzma, br, lz4, snappy"));
    }

    private static string VersionInfo
    {
        get
        {
            var attr = typeof(MainViewModel).Assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            var version = attr?.InformationalVersion ?? typeof(MainViewModel).Assembly.GetName().Version?.ToString() ?? "unknown";
            return version.Split('+')[0];
        }
    }

    [RelayCommand]
    private void Exit()
    {
        _dialogs.MainWindow?.Close();
    }

    // ---- Plumbing ----

    private async Task RunBusyAsync(string busyText, Func<Task> work)
    {
        IsBusy = true;
        BusyText = busyText;
        StatusText = busyText;
        try
        {
            Log.Debug("Busy: {BusyText}", busyText);
            await Task.Run(work);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Operation failed: {BusyText}", busyText);
            await _dialogs.ShowInfoAsync(T("msg.error"), ex.Message);
            StatusText = T("msg.error");
        }
        finally
        {
            IsBusy = false;
            BusyText = "";
            UpdateStatus();
        }
    }

    private void RefreshSelectionCommands()
    {
        DeleteCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
        InfoCommand.NotifyCanExecuteChanged();
        ExtractCommand.NotifyCanExecuteChanged();
        ExtractToCurrentCommand.NotifyCanExecuteChanged();
        TestCommand.NotifyCanExecuteChanged();
        AddToArchiveCommand.NotifyCanExecuteChanged();
        SaveCopyAsCommand.NotifyCanExecuteChanged();
        SplitArchiveCommand.NotifyCanExecuteChanged();
        ConvertArchiveCommand.NotifyCanExecuteChanged();
        SetPasswordCommand.NotifyCanExecuteChanged();
        CompareArchivesCommand.NotifyCanExecuteChanged();
        SelectAllCommand.NotifyCanExecuteChanged();
    }

    private void UpdateStatus()
    {
        if (IsBusy)
            return;
        StatusText = Archive.HasArchive
            ? $"{ArchiveName} · {Archive.BuildStatusText()}"
            : T("status.ready");
    }
}
