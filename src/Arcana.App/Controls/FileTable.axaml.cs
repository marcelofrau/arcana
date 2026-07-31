using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Arcana.App.ViewModels;

namespace Arcana.App.Controls;

public partial class FileTable : UserControl
{
    public FileTable()
    {
        InitializeComponent();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        Vm?.Archive.OpenItemCommand.Execute(null);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                Vm?.InfoCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                Vm?.Archive.OpenItemCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Back:
                Vm?.Archive.NavigateUpCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete:
                Vm?.Archive.DeleteCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F2:
                Vm?.RenameCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.A when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                Grid.SelectAll();
                e.Handled = true;
                break;
        }
    }
}
