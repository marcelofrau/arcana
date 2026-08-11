using Avalonia.Controls;
using Avalonia.Interactivity;
using Arcana.App.Services;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class ExtractCompleteDialog : Window
{
    public ExtractCompleteDialog()
    {
        InitializeComponent();
    }

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ExtractCompleteViewModel vm)
            DialogService.OpenInFileManager(vm.FolderPath);
        Close();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
