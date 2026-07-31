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
using Arcana.App.Services;
using Arcana.Core.Compression;
using Arcana.Core.Cryptography;
using Arcana.Core.Filesystem;
using Arcana.Core.Logging;
using Arcana.Core.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ArchiveService _archiveService;
    private readonly DialogService _dialogs;
    private readonly IconThemeService _themes;
    private readonly SettingsService _settingsService;
    private readonly FavoritesService _favorites;

    private AppSettings _settings;

    public ArchiveViewModel Archive { get; }
    public PreviewViewModel Preview { get; }
    public ObservableCollection<ToolBarButton> Toolbar { get; } = [];
    public ObservableCollection<ThemeMenuItem> ThemeMenuItems { get; } = [];
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

    public MainViewModel(ArchiveService archiveService, PreviewService previewService,
                         DialogService dialogs, IconThemeService themes,
                         DefaultIconProvider defaultIcons,
                         SettingsService settingsService, FavoritesService favoritesService)
    {
        _archiveService = archiveService;
        _dialogs = dialogs;
        _themes = themes;
        _settingsService = settingsService;
        _favorites = favoritesService;
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

        _favorites.RebindCommands(_ => OpenFavoriteCommand);

        MenuBarVisible = _settings.ShowMenuBar;
        ToolbarVisible = _settings.ShowToolbar;
        FileListVisible = _settings.ShowFileList;
        CommentsVisible = _settings.ShowComments;

        BuildToolbar();
        RefreshThemes();
    }

    // ---- Toolbar / theme helpers ----

    private void BuildToolbar()
    {
        Toolbar.Clear();
        AddToolButton(IconKey.Open, "Open", "Open archive (Ctrl+O)", OpenArchiveCommand);
        AddToolButton(IconKey.Add, "New", "New archive", NewArchiveCommand);
        AddToolButton(IconKey.Extract, "Extract", "Extract to folder", ExtractCommand);
        AddToolButton(IconKey.Test, "Test", "Test archive", TestCommand);
        AddToolButton(IconKey.View, "View", "Toggle preview", TogglePreviewCommand);
        AddToolButton(IconKey.Delete, "Delete", "Delete selected (Del)", DeleteCommand);
        AddToolButton(IconKey.Find, "Find", "Find in folder (F3)", FindCommand);
        AddToolButton(IconKey.Info, "Info", "Properties (Alt+Enter)", InfoCommand);
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
        AddThemeItem(PapirusIconProvider.BuiltInName);
        AddThemeItem(PapirusIconProvider.MaterialName);
        foreach (var theme in _themes.InstalledThemes)
        {
            ThemeMenuItems.Add(new ThemeMenuItem
            {
                Name = theme.Title,
                ApplyCommand = ApplyThemeCommand,
                IsCurrent = _themes.Current.Name == theme.Title,
            });
        }
    }

    private void AddThemeItem(string name)
    {
        ThemeMenuItems.Add(new ThemeMenuItem
        {
            Name = name,
            ApplyCommand = ApplyThemeCommand,
            IsCurrent = _themes.Current.Name == name,
        });
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
        var path = await _dialogs.PickSaveArchiveAsync("New Archive.zip");
        if (path == null)
            return;

        await RunBusyAsync("Creating archive...", async () =>
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
        await RunBusyAsync($"Opening {Path.GetFileName(path)}...", async () =>
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

        var dest = await _dialogs.PickDirectoryAsync("Select destination folder");
        if (dest == null)
            return;

        var folderName = Path.GetFileNameWithoutExtension(_currentPath ?? "archive");
        var targetDir = Path.Combine(dest, string.IsNullOrEmpty(folderName) ? "extracted" : folderName);
        var archive = Archive.Archive;
        if (archive == null)
            return;

        await RunBusyAsync("Extracting...", async () =>
        {
            var progress = new Progress<ProgressReport>(r =>
                BusyText = $"Extracting {r.CurrentFile} ({r.FilesProcessed}/{r.TotalFiles})");
            foreach (var node in nodes)
                await _archiveService.ExtractAsync(archive, node, targetDir, progress);
        });

        await _dialogs.ShowInfoAsync("Extract complete",
            $"Extracted to:\n{targetDir}");
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

        await RunBusyAsync("Extracting...", async () =>
        {
            var progress = new Progress<ProgressReport>(r =>
                BusyText = $"Extracting {r.CurrentFile} ({r.FilesProcessed}/{r.TotalFiles})");
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
        await RunBusyAsync("Testing archive...", async () =>
        {
            var progress = new Progress<ProgressReport>(r =>
                BusyText = $"Testing {r.CurrentFile} ({r.FilesProcessed}/{r.TotalFiles})");
            results = await _archiveService.TestAsync(archive, node, progress);
        });

        var ok = results.Count(r => r.Success);
        var failed = results.Count - ok;
        var detail = failed > 0
            ? "\n\nFailed:\n" + string.Join("\n", results.Where(r => !r.Success).Take(10).Select(r => r.Path))
            : "";
        await _dialogs.ShowInfoAsync("Test result",
            $"{ok} file(s) OK, {failed} failed.{detail}");
    }

    private bool CanTest() => Archive.HasArchive;

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task AddToArchive()
    {
        var archive = Archive.Archive;
        if (archive == null)
            return;

        var files = await _dialogs.PickFilesAsync("Add files to archive", allowMultiple: true);
        if (files.Count == 0)
            return;

        var inPlace = _currentPath != null && IsWritableFormat(archive.Format);
        var targetPath = inPlace
            ? _currentPath!
            : await _dialogs.PickSaveCopyAsync(ArchiveName.Length > 0 ? ArchiveName : "archive.zip");
        if (targetPath == null)
            return;

        var targetFormat = inPlace ? archive.Format : CompressionFormat.Zip;
        var prefix = Archive.CurrentNode?.FullPath.TrimStart('/') ?? "";
        var tempPath = targetPath + ".tmp";

        await RunBusyAsync("Adding files...", async () =>
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
            await _dialogs.ShowInfoAsync("Error", $"Could not update archive:\n{ex.Message}");
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

        await RunBusyAsync("Saving copy...", async () =>
        {
            var targetFormat = IsWritableFormat(archive.Format) ? archive.Format : CompressionFormat.Zip;
            var engine = ArchiveFactory.GetFormat(targetFormat);
            await using var stream = File.Create(path);
            await engine.SaveAsync(archive, stream, BuildOptions(targetFormat));
        });

        await _dialogs.ShowInfoAsync("Save complete", $"Saved copy to:\n{path}");
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task SplitArchive()
    {
        if (_currentPath == null)
            return;

        var dest = await _dialogs.PickDirectoryAsync("Select folder for split parts");
        if (dest == null)
            return;

        var sizeMb = await _dialogs.ShowPromptAsync("Part size (MB)", "100");
        if (sizeMb == null || !double.TryParse(sizeMb, out var mb) || mb <= 0)
            return;

        await RunBusyAsync("Splitting...", async () =>
        {
            var splitter = new FileSplitter();
            var progress = new Progress<ProgressReport>(r =>
                BusyText = $"Splitting {r.CurrentFile} ({ByteFormat.Format(r.BytesProcessed)}/{ByteFormat.Format(r.TotalBytes)})");
            await splitter.SplitAsync(_currentPath, dest, (long)(mb * 1024 * 1024), progress);
        });
    }

    [RelayCommand]
    private async Task SplitFile()
    {
        var files = await _dialogs.PickFilesAsync("Select file to split", false);
        if (files.Count == 0)
            return;

        var choice = await _dialogs.ShowSplitFileAsync(files[0]);
        if (choice == null)
            return;

        await RunBusyAsync("Splitting...", async () =>
        {
            var splitter = new FileSplitter();
            var progress = new Progress<ProgressReport>(r =>
                BusyText = $"Splitting {r.CurrentFile} ({ByteFormat.Format(r.BytesProcessed)}/{ByteFormat.Format(r.TotalBytes)})");
            await splitter.SplitAsync(choice.SourcePath, choice.DestinationDir,
                (long)(choice.PartSizeMb * 1024 * 1024), progress, hjsplitMode: choice.HjsplitMode);
        });
    }

    [RelayCommand]
    private async Task JoinFile()
    {
        var choice = await _dialogs.ShowJoinFileAsync();
        if (choice == null)
            return;

        await RunBusyAsync("Joining...", async () =>
        {
            var parts = FileJoiner.AutoDiscoverParts(choice.FirstPart);
            var joiner = new FileJoiner();
            var progress = new Progress<ProgressReport>(r =>
                BusyText = $"Joining {r.CurrentFile} ({ByteFormat.Format(r.BytesProcessed)}/{ByteFormat.Format(r.TotalBytes)})");
            await joiner.JoinAsync(parts, choice.OutputPath, progress);
        });
    }

    [RelayCommand]
    private async Task HashFile()
    {
        var files = await _dialogs.PickFilesAsync("Select file to hash", false);
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
        await RunBusyAsync("Hashing...", async () =>
        {
            var calc = new HashCalculator();
            await using var stream = File.OpenRead(choice.FilePath);
            hash = await calc.ComputeHashAsync(stream, algorithm);
        });

        if (hash.Length > 0)
            await _dialogs.ShowInfoAsync($"{choice.Algorithm} hash", hash);
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

        await RunBusyAsync("Converting...", async () =>
        {
            var targetFormat = ParseFormat(choice.Format);
            var engine = ArchiveFactory.GetFormat(targetFormat);
            await using var stream = File.Create(path);
            await engine.SaveAsync(archive, stream, BuildOptions(targetFormat, choice.Level));
        });

        await _dialogs.ShowInfoAsync("Convert complete", $"Created:\n{path}");
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task SetPassword()
    {
        var archive = Archive.Archive;
        if (archive == null)
            return;

        var password = await _dialogs.ShowPasswordAsync("Set password");
        if (string.IsNullOrEmpty(password))
            return;

        var inPlace = _currentPath != null && archive.Format == CompressionFormat.Zip;
        var targetPath = inPlace
            ? _currentPath!
            : await _dialogs.PickSaveCopyAsync(ArchiveName);
        if (targetPath == null)
            return;

        var tempPath = targetPath + ".tmp";
        await RunBusyAsync("Encrypting...", async () =>
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
            if (onlyA.Count > 0)
                lines.Add("\nOnly in first:\n  " + string.Join("\n  ", onlyA.Take(10)));
            if (onlyB.Count > 0)
                lines.Add("\nOnly in second:\n  " + string.Join("\n  ", onlyB.Take(10)));
            if (different.Count > 0)
                lines.Add("\nDifferent:\n  " + string.Join("\n  ", different.Take(10)));

            await _dialogs.ShowInfoAsync("Compare archives", string.Join("\n", lines));
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
            ShowMenuBar = MenuBarVisible,
            ShowToolbar = ToolbarVisible,
            ShowFileList = FileListVisible,
            ShowComments = CommentsVisible,
        };
        _settingsService.Save(_settings);
        LogConfig.SetLevel(vm.LogLevel);
    }

    [RelayCommand]
    private async Task AddFavorite()
    {
        if (_currentPath == null)
            return;

        var name = await _dialogs.ShowPromptAsync("Add to Favorites", ArchiveName);
        if (string.IsNullOrWhiteSpace(name))
            return;

        _favorites.Add(name.Trim(), _currentPath);
    }

    [RelayCommand]
    private async Task OpenFavorite(FavoriteItem item)
    {
        if (string.IsNullOrEmpty(item.Path) || !File.Exists(item.Path))
        {
            await _dialogs.ShowInfoAsync("Favorite not found", $"File no longer exists:\n{item.Path}");
            return;
        }
        await OpenPathAsync(item.Path);
    }

    [RelayCommand]
    private async Task OrganizeFavorites()
    {
        if (_favorites.Items.Count == 0)
        {
            await _dialogs.ShowInfoAsync("Favorites", "No favorites yet. Use 'Add to Favorites'.");
            return;
        }

        var list = _favorites.Items.Select(f => f.Name).ToList();
        await _dialogs.ShowInfoAsync("Favorites",
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
        await _dialogs.ShowInfoAsync("Arcana Help",
            "Arcana — archive manager\n\n" +
            "Open (Ctrl+O): open an archive\n" +
            "Add (Alt+A): add files to the archive\n" +
            "Extract (Alt+E / Ctrl+E): extract to folder\n" +
            "Test (Alt+T): verify archive integrity\n" +
            "Convert (Alt+C): change archive format\n" +
            "Find (F3): filter the current folder\n" +
            "Select All (Ctrl+A): select every entry");
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

        var newName = await _dialogs.ShowPromptAsync("Rename", item.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == item.Name)
            return;

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
        var type = n.Type == NodeType.Directory ? "Folder" : "File";
        var message =
            $"Name:     {n.FullPath}\n" +
            $"Type:     {type}\n" +
            $"Size:     {ByteFormat.Format(n.OriginalSize)}\n" +
            $"Packed:   {ByteFormat.Format(n.CompressedSize)}\n" +
            $"Modified: {n.LastModified:yyyy-MM-dd HH:mm:ss}";

        await _dialogs.ShowInfoAsync("Properties", message);
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
        var term = await _dialogs.ShowPromptAsync("Find in folder", Archive.Filter);
        if (term == null)
            return;
        Archive.Filter = term;
    }

    [RelayCommand]
    private void ApplyTheme(ThemeMenuItem item)
    {
        _themes.ApplyTheme(item.Name);
    }

    [RelayCommand]
    private async Task InstallTheme()
    {
        var path = await _dialogs.PickThemeAsync();
        if (path == null)
            return;

        if (_themes.InstallTheme(path))
            StatusText = "Theme installed.";
        else
            await _dialogs.ShowInfoAsync("Install failed",
                "The selected file is not a valid WinRAR theme (.theme.rar).");
    }

    [RelayCommand]
    private void OpenThemesFolder()
    {
        _themes.OpenThemesFolder();
    }

    [RelayCommand]
    private async Task About()
    {
        await _dialogs.ShowInfoAsync("About Arcana",
            "Arcana — archive manager\n" +
            $"Version {VersionInfo}\n\n" +
            "Classic spirit, modern skin.\n\n" +
            "Supported formats: zip, 7z, rar, tar, gz, bz2, xz, zst,\n" +
            "cab, arj, lzh, lzma, br, lz4, snappy.");
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
            await Task.Run(work);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowInfoAsync("Error", ex.Message);
            StatusText = "Error";
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
            : "Ready";
    }
}
