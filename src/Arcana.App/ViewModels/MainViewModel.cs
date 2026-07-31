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
using Arcana.Core.Filesystem;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ArchiveService _archiveService;
    private readonly DialogService _dialogs;
    private readonly IconThemeService _themes;

    public ArchiveViewModel Archive { get; }
    public PreviewViewModel Preview { get; }
    public ObservableCollection<ToolBarButton> Toolbar { get; } = [];
    public ObservableCollection<ThemeMenuItem> ThemeMenuItems { get; } = [];

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

    private string? _currentPath;

    public IconThemeService IconThemes => _themes;

    public MainViewModel(ArchiveService archiveService, PreviewService previewService,
                         DialogService dialogs, IconThemeService themes,
                         DefaultIconProvider defaultIcons)
    {
        _archiveService = archiveService;
        _dialogs = dialogs;
        _themes = themes;
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

        BuildToolbar();
        RefreshThemes();
    }

    // ---- Toolbar / theme helpers ----

    private void BuildToolbar()
    {
        Toolbar.Clear();
        AddToolButton(IconKey.Open, "Open archive (Ctrl+O)", OpenArchiveCommand);
        AddToolButton(IconKey.Add, "New archive", NewArchiveCommand);
        AddToolButton(IconKey.Extract, "Extract to folder", ExtractCommand);
        AddToolButton(IconKey.Test, "Test archive", TestCommand);
        AddToolButton(IconKey.View, "Toggle preview", TogglePreviewCommand);
        AddToolButton(IconKey.Delete, "Delete selected (Del)", DeleteCommand);
        AddToolButton(IconKey.Find, "Find in folder (F3)", FindCommand);
        AddToolButton(IconKey.Info, "Properties (Alt+Enter)", InfoCommand);
        RefreshToolbarIcons();
    }

    private void AddToolButton(IconKey key, string toolTip, System.Windows.Input.ICommand command)
    {
        var button = new ToolBarButton { Icon = key, ToolTip = toolTip, Command = command };
        Toolbar.Add(button);
    }

    private void RefreshToolbarIcons()
    {
        foreach (var button in Toolbar)
            button.Image = _themes.Current.GetIcon(button.Icon);
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
                Archive.LoadArchive(archive);
                _currentPath = path;
                ArchiveName = Path.GetFileName(path);
                RefreshSelectionCommands();
            });
        });
    }

    [RelayCommand(CanExecute = nameof(CanExtract))]
    private async Task Extract()
    {
        var node = Archive.SelectedItem?.Node ?? Archive.CurrentNode;
        if (node == null)
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
            await _archiveService.ExtractAsync(archive, node, targetDir, progress);
        });

        await _dialogs.ShowInfoAsync("Extract complete",
            $"Extracted {Path.GetFileName(folderName)} to:\n{targetDir}");
    }

    private bool CanExtract() => Archive.HasArchive;

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
        TestCommand.NotifyCanExecuteChanged();
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
