using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Arcana.App.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Arcana.App.Controls;

/// <summary>
/// Lightweight live chart that renders transfer speed (MB/s) over time, DownThemAll-style:
/// a filled area with a gradient fade under the value line, horizontal gridlines and a
/// floating label for the latest value. Zero external deps — draws with
/// <see cref="DrawingContext"/>.
/// </summary>
public class SpeedChart : Control
{
    public static readonly StyledProperty<IReadOnlyList<SpeedSample>> SamplesProperty =
        AvaloniaProperty.Register<SpeedChart, IReadOnlyList<SpeedSample>>(nameof(Samples), Array.Empty<SpeedSample>());

    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<SpeedChart, IBrush>(nameof(LineBrush), new ImmutableSolidColorBrush(Colors.MediumSeaGreen));

    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<SpeedChart, IBrush>(nameof(TextBrush), new ImmutableSolidColorBrush(Colors.Silver));

    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<SpeedChart, IBrush>(nameof(GridBrush), new ImmutableSolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)));

    private INotifyCollectionChanged? _subscribed;

    static SpeedChart()
    {
        AffectsRender<SpeedChart>(SamplesProperty, LineBrushProperty, TextBrushProperty, GridBrushProperty);
    }

    public IReadOnlyList<SpeedSample> Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public IBrush LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
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

        const double left = 6;
        const double right = 6;
        const double top = 6;
        var w = Math.Max(Bounds.Width - left - right, 10);
        var h = Math.Max(Bounds.Height - top, 10);
        var bottom = top + h;

        var baselinePen = new Pen(GridBrush, 1);
        dc.DrawLine(baselinePen, new Point(left, bottom), new Point(left + w, bottom));

        var samples = Samples;
        if (samples.Count == 0)
            return;

        // Scale: 0..max sample speed (min 1 MB/s floor so tiny values stay visible).
        var maxSpeed = 1.0;
        foreach (var s in samples)
        {
            if (s.MBps > maxSpeed)
                maxSpeed = s.MBps;
        }

        var maxTime = 1.0;
        foreach (var s in samples)
        {
            if (s.Time > maxTime)
                maxTime = s.Time;
        }

        double X(double t) => left + (t / maxTime) * w;
        double Y(double mbps) => bottom - (mbps / maxSpeed) * h;

        // Gridlines at 25/50/75 % of the max speed.
        foreach (var g in new[] { 0.25, 0.5, 0.75 })
            dc.DrawLine(baselinePen, new Point(left, bottom - g * h), new Point(left + w, bottom - g * h));

        // Gradient fill under the line.
        var lineColor = (LineBrush as ISolidColorBrush)?.Color ?? Colors.MediumSeaGreen;
        var fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        fill.GradientStops.Add(new GradientStop(new Color((byte)(lineColor.A * 0.55), lineColor.R, lineColor.G, lineColor.B), 0));
        fill.GradientStops.Add(new GradientStop(new Color((byte)(lineColor.A * 0.05), lineColor.R, lineColor.G, lineColor.B), 1));

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(X(samples[0].Time), bottom), true);
            foreach (var s in samples)
                ctx.LineTo(new Point(X(s.Time), Y(s.MBps)));
            ctx.LineTo(new Point(X(samples[^1].Time), bottom));
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(fill, null, geo);

        // Line.
        var lineGeo = new StreamGeometry();
        using (var ctx = lineGeo.Open())
        {
            ctx.BeginFigure(new Point(X(samples[0].Time), Y(samples[0].MBps)), false);
            foreach (var s in samples)
                ctx.LineTo(new Point(X(s.Time), Y(s.MBps)));
            ctx.EndFigure(false);
        }
        dc.DrawGeometry(null, new Pen(LineBrush, 1.5), lineGeo);

        // Latest point + floating value label.
        var last = samples[^1];
        var p = new Point(X(last.Time), Y(last.MBps));
        dc.DrawEllipse(LineBrush, null, p, 2.5, 2.5);

        var label = new FormattedText(
            $"{last.MBps:0.#} MB/s",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            11,
            TextBrush);
        var labelX = p.X + 8 > left + w - label.Width ? p.X - label.Width - 8 : p.X + 8;
        dc.DrawText(label, new Point(Math.Max(left, labelX), Math.Max(top, p.Y - 20)));
    }
}
