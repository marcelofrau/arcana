using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Arcana.App.Views.Dialogs;

public enum ErrorDialogResult
{
    Restart,
    Continue,
    Close,
}

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    public ErrorDialogResult Result { get; private set; } = ErrorDialogResult.Close;

    public void SetDetails(string text) => DetailsBox.Text = text;

    private void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        Result = ErrorDialogResult.Restart;
        Close(Result);
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        Result = ErrorDialogResult.Continue;
        Close(Result);
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Result = ErrorDialogResult.Close;
        Close(Result);
    }
}
