using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Serilog;

namespace Arcana.App.Services;

/// <summary>
/// Adds a soft fade-in on open and fade-out on close to any window or dialog.
/// The window starts fully transparent and animates <see cref="Window.Opacity"/>
/// to 1 once shown; closing is deferred until it has faded back to 0, then the
/// window really closes. Fade-out guards against double-close during the animation.
/// </summary>
public static class WindowFade
{
    private const double InMs = 220;
    private const double OutMs = 170;
    private const int FrameMs = 16;

    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(WindowFade));
    private static readonly ConditionalWeakTable<Window, object> Attached = new();
    private static readonly ConditionalWeakTable<Window, object> FadingOut = new();

    public static void Attach(Window window)
    {
        if (window is null || Attached.TryGetValue(window, out _))
            return;

        Attached.Add(window, new object());
        window.Opacity = 0;
        window.Opened += (_, _) => _ = FadeToAsync(window, 1, InMs);
        window.Closing += OnClosing;
        Log.Debug("Fade attached to {Window}", window.Title);
    }

    private static void OnClosing(object? sender, CancelEventArgs e)
    {
        if (sender is not Window window)
            return;

        // A second Closing while the fade-out runs means the real close is allowed.
        if (FadingOut.TryGetValue(window, out _))
            return;

        FadingOut.Add(window, new object());
        e.Cancel = true;
        _ = FadeAndCloseAsync(window);
    }

    private static async Task FadeAndCloseAsync(Window window)
    {
        await FadeToAsync(window, 0, OutMs);
        try
        {
            window.Close();
        }
        finally
        {
            FadingOut.Remove(window);
        }
    }

    private static async Task FadeToAsync(Window window, double target, double ms)
    {
        var start = window.Opacity;
        var steps = Math.Max(1, (int)(ms / FrameMs));
        for (var i = 1; i <= steps; i++)
        {
            window.Opacity = start + (target - start) * i / steps;
            await Task.Delay(FrameMs);
        }

        window.Opacity = target;
    }
}
