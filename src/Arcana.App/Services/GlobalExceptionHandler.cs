using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Arcana.App.Views.Dialogs;
using Serilog;

namespace Arcana.App.Services;

/// <summary>
/// Global unhandled-exception handlers. Instead of letting the app die silently,
/// a modal error dialog shows the copyable exception and offers Restart / Continue / Close.
/// </summary>
public static class GlobalExceptionHandler
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GlobalExceptionHandler));

    public static void Attach()
    {
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandled;
        TaskScheduler.UnobservedTaskException += OnUnobservedTask;
        Dispatcher.UIThread.UnhandledException += OnDispatcherException;
        Log.Debug("Global exception handlers attached");
    }

    private static void OnDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new Exception("Unknown fatal error");
        Log.Fatal(ex, "Unhandled app-domain exception");
        if (Dispatcher.UIThread.CheckAccess())
        {
            _ = ShowAndApplyAsync(ex, exitOnClose: true);
        }
        else
        {
            var result = Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(ex)).GetAwaiter().GetResult();
            ApplyResult(result, exitOnClose: true);
        }
    }

    private static void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        Log.Error(e.Exception, "Unobserved task exception");
        if (Dispatcher.UIThread.CheckAccess())
        {
            _ = ShowAndApplyAsync(e.Exception, exitOnClose: false);
        }
        else
        {
            var result = Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(e.Exception)).GetAwaiter().GetResult();
            ApplyResult(result, exitOnClose: false);
        }
    }

    private static void OnDispatcherException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled dispatcher exception");
        e.Handled = true;
        _ = ShowAndApplyAsync(e.Exception, exitOnClose: false);
    }

    private static async Task ShowAndApplyAsync(Exception exception, bool exitOnClose)
    {
        var result = await ShowDialogAsync(exception);
        ApplyResult(result, exitOnClose);
    }

    private static async Task<ErrorDialogResult> ShowDialogAsync(Exception exception)
    {
        var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        var dialog = new ErrorDialog();
        dialog.SetDetails(exception.ToString());
        if (owner != null)
            return await dialog.ShowDialog<ErrorDialogResult>(owner);

        var tcs = new TaskCompletionSource<ErrorDialogResult>();
        dialog.Closed += (_, _) => tcs.TrySetResult(dialog.Result);
        dialog.Show();
        return await tcs.Task;
    }

    private static void ApplyResult(ErrorDialogResult result, bool exitOnClose)
    {
        switch (result)
        {
            case ErrorDialogResult.Restart:
                RestartApp();
                Environment.Exit(1);
                break;
            case ErrorDialogResult.Close:
                if (exitOnClose)
                    Environment.Exit(0);
                break;
        }
    }

    private static void RestartApp()
    {
        try
        {
            var exe = Environment.ProcessPath;
            var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(a => $"\"{a}\""));
            Process.Start(exe ?? "Arcana.App", args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restart application");
        }
    }
}
