using Avalonia.Controls;
using Avalonia.Interactivity;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.Save();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
