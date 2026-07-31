using Avalonia.Controls;
using Avalonia.Interactivity;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class HashFileDialog : Window
{
    public HashFileDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HashFileViewModel vm)
            vm.Confirm();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
