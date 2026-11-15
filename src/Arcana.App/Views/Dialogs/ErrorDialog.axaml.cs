using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Arcana.Core.Logging;
using Serilog;

namespace Arcana.App.Views.Dialogs;

public enum ErrorDialogResult
{
    Restart,
    Continue,
    Close,
}

public partial class ErrorDialog : Window
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ErrorDialog>();

    public ErrorDialog()
    {
        InitializeComponent();
    }

    public ErrorDialogResult Result { get; private set; } = ErrorDialogResult.Close;

    public void SetDetails(string text)
    {
        if (ErrorDetailsBox != null)
            ErrorDetailsBox.Text = text;
    }

    public void SetLogDetails(string text)
    {
        if (LogDetailsBox != null)
            LogDetailsBox.Text = text;
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

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Result = ErrorDialogResult.Close;
        Close(Result);
    }

    private async void OnCopyErrorClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var text = $"{Title}\n\nError\n\n--- Error Details ---\n{ErrorDetailsBox?.Text ?? ""}";
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
                await topLevel.Clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to copy error to clipboard");
        }
    }

    private async void OnCopyLogsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var text = $"{Title}\n\nError\n\n--- Recent Logs ---\n{LogDetailsBox?.Text ?? ""}";
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
                await topLevel.Clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to copy logs to clipboard");
        }
    }

    private void OnOpenLogFolderClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var logDir = LogConfig.LogDirectory;
            if (Directory.Exists(logDir))
            {
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logDir}\"") { UseShellExecute = true });
                else if (OperatingSystem.IsMacOS())
                    Process.Start("open", logDir);
                else
                    Process.Start("xdg-open", logDir);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open log folder");
        }
    }
}
