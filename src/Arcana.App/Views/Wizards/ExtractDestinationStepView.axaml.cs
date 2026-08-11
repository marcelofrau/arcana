using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Arcana.App.ViewModels.Wizard;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.Views.Wizards;

public partial class ExtractDestinationStepView : UserControl
{
    public ExtractDestinationStepView()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null || DataContext is not ExtractDestinationStep vm)
            return;

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = T("wizard.extract.folderTitle"),
            AllowMultiple = false,
        });
        if (folders.Count > 0)
            vm.DestinationDir = folders[0].TryGetLocalPath() ?? "";
    }
}
