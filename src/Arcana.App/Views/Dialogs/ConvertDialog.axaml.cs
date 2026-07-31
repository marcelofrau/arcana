using Avalonia.Controls;
using Avalonia.Interactivity;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class ConvertDialog : Window
{
    public ConvertDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConvertViewModel vm)
            vm.Confirm();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
