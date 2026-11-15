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
///
/// The x axis is a fixed <see cref="WindowSeconds"/> window ending at the newest sample, so
/// the line scrolls leftwards as the operation advances instead of being rescaled to the
/// whole elapsed time (which drew a full-width streak from the first sample).
/// </summary>
public class SpeedChart : Control
{
    public static readonly StyledProperty<IReadOnlyList<SpeedSample>> SamplesProperty =
        AvaloniaProperty.Register<SpeedChart, IReadOnlyList<SpeedSample>>(nameof(Samples), Array.Empty<SpeedSample>());

    /// <summary>Second series, drawn without the area fill (archive-side throughput).</summary>
    public static readonly StyledProperty<IReadOnlyList<SpeedSample>> ArchiveSamplesProperty =
        AvaloniaProperty.Register<SpeedChart, IReadOnlyList<SpeedSample>>(nameof(ArchiveSamples), Array.Empty<SpeedSample>());

    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<SpeedChart, IBrush>(nameof(LineBrush), new ImmutableSolidColorBrush(Colors.MediumSeaGreen));

    public static readonly StyledProperty<IBrush> ArchiveBrushProperty =
        AvaloniaProperty.Register<SpeedChart, IBrush>(nameof(ArchiveBrush), new ImmutableSolidColorBrush(Color.FromRgb(0x4E, 0xA1, 0xF0)));

    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<SpeedChart, IBrush>(nameof(TextBrush), new ImmutableSolidColorBrush(Colors.Silver));

    public static readonly StyledProperty<IBrush> GridBrushProperty =
        AvaloniaProperty.Register<SpeedChart, IBrush>(nameof(GridBrush), new ImmutableSolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)));

    /// <summary>Seconds of history shown. The newest sample sits at the right edge once the
    /// window is full; before that the line grows from the left.</summary>
    public static readonly StyledProperty<double> WindowSecondsProperty =
        AvaloniaProperty.Register<SpeedChart, double>(nameof(WindowSeconds), 60.0);

    private INotifyCollectionChanged? _subscribed;
    private INotifyCollectionChanged? _archiveSubscribed;

    public SpeedChart()
    {
        // Samples older than the visible window stay in the collection, so their x lands
        // left of the control. Without clipping they paint outside the chart bounds.
        ClipToBounds = true;
    }

    static SpeedChart()
    {
        AffectsRender<SpeedChart>(SamplesProperty, ArchiveSamplesProperty, LineBrushProperty,
            ArchiveBrushProperty, TextBrushProperty, GridBrushProperty, WindowSecondsProperty);
    }

    public IReadOnlyList<SpeedSample> Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public IReadOnlyList<SpeedSample> ArchiveSamples
    {
        get => GetValue(ArchiveSamplesProperty);
        set => SetValue(ArchiveSamplesProperty, value);
    }

    public IBrush LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public IBrush ArchiveBrush
    {
        get => GetValue(ArchiveBrushProperty);
        set => SetValue(ArchiveBrushProperty, value);
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

    public double WindowSeconds
    {
        get => GetValue(WindowSecondsProperty);
        set => SetValue(WindowSecondsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SamplesProperty)
        {
            _subscribed = Resubscribe(_subscribed, Samples, OnSamplesChanged);
            InvalidateVisual();
        }
        else if (change.Property == ArchiveSamplesProperty)
        {
            _archiveSubscribed = Resubscribe(_archiveSubscribed, ArchiveSamples, OnSamplesChanged);
            InvalidateVisual();
        }
    }

    private INotifyCollectionChanged? Resubscribe(INotifyCollectionChanged? previous,
        IReadOnlyList<SpeedSample> source, NotifyCollectionChangedEventHandler handler)
    {
        if (previous is not null)
            previous.CollectionChanged -= handler;
        var current = source as INotifyCollectionChanged;
        if (current is not null)
            current.CollectionChanged += handler;
        return current;
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
        var archive = ArchiveSamples;
        if (samples.Count == 0 && archive.Count == 0)
            return;

        // Scale across both series, with a 1 MB/s floor so tiny values stay visible.
        var maxSpeed = 1.0;
        var maxTime = 0.0;
        foreach (var s in samples)
        {
            if (s.MBps > maxSpeed) maxSpeed = s.MBps;
            if (s.Time > maxTime) maxTime = s.Time;
        }
        foreach (var s in archive)
        {
            if (s.MBps > maxSpeed) maxSpeed = s.MBps;
            if (s.Time > maxTime) maxTime = s.Time;
        }

        // Fixed-width window ending at the newest sample: the trace scrolls left as time
        // passes instead of being stretched over the whole elapsed time.
        var window = Math.Max(WindowSeconds, 1.0);
        var windowEnd = Math.Max(maxTime, window);
        var windowStart = windowEnd - window;

        double X(double t) => left + (t - windowStart) / window * w;
        double Y(double mbps) => bottom - (mbps / maxSpeed) * h;

        foreach (var g in new[] { 0.25, 0.5, 0.75 })
            dc.DrawLine(baselinePen, new Point(left, bottom - g * h), new Point(left + w, bottom - g * h));

        DrawSeries(dc, samples, X, Y, bottom, LineBrush, withFill: true);
        DrawSeries(dc, archive, X, Y, bottom, ArchiveBrush, withFill: false);

        // One floating label only: two would sit on top of each other. The header shows both
        // current values with a colour chip each.
        DrawLatestLabel(dc, samples, X, Y, left, top, w, LineBrush);
    }

    private static void DrawSeries(DrawingContext dc, IReadOnlyList<SpeedSample> samples,
        Func<double, double> x, Func<double, double> y, double bottom, IBrush brush, bool withFill)
    {
        if (samples.Count == 0)
            return;

        if (withFill)
        {
            var lineColor = (brush as ISolidColorBrush)?.Color ?? Colors.MediumSeaGreen;
            var fill = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            };
            fill.GradientStops.Add(new GradientStop(new Color((byte)(lineColor.A * 0.55), lineColor.R, lineColor.G, lineColor.B), 0));
            fill.GradientStops.Add(new GradientStop(new Color((byte)(lineColor.A * 0.05), lineColor.R, lineColor.G, lineColor.B), 1));

            var area = new StreamGeometry();
            using (var ctx = area.Open())
            {
                ctx.BeginFigure(new Point(x(samples[0].Time), bottom), true);
                foreach (var s in samples)
                    ctx.LineTo(new Point(x(s.Time), y(s.MBps)));
                ctx.LineTo(new Point(x(samples[^1].Time), bottom));
                ctx.EndFigure(true);
            }
            dc.DrawGeometry(fill, null, area);
        }

        var line = new StreamGeometry();
        using (var ctx = line.Open())
        {
            ctx.BeginFigure(new Point(x(samples[0].Time), y(samples[0].MBps)), false);
            for (var i = 1; i < samples.Count; i++)
                ctx.LineTo(new Point(x(samples[i].Time), y(samples[i].MBps)));
            ctx.EndFigure(false);
        }
        dc.DrawGeometry(null, new Pen(brush, withFill ? 1.5 : 1.2), line);

        var last = samples[^1];
        dc.DrawEllipse(brush, null, new Point(x(last.Time), y(last.MBps)), 2.5, 2.5);
    }

    private void DrawLatestLabel(DrawingContext dc, IReadOnlyList<SpeedSample> samples,
        Func<double, double> x, Func<double, double> y, double left, double top, double w, IBrush brush)
    {
        if (samples.Count == 0)
            return;

        var last = samples[^1];
        var p = new Point(x(last.Time), y(last.MBps));
        var label = new FormattedText(
            $"{last.MBps:0.#} MB/s",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            11,
            brush);
        var labelX = p.X + 8 > left + w - label.Width ? p.X - label.Width - 8 : p.X + 8;
        dc.DrawText(label, new Point(Math.Max(left, labelX), Math.Max(top, p.Y - 20)));
    }
}
