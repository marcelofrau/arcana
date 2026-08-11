using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Arcana.App.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Arcana.App.Controls;

/// <summary>
/// Lightweight live chart that renders the compression ratio over time
/// (compressed size / original size, %, 100 = stored). Zero external deps —
/// draws directly with <see cref="DrawingContext"/>.
/// </summary>
public class RatioChart : Control
{
    public static readonly StyledProperty<IReadOnlyList<RatioSample>> SamplesProperty =
        AvaloniaProperty.Register<RatioChart, IReadOnlyList<RatioSample>>(nameof(Samples), Array.Empty<RatioSample>());

    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<RatioChart, IBrush>(nameof(LineBrush), new ImmutableSolidColorBrush(Colors.MediumSeaGreen));

    public static readonly StyledProperty<IBrush> FillBrushProperty =
        AvaloniaProperty.Register<RatioChart, IBrush>(nameof(FillBrush), new ImmutableSolidColorBrush(Color.FromArgb(0x66, 0x3C, 0xB3, 0x71)));

    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<RatioChart, IBrush>(nameof(TextBrush), new ImmutableSolidColorBrush(Colors.Silver));

    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<RatioChart, IBrush>(nameof(GridBrush), new ImmutableSolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)));

    private INotifyCollectionChanged? _subscribed;

    static RatioChart()
    {
        AffectsRender<RatioChart>(SamplesProperty, LineBrushProperty, FillBrushProperty, TextBrushProperty, GridBrushProperty);
    }

    public IReadOnlyList<RatioSample> Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public IBrush LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public IBrush FillBrush
    {
        get => GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public IBrush TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public IBrush GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SamplesProperty)
        {
            if (_subscribed is not null)
                _subscribed.CollectionChanged -= OnSamplesChanged;
            _subscribed = Samples as INotifyCollectionChanged;
            if (_subscribed is not null)
                _subscribed.CollectionChanged += OnSamplesChanged;
            InvalidateVisual();
        }
    }

    private void OnSamplesChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        const double padding = 6;
        var w = Math.Max(Bounds.Width - padding * 2, 10);
        var h = Math.Max(Bounds.Height - padding * 2, 10);
        var left = padding;
        var bottom = padding + h;

        // Baseline.
        var baselinePen = new Pen(GridBrush, 1);
        dc.DrawLine(baselinePen, new Point(left, bottom), new Point(left + w, bottom));

        var samples = Samples;
        if (samples.Count == 0)
            return;

        // Scale: max ratio of all samples (min 100 so a "100" floor is visible).
        var maxRatio = 100.0;
        foreach (var s in samples)
        {
            if (s.Ratio > maxRatio)
                maxRatio = s.Ratio;
        }
        maxRatio = Math.Max(maxRatio, 100);

        var maxTime = 1.0;
        foreach (var s in samples)
        {
            if (s.Time > maxTime)
                maxTime = s.Time;
        }

        double X(double t) => left + (t / maxTime) * w;
        double Y(double ratio) => bottom - (ratio / maxRatio) * h;

        // Grid lines at 50% and 100%.
        foreach (var g in new[] { 50.0, 100.0 })
        {
            if (g >= maxRatio) continue;
            dc.DrawLine(baselinePen, new Point(left, Y(g)), new Point(left + w, Y(g)));
        }

        // Filled area.
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(X(samples[0].Time), bottom), true);
            foreach (var s in samples)
                ctx.LineTo(new Point(X(s.Time), Y(s.Ratio)));
            ctx.LineTo(new Point(X(samples[^1].Time), bottom));
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(FillBrush, null, geo);

        // Line.
        var lineGeo = new StreamGeometry();
        using (var ctx = lineGeo.Open())
        {
            ctx.BeginFigure(new Point(X(samples[0].Time), Y(samples[0].Ratio)), false);
            foreach (var s in samples)
                ctx.LineTo(new Point(X(s.Time), Y(s.Ratio)));
            ctx.EndFigure(false);
        }
        dc.DrawGeometry(null, new Pen(LineBrush, 1.5), lineGeo);

        // Latest point + value label.
        var last = samples[^1];
        var p = new Point(X(last.Time), Y(last.Ratio));
        dc.DrawEllipse(new ImmutableSolidColorBrush(Colors.Transparent), new Pen(LineBrush, 2), p, 3, 3);
        dc.DrawEllipse(LineBrush, null, p, 2, 2);

        var label = new FormattedText(
            $"{last.Ratio:0}%",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            TextBrush);
        dc.DrawText(label, new Point(left, Math.Max(padding - 4, p.Y - 20)));
    }
}
