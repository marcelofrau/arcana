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
/// a modal error dialog shows the copyable exception and offers Exit / Restart / Continue.
/// </summary>
public static class GlobalExceptionHandler
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GlobalExceptionHandler));

    private static int _dialogShowing;

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
            _ = ShowAndApplyAsync(ex);
        }
        else
        {
            var result = Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(ex)).GetAwaiter().GetResult();
            ApplyResult(result);
        }
    }

    private static void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        Log.Error(e.Exception, "Unobserved task exception");
        if (Dispatcher.UIThread.CheckAccess())
        {
            _ = ShowAndApplyAsync(e.Exception);
        }
        else
        {
            var result = Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(e.Exception)).GetAwaiter().GetResult();
            ApplyResult(result);
        }
    }

    private static void OnDispatcherException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled dispatcher exception");
        e.Handled = true;
        _ = ShowAndApplyAsync(e.Exception);
    }

    private static async Task ShowAndApplyAsync(Exception exception)
    {
        var result = await ShowDialogAsync(exception);
        ApplyResult(result);
    }

    private static async Task<ErrorDialogResult> ShowDialogAsync(Exception exception)
    {
        // Re-entrancy guard: if an error dialog is already up, don't stack another.
        if (Interlocked.Exchange(ref _dialogShowing, 1) != 0)
        {
            Log.Warning("Error dialog already showing; skipping {Type}", exception.GetType().Name);
            return ErrorDialogResult.Continue;
        }

        try
        {
            var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            var dialog = new ErrorDialog();
            dialog.SetDetails(exception.ToString());
            if (owner != null)
                return await ModalHost.ShowAsync<ErrorDialogResult>(owner, dialog);

            var tcs = new TaskCompletionSource<ErrorDialogResult>();
            dialog.Closed += (_, _) => tcs.TrySetResult(dialog.Result);
            dialog.Show();
            return await tcs.Task;
        }
        finally
        {
            Interlocked.Exchange(ref _dialogShowing, 0);
        }
    }

    private static void ApplyResult(ErrorDialogResult result)
    {
        switch (result)
        {
            case ErrorDialogResult.Restart:
                RestartApp();
                Environment.Exit(1);
                break;
            case ErrorDialogResult.Close:
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
