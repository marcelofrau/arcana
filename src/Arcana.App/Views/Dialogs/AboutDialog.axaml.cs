using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Arcana.App.ViewModels;

namespace Arcana.App.Views.Dialogs;

public partial class AboutDialog : Window
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<AboutDialog>();

    private readonly DispatcherTimer _ambientTimer;
    private readonly DispatcherTimer _creditsTimer;

    private readonly TranslateTransform _orbTopTransform;
    private readonly TranslateTransform _orbBottomTransform;
    private readonly TranslateTransform _creditsTransform;

    private double _phase;
    private double _creditsY;
    private double _textHeight;
    private double _viewHeight;
    private bool _creditsReady;

    private byte[]? _srcPixels;
    private byte[]? _dstPixels;
    private WriteableBitmap? _ripple;
    private int _rippleWidth;
    private int _rippleHeight;
    private double _rippleT;

    public AboutDialog()
    {
        InitializeComponent();

        _orbTopTransform = (TranslateTransform)OrbTop.RenderTransform!;
        _orbBottomTransform = (TranslateTransform)OrbBottom.RenderTransform!;
        _creditsTransform = (TranslateTransform)CreditsText.RenderTransform!;

        Opened += OnOpened;
        Closed += OnClosed;

        _ambientTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(33),
        };
        _ambientTimer.Tick += OnAmbientTick;

        _creditsTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(16),
        };
        _creditsTimer.Tick += OnCreditsTick;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Root.Opacity = 1;
        InitRipple();
        _ambientTimer.Start();
        _creditsTimer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _ambientTimer.Stop();
        _creditsTimer.Stop();
    }

    private void OnAmbientTick(object? sender, EventArgs e)
    {
        _phase += 0.045;

        _orbTopTransform.X = Math.Sin(_phase) * 20;
        _orbTopTransform.Y = Math.Cos(_phase * 0.8) * 14;
        _orbBottomTransform.X = Math.Cos(_phase * 0.7) * 16;
        _orbBottomTransform.Y = Math.Sin(_phase * 0.9) * 12;

        LogoGlow.Opacity = 0.4 + 0.35 * (0.5 + 0.5 * Math.Sin(_phase * 1.3));

        if (_ripple != null)
            UpdateRipple();
    }

    private void InitRipple()
    {
        if (DataContext is not AboutViewModel vm || vm.Logo is not { } logo || LogoImage == null)
            return;

        var (srcW, srcH) = (logo.PixelSize.Width, logo.PixelSize.Height);
        if (srcW <= 0 || srcH <= 0)
            return;

        var scale = Math.Min(1.0, 380.0 / srcW);
        _rippleWidth = Math.Max(2, (int)(srcW * scale));
        _rippleHeight = Math.Max(2, (int)(srcH * scale));
        var stride = _rippleWidth * 4;

        using var scaled = logo.CreateScaledBitmap(new PixelSize(_rippleWidth, _rippleHeight));
        _srcPixels = new byte[_rippleWidth * _rippleHeight * 4];
        var handle = GCHandle.Alloc(_srcPixels, GCHandleType.Pinned);
        try
        {
            scaled.CopyPixels(new PixelRect(0, 0, _rippleWidth, _rippleHeight),
                handle.AddrOfPinnedObject(), _srcPixels.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        _dstPixels = new byte[_rippleWidth * _rippleHeight * 4];
        _ripple = new WriteableBitmap(
            new PixelSize(_rippleWidth, _rippleHeight),
            new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        LogoImage.Source = _ripple;
        Log.Verbose("Water ripple initialized {W}x{H}", _rippleWidth, _rippleHeight);
    }

    private void UpdateRipple()
    {
        _rippleT += 0.05;
        var t = _rippleT;
        var (w, h) = (_rippleWidth, _rippleHeight);
        var stride = w * 4;

        Span<int> dx = stackalloc int[h];
        Span<int> dy = stackalloc int[w];
        for (var y = 0; y < h; y++)
            dx[y] = (int)(2.5 * Math.Sin(y * 0.045 + t)) + (int)(1.5 * Math.Sin(y * 0.013 - t * 0.7));
        for (var x = 0; x < w; x++)
            dy[x] = (int)(1.5 * Math.Sin(x * 0.035 + t * 0.9));

        var src = _srcPixels!;
        var dst = _dstPixels!;
        for (var y = 0; y < h; y++)
        {
            var rowS = y * stride;
            var rowD = rowS;
            var dxv = dx[y];
            for (var x = 0; x < w; x++)
            {
                var sx = Math.Clamp(x + dxv, 0, w - 1);
                var sy = Math.Clamp(y + dy[x], 0, h - 1);
                var si = sy * stride + sx * 4;
                var di = rowD + x * 4;
                dst[di] = src[si];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
                dst[di + 3] = src[si + 3];
            }
        }

        using (var fb = _ripple!.Lock())
        {
            for (var y = 0; y < h; y++)
                Marshal.Copy(_dstPixels!, y * stride, IntPtr.Add(fb.Address, y * fb.RowBytes), stride);
        }

        LogoImage.InvalidateVisual();
    }

    private void OnCreditsTick(object? sender, EventArgs e)
    {
        if (!_creditsReady)
        {
            // Measure at the box's real width so Wrap yields the true wrapped
            // height (infinite width would collapse _textHeight to one line and
            // the marquee would never scroll past the header into the story).
            var width = Math.Max(0, CreditsBox.Bounds.Width - 16);
            if (width <= 0)
                return;
            CreditsText.Measure(new Size(width, double.PositiveInfinity));
            _textHeight = CreditsText.DesiredSize.Height;
            _viewHeight = CreditsBox.Bounds.Height;
            if (_viewHeight <= 0 || _textHeight <= 0)
                return;

            _creditsReady = true;
            _creditsY = _viewHeight;
            _creditsTransform.Y = _creditsY;
            return;
        }

        _creditsY -= 1.1;
        if (_creditsY < -_textHeight)
            _creditsY = _viewHeight;

        _creditsTransform.Y = _creditsY;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
