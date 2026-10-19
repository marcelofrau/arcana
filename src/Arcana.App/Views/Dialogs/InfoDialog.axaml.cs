using Avalonia.Controls;
using Avalonia.Interactivity;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class InfoDialog : Window
{
    public InfoDialog()
    {
        InitializeComponent();
        Opened += (_, _) => ActionButton.IsVisible =
            DataContext is InfoViewModel vm && vm.ActionButtonText != null;
        Opened += (_, _) => SecondActionButton.IsVisible =
            DataContext is InfoViewModel vm && vm.SecondActionButtonText != null;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InfoViewModel { DismissAction: { } action }) action();
        Close();
    }

    private void OnActionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InfoViewModel { Action: { } action })
            action();
        Close();
    }

    private void OnSecondActionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InfoViewModel { SecondAction: { } action }) action();
        Close();
    }
}
