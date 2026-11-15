using Arcana.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Arcana.App.Views.Dialogs;

public partial class DeleteFilesConfirmDialog : Window
{
    public DeleteFilesConfirmDialog()
    {
        InitializeComponent();
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DeleteFilesConfirmViewModel vm)
        {
            vm.Confirm();
        }
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}