using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Arcana.App.ViewModels.Wizard;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.Views.Wizards;

public partial class ConvertDestinationStepView : UserControl
{
    public ConvertDestinationStepView()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null || DataContext is not ConvertDestinationStep vm)
            return;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = T("wizard.convert.saveTitle"),
            SuggestedFileName = "archive.zip",
            DefaultExtension = ".zip",
        });
        if (file != null)
            vm.OutputPath = file.TryGetLocalPath() ?? "";
    }
}
