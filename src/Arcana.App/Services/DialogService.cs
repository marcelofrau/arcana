using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Arcana.App.Localization;
using static Arcana.App.Localization.LocalizationManager;
using Arcana.App.ViewModels;
using Arcana.App.ViewModels.Wizard;
using Arcana.App.Views.Dialogs;
using Arcana.App.Views.Wizards;
using Serilog;

namespace Arcana.App.Services;

public sealed class DialogService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<DialogService>();

    private readonly SettingsService _settingsService;

    public DialogService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private static readonly string[] ArchivePatterns =
    {
        "*.zip", "*.rar", "*.7z", "*.tar", "*.gz", "*.tgz", "*.bz2", "*.xz", "*.zst",
        "*.cab", "*.arj", "*.lzh", "*.lzma", "*.br"
    };

    private static readonly FilePickerFileType ArchivesFilter = new("Archives")
    {
        Patterns = ArchivePatterns
    };

    private static readonly FilePickerFileType AllFilesFilter = new("All files")
    {
        Patterns = new[] { "*" }
    };

    private static readonly FilePickerFileType ThemeFilter = new("WinRAR theme")
    {
        Patterns = new[] { "*.theme.rar" }
    };

    private static readonly FilePickerFileType AnyFilesFilter = new("Files")
    {
        Patterns = new[] { "*" }
    };

    public Window? MainWindow
    {
        get
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }
    }

    private IStorageProvider? StorageProvider => MainWindow?.StorageProvider;

    public async Task<string?> PickArchiveAsync()
    {
        if (StorageProvider == null)
            return null;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = T("dialog.openArchive"),
            AllowMultiple = false,
            FileTypeFilter = BuildArchiveOpenFilter(),
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// Builds the open-archive file-type dropdown: one "all supported" entry, three
    /// category groups (modern / obsolete / unusual formats) and one entry per
    /// individual format, plus "all files". Names are localized at call time.
    /// </summary>
    private static List<FilePickerFileType> BuildArchiveOpenFilter()
    {
        static FilePickerFileType F(string name, string[] patterns) => new(name) { Patterns = patterns };

        var modern = new[]
        {
            "*.zip", "*.7z", "*.tar", "*.tar.gz", "*.tgz", "*.tar.bz2", "*.tbz2",
            "*.tar.xz", "*.txz", "*.tar.zst", "*.tzst", "*.gz", "*.bz2", "*.xz",
            "*.zst", "*.zstd", "*.br", "*.lz4", "*.lzma", "*.snappy"
        };
        var obsolete = new[] { "*.rar", "*.ace", "*.arj", "*.cab", "*.lzh", "*.lha" };
        var unusual = new[] { "*.iso", "*.exe", "*.msi", "*.msix", "*.appx", "*.dmg", "*.wim" };

        var filters = new List<FilePickerFileType>
        {
            F(T("dialog.filter.allArchives"), modern.Concat(obsolete).Concat(unusual).ToArray()),
            F(T("dialog.filter.modern"), modern),
            F(T("dialog.filter.obsolete"), obsolete),
            F(T("dialog.filter.unusual"), unusual),
            F(T("dialog.filter.zip"), new[] { "*.zip" }),
            F(T("dialog.filter.sevenZip"), new[] { "*.7z" }),
            F(T("dialog.filter.tar"), new[] { "*.tar", "*.tar.gz", "*.tgz", "*.tar.bz2", "*.tbz2", "*.tar.xz", "*.txz", "*.tar.zst", "*.tzst" }),
            F(T("dialog.filter.gzip"), new[] { "*.gz", "*.tgz" }),
            F(T("dialog.filter.bzip2"), new[] { "*.bz2", "*.tbz2" }),
            F(T("dialog.filter.xz"), new[] { "*.xz", "*.txz" }),
            F(T("dialog.filter.zstd"), new[] { "*.zst", "*.zstd", "*.tzst" }),
            F(T("dialog.filter.brotli"), new[] { "*.br" }),
            F(T("dialog.filter.lz4"), new[] { "*.lz4" }),
            F(T("dialog.filter.lzma"), new[] { "*.lzma" }),
            F(T("dialog.filter.snappy"), new[] { "*.snappy" }),
            F(T("dialog.filter.rar"), new[] { "*.rar" }),
            F(T("dialog.filter.ace"), new[] { "*.ace" }),
            F(T("dialog.filter.arj"), new[] { "*.arj" }),
            F(T("dialog.filter.cab"), new[] { "*.cab" }),
            F(T("dialog.filter.lzh"), new[] { "*.lzh", "*.lha" }),
            F(T("dialog.filter.iso"), new[] { "*.iso" }),
            F(T("dialog.filter.sfx"), new[] { "*.exe" }),
            F(T("dialog.filter.msi"), new[] { "*.msi" }),
            F(T("dialog.filter.msix"), new[] { "*.msix", "*.appx" }),
            F(T("dialog.filter.dmg"), new[] { "*.dmg" }),
            F(T("dialog.filter.wim"), new[] { "*.wim" }),
            F(T("dialog.filter.all"), new[] { "*" }),
        };
        return filters;
    }

    public async Task<string?> PickThemeAsync()
    {
        if (StorageProvider == null)
            return null;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Install WinRAR theme",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { ThemeFilter, AllFilesFilter },
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickDirectoryAsync(string title = "Select destination folder")
    {
        if (StorageProvider == null)
            return null;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(string title = "Select files",
                                                            bool allowMultiple = true)
    {
        if (StorageProvider == null)
            return Array.Empty<string>();

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = new List<FilePickerFileType> { AnyFilesFilter },
        });

        return files
            .Select(f => f.TryGetLocalPath())
            .Where(p => p != null)
            .Cast<string>()
            .ToList();
    }

    public async Task<string?> PickSaveArchiveAsync(string suggestedName)
    {
        if (StorageProvider == null)
            return null;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Create archive",
            SuggestedFileName = suggestedName,
            DefaultExtension = "zip",
            FileTypeChoices = new List<FilePickerFileType> { ArchivesFilter },
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickSaveCopyAsync(string suggestedName)
    {
        if (StorageProvider == null)
            return null;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save copy as",
            SuggestedFileName = suggestedName,
            DefaultExtension = "zip",
            FileTypeChoices = new List<FilePickerFileType> { ArchivesFilter },
        });

        return file?.TryGetLocalPath();
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        if (MainWindow == null)
            return;

        var vm = new InfoViewModel(title, message);
        var dialog = new InfoDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
    }

    /// <summary>
    /// Shows an extraction-complete dialog with a summary (extracted / skipped /
    /// overwritten), the list of extracted files and an "open folder" button that
    /// reveals the target in the OS file manager.
    /// </summary>
    public async Task ShowExtractCompleteAsync(string title, string summary, string folderPath, IReadOnlyList<ExtractedFile> files)
    {
        if (MainWindow == null)
            return;

        var vm = new ExtractCompleteViewModel(title, summary, folderPath, files);
        var dialog = new ExtractCompleteDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
    }

    public static void OpenInFileManager(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start("explorer.exe", $"\"{path}\"");
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", path);
            else
                Process.Start("xdg-open", path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open {Path} in file manager", path);
        }
    }

    public async Task ShowAboutAsync()
    {
        if (MainWindow == null)
            return;

        var dialog = new AboutDialog { DataContext = new AboutViewModel() };
        await ShowDialogAsync(dialog);
    }

    public async Task<string?> ShowPromptAsync(string title, string initial = "")
    {
        if (MainWindow == null)
            return null;

        var vm = new PromptViewModel(title, initial);
        var dialog = new PromptDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        return vm.Confirmed ? vm.Value : null;
    }

    public async Task<string?> ShowPasswordAsync(string title = null!)
    {
        if (MainWindow == null)
            return null;

        var vm = new PasswordViewModel { Title = title ?? LocalizationManager.T("dialog.password.title") };
        var dialog = new PasswordDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        return vm.Confirmed ? vm.Password : null;
    }

    public async Task<SettingsViewModel?> ShowSettingsAsync()
    {
        if (MainWindow == null)
            return null;

        var vm = new SettingsViewModel(_settingsService);
        var dialog = new SettingsDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        return vm;
    }

    public async Task<ConvertViewModel?> ShowConvertAsync(string sourceName)
    {
        if (MainWindow == null)
            return null;

        var vm = new ConvertViewModel { SourceName = sourceName };
        var dialog = new ConvertDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        return vm.Confirmed ? vm : null;
    }

    public async Task<SplitFileViewModel?> ShowSplitFileAsync(string sourcePath)
    {
        if (MainWindow == null)
            return null;

        var vm = new SplitFileViewModel { SourcePath = sourcePath };
        var dialog = new SplitFileDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        return vm.Confirmed ? vm : null;
    }

    public async Task<JoinFileViewModel?> ShowJoinFileAsync()
    {
        if (MainWindow == null)
            return null;

        var vm = new JoinFileViewModel();
        var dialog = new JoinFileDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        return vm.Confirmed ? vm : null;
    }

    public async Task<HashFileViewModel?> ShowHashFileAsync(string filePath)
    {
        if (MainWindow == null)
            return null;

        var vm = new HashFileViewModel { FilePath = filePath };
        var dialog = new HashFileDialog { DataContext = vm };
        await ShowDialogAsync(dialog);
        return vm.Confirmed ? vm : null;
    }

    private Task ShowDialogAsync(Window dialog) => ModalHost.ShowAsync(MainWindow!, dialog);

    public async Task<CreateWizardResult?> ShowCreateWizardAsync(string? defaultPath = null)
    {
        if (MainWindow == null)
            return null;
        return await WizardWindow.ShowAsync(MainWindow, new CreateWizardViewModel(defaultPath));
    }

    public async Task<ExtractWizardResult?> ShowExtractWizardAsync(string? defaultDestination = null, string? archiveFolderName = null)
    {
        if (MainWindow == null)
            return null;
        return await WizardWindow.ShowAsync(MainWindow, new ExtractWizardViewModel(defaultDestination, archiveFolderName));
    }

    public async Task<ConvertWizardResult?> ShowConvertWizardAsync(string? suggestedName = null)
    {
        if (MainWindow == null)
            return null;
        return await WizardWindow.ShowAsync(MainWindow, new ConvertWizardViewModel(suggestedName));
    }
}
