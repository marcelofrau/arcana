using Avalonia.Controls;
using Avalonia.Input.Platform;
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

    private void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(DetailsBox.Text))
            Clipboard?.SetTextAsync(DetailsBox.Text);
    }

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

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Result = ErrorDialogResult.Close;
        Close(Result);
    }
}
