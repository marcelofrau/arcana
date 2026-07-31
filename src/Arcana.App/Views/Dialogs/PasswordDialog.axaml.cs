using Avalonia.Controls;
using Avalonia.Interactivity;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class PasswordDialog : Window
{
    public PasswordDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordViewModel vm)
            vm.OkCommand.Execute(null);
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
