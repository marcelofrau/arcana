using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Arcana.App.ViewModels.Wizard;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.Views.Wizards;

public partial class CreateDestinationStepView : UserControl
{
    public CreateDestinationStepView()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null || DataContext is not CreateDestinationStep vm)
            return;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = T("wizard.create.saveTitle"),
            SuggestedFileName = "New Archive.zip",
            DefaultExtension = ".zip",
        });
        if (file != null)
            vm.ArchivePath = file.TryGetLocalPath() ?? "";
    }
}
