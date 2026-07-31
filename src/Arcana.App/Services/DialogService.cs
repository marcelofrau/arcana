using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Arcana.App.ViewModels;
using Arcana.App.Views.Dialogs;

namespace Arcana.App.Services;

public sealed class DialogService
{
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
            Title = "Open archive",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { ArchivesFilter, AllFilesFilter },
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
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

    public async Task ShowInfoAsync(string title, string message)
    {
        if (MainWindow == null)
            return;

        var vm = new InfoViewModel(title, message);
        var dialog = new InfoDialog { DataContext = vm };
        await dialog.ShowDialog(MainWindow);
    }

    public async Task<string?> ShowPromptAsync(string title, string initial = "")
    {
        if (MainWindow == null)
            return null;

        var vm = new PromptViewModel(title, initial);
        var dialog = new PromptDialog { DataContext = vm };
        await dialog.ShowDialog(MainWindow);
        return vm.Confirmed ? vm.Value : null;
    }
}
