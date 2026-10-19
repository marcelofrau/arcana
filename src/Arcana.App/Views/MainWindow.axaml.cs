using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Threading;
using Arcana.App.Services;
using Arcana.App.ViewModels;
using Serilog;
using System.Threading.Tasks;

namespace Arcana.App.Views;

public partial class MainWindow : Window
{
    private static readonly ILogger Log = Serilog.Log.ForContext<MainWindow>();
    private bool _closeApproved;
    private bool _closingPrompt;
    private readonly DispatcherTimer _magicTimer;

    private double _phase;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        WindowFade.Attach(this);

        _magicTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(33),
        };
        _magicTimer.Tick += OnMagicTick;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IconThemes.Changed -= OnThemeChanged;
            vm.IconThemes.Changed += OnThemeChanged;
            ApplyThemeIcon();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Root.Opacity = 1;
        _magicTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _magicTimer.Stop();
        if (DataContext is MainViewModel vm) vm.ReleaseResources();
        base.OnClosed(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeApproved || DataContext is not MainViewModel vm) return;
        e.Cancel = true;
        if (!_closingPrompt) _ = RequestCloseAsync(vm);
    }

    private async Task RequestCloseAsync(MainViewModel vm)
    {
        _closingPrompt = true;
        try
        {
            if (!await vm.ConfirmCloseAsync()) return;
            _closeApproved = true;
            Close();
        }
        catch (Exception ex) { Log.Error(ex, "Could not finish closing the archive"); }
        finally { _closingPrompt = false; }
    }

    private void OnMagicTick(object? sender, EventArgs e)
    {
        _phase += 0.06;

        if (MagicDot != null)
            MagicDot.Opacity = 0.45 + 0.4 * (0.5 + 0.5 * Math.Sin(_phase * 1.5));

        if (OpenOverlayChip != null && OpenOverlayChip.IsVisible)
            OpenOverlayChip.Opacity = 0.7 + 0.3 * (0.5 + 0.5 * Math.Sin(_phase * 1.2));
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyThemeIcon();
    }

    private void ApplyThemeIcon()
    {
        if (DataContext is not MainViewModel vm)
            return;

        var path = vm.IconThemes.CurrentWindowIconPath;
        if (path == null || !File.Exists(path))
            return;

        try
        {
            using var stream = File.OpenRead(path);
            Icon = new WindowIcon(stream);
        }
        catch (Exception)
        {
            // keep the default icon when a theme icon cannot be decoded
        }
    }
}
