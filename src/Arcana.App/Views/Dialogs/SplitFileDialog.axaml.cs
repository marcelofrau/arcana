using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class SplitFileDialog : Window
{
    public SplitFileDialog()
    {
        InitializeComponent();
    }

    private async void OnBrowseDestClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null || DataContext is not SplitFileViewModel vm)
            return;

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select destination folder",
            AllowMultiple = false,
        });

        if (folders.Count > 0)
            vm.DestinationDir = folders[0].TryGetLocalPath() ?? "";
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SplitFileViewModel vm && vm.DestinationDir.Length > 0)
            vm.Confirm();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
