using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Arcana.App.ViewModels.Wizard;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.Views.Wizards;

public partial class CreateFilesStepView : UserControl
{
    public CreateFilesStepView()
    {
        InitializeComponent();
    }

    private async void OnAddFilesClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null || DataContext is not CreateFilesStep vm)
            return;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = T("wizard.create.addFilesTitle"),
            AllowMultiple = true,
        });

        var paths = new List<string>();
        foreach (var file in files)
        {
            var local = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(local))
                paths.Add(local);
        }
        vm.AddFiles(paths);
    }

    private async void OnAddFolderClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage || DataContext is not CreateFilesStep vm)
            return;
        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = true, Title = T("msg.addFolder") });
        foreach (var folder in folders)
            if (folder.TryGetLocalPath() is { } path) vm.AddFiles([path]);
    }
}
