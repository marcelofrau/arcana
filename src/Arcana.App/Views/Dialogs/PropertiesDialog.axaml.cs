using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Arcana.App.Views.Dialogs;

public partial class PropertiesDialog : Window
{
    public PropertiesDialog()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
