using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Arcana.App.Controls;

/// <summary>
/// WinACE-style 3D cylinder ("pot") chart. Shows how much of the original size
/// the compressed data still occupies: the pot is filled up to
/// <see cref="FilledRatio"/> (compressed / original) — a mostly empty pot means
/// the archive compressed well, a full pot means little or no savings.
/// </summary>
public class CylinderChart : Control
{
    public static readonly StyledProperty<double> FilledRatioProperty =
        AvaloniaProperty.Register<CylinderChart, double>(nameof(FilledRatio), 0.0);

    public static readonly StyledProperty<IBrush> FillBrushProperty =
        AvaloniaProperty.Register<CylinderChart, IBrush>(nameof(FillBrush), new ImmutableSolidColorBrush(Color.FromRgb(0x3C, 0xB3, 0x71)));

    public static readonly StyledProperty<IBrush> EmptyBrushProperty =
        AvaloniaProperty.Register<CylinderChart, IBrush>(nameof(EmptyBrush), new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0x00, 0x00, 0x00)));

    public static readonly StyledProperty<IBrush> RimBrushProperty =
        AvaloniaProperty.Register<CylinderChart, IBrush>(nameof(RimBrush), new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)));

    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<CylinderChart, IBrush>(nameof(TextBrush), new ImmutableSolidColorBrush(Colors.Silver));

    static CylinderChart()
    {
        AffectsRender<CylinderChart>(FilledRatioProperty, FillBrushProperty, EmptyBrushProperty, RimBrushProperty, TextBrushProperty);
    }

    public double FilledRatio
    {
        get => GetValue(FilledRatioProperty);
        set => SetValue(FilledRatioProperty, value);
    }

    public IBrush FillBrush
    {
        get => GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public IBrush EmptyBrush
    {
        get => GetValue(EmptyBrushProperty);
        set => SetValue(EmptyBrushProperty, value);
    }

    public IBrush RimBrush
    {
        get => GetValue(RimBrushProperty);
        set => SetValue(RimBrushProperty, value);
    }

    public IBrush TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        var fill = Math.Clamp(FilledRatio, 0, 1);
        var accent = FillBrush is ISolidColorBrush sc ? sc.Color : Color.FromRgb(0x3C, 0xB3, 0x71);

        double cx = Bounds.Width / 2;
        double rx = Math.Min(Math.Max(Bounds.Width - 10, 10) / 2, 78);
        double ry = Math.Max(6, rx * 0.30);
        double topY = 8;
        double bottomY = Bounds.Height - ry - 8;
        double height = Math.Max(bottomY - topY, 1);
        double fillBottom = bottomY;
        double fillTop = fill <= 0 ? topY : bottomY - height * fill;

        // Cylinder silhouette is a simple rect region (top ellipse upper half +
        // sides + bottom ellipse lower half all fit inside bounds).
        var bodyRect = new Rect(cx - rx, topY, rx * 2, bottomY - topY);
        using (dc.PushClip(bodyRect))
        {
            // Empty top volume (dark, translucent).
            var emptyRect = new Rect(cx - rx, topY, rx * 2, Math.Max(fillTop - topY, 0));
            if (emptyRect.Height > 0)
                dc.DrawRectangle(BuildEmptySideBrush(accent), null, emptyRect);

            // Filled bottom volume — the compressed content.
            var fillRect = new Rect(cx - rx, fillTop, rx * 2, Math.Max(fillBottom - fillTop, 0));
            if (fillRect.Height > 0)
                dc.DrawRectangle(BuildSideBrush(accent), null, fillRect);
        }

        // Gloss highlight on the left side of the body.
        using (dc.PushClip(bodyRect))
        {
            var gloss = new Rect(cx - rx + rx * 0.14, topY, rx * 0.34, Math.Max(bottomY - topY, 0));
            dc.DrawRectangle(new ImmutableSolidColorBrush(Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF)), null, gloss);
        }

        // Fill surface (horizontal ellipse at the level of the content) + separator.
        if (fill > 0 && fill < 1)
        {
            var surface = Lerp(accent, Colors.White, 0.35f);
            dc.DrawEllipse(new ImmutableSolidColorBrush(surface),
                new Pen(BuildSeparatorBrush(accent), 1.2), new Point(cx, fillTop), rx, ry);
            dc.DrawEllipse(null, new Pen(BuildSeparatorBrush(accent), 1.2), new Point(cx, fillTop), rx - 2, Math.Max(ry - 2, 1));
        }

        // Foot ellipse.
        dc.DrawEllipse(BuildFootBrush(accent), new Pen(RimBrush, 1), new Point(cx, bottomY), rx, ry);
        dc.DrawEllipse(BuildEmptySideBrush(accent), new Pen(BuildSeparatorBrush(accent), 1), new Point(cx, topY), rx, ry);

        // Percent label next to the fill level.
        var label = new FormattedText(
            $"{fill * 100:0.#}%",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            TextBrush);
        dc.DrawText(label, new Point(cx + rx + 6, Math.Max(0, fillTop - label.Height / 2)));
    }

    private static IBrush BuildSideBrush(Color accent)
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Shade(accent, 0.55f), 0),
                new GradientStop(Shade(accent, 0.95f), 0.35),
                new GradientStop(Shade(accent, 0.7f), 0.7),
                new GradientStop(Shade(accent, 0.38f), 1),
            },
        };
    }

    private static IBrush BuildEmptySideBrush(Color accent)
    {
        var c = Shade(accent, 0.30f);
        return new ImmutableSolidColorBrush(Color.FromArgb(150, c.R, c.G, c.B));
    }

    private static IBrush BuildSeparatorBrush(Color accent) => new ImmutableSolidColorBrush(Shade(accent, 0.42f));

    private static IBrush BuildFootBrush(Color accent)
    {
        var c = Shade(accent, 0.30f);
        return new ImmutableSolidColorBrush(Color.FromArgb(230, c.R, c.G, c.B));
    }

    private static Color Shade(Color c, float factor)
        => Color.FromArgb(c.A, (byte)Math.Min(255, c.R * factor), (byte)Math.Min(255, c.G * factor), (byte)Math.Min(255, c.B * factor));

    private static Color Lerp(Color a, Color b, float t)
        => Color.FromArgb(
            (byte)(a.A + (b.A - a.A) * t),
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
}
