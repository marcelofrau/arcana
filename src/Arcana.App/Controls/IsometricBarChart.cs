using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Arcana.App.Controls;

/// <summary>
/// Isometric 3D bar ("prism") chart. The bar's full height represents the
/// uncompressed (original) size; the colored bottom portion shows the compressed
/// data, and the translucent top portion is the space saved by compression. The
/// <see cref="TextBrush"/> readout below the bar shows the compression percentage
/// (savings), i.e. how much smaller the archive is than the original bytes.
/// </summary>
public class IsometricBarChart : Control
{
    public static readonly StyledProperty<double> FilledRatioProperty =
        AvaloniaProperty.Register<IsometricBarChart, double>(nameof(FilledRatio), 0.0);

    public static readonly StyledProperty<IBrush> FillBrushProperty =
        AvaloniaProperty.Register<IsometricBarChart, IBrush>(nameof(FillBrush), new ImmutableSolidColorBrush(Color.FromRgb(0x3C, 0xB3, 0x71)));

    public static readonly StyledProperty<IBrush> EmptyBrushProperty =
        AvaloniaProperty.Register<IsometricBarChart, IBrush>(nameof(EmptyBrush), new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0x00, 0x00, 0x00)));

    public static readonly StyledProperty<IBrush> RimBrushProperty =
        AvaloniaProperty.Register<IsometricBarChart, IBrush>(nameof(RimBrush), new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)));

    public static readonly StyledProperty<IBrush> TextBrushProperty =
        AvaloniaProperty.Register<IsometricBarChart, IBrush>(nameof(TextBrush), new ImmutableSolidColorBrush(Colors.Silver));

    static IsometricBarChart()
    {
        AffectsRender<IsometricBarChart>(FilledRatioProperty, FillBrushProperty, EmptyBrushProperty, RimBrushProperty, TextBrushProperty);
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
        var empty = EmptyBrush is ISolidColorBrush ec ? ec.Color : accent;
        var rim = RimBrush is ISolidColorBrush rc ? rc.Color : Color.FromRgb(0x2A, 0x2A, 0x2A);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 40)
            return;

        double d = Math.Min(26, w * 0.22);          // isometric depth (horizontal)
        double dy = d * 0.5;                        // isometric depth (vertical)
        double frontW = Math.Min(84, Math.Max(40, w - d - 14));
        double topMargin = 18;
        double bottomMargin = 34;                   // readout zone
        double frontH = Math.Max(40, h - topMargin - bottomMargin);
        double bottomY = h - bottomMargin;
        double topY = bottomY - frontH;
        double x = (w - frontW - d) / 2.0;          // front-left edge

        double fillH = frontH * fill;
        double ytop = bottomY - fillH;              // fill level

        var outline = new Pen(new ImmutableSolidColorBrush(WithAlpha(rim, 0x80)), 1);

        // Ground shadow under the bar.
        Polygon(dc, new ImmutableSolidColorBrush(Color.FromArgb(0x2E, 0, 0, 0)), null,
            new Point(x, bottomY + 5), new Point(x + frontW, bottomY + 5),
            new Point(x + frontW + d, bottomY + 5 - dy), new Point(x + d, bottomY + 5 - dy));

        // --- Free (uncompressed / saved) volume, drawn as a translucent ghost ---
        var freeFront = new ImmutableSolidColorBrush(WithAlpha(empty, 0x3C));
        var freeRight = new ImmutableSolidColorBrush(WithAlpha(Shade(empty, 0.88f), 0x44));
        var freeTop = new ImmutableSolidColorBrush(WithAlpha(empty, 0x2C));

        var freeH = Math.Max(0, topY - ytop);
        if (freeH > 0.5)
        {
            // Front face of the free part.
            dc.DrawRectangle(freeFront, null, new Rect(x, ytop, frontW, freeH));
            // Right face.
            Polygon(dc, freeRight, null,
                new Point(x + frontW, ytop), new Point(x + frontW + d, ytop - dy),
                new Point(x + frontW + d, topY - dy), new Point(x + frontW, topY));
        }
        else
        {
            // Fill nearly fills the bar — still shade the top surface.
            dc.DrawRectangle(freeFront, null, new Rect(x, topY, frontW, 0.5));
        }

        // Top diamond of the whole bar (the uncompressed extent).
        Polygon(dc, freeTop, outline,
            new Point(x, topY), new Point(x + frontW, topY),
            new Point(x + frontW + d, topY - dy), new Point(x + d, topY - dy));

        // Quarter guides on the front + right faces.
        var guidePen = new Pen(new ImmutableSolidColorBrush(WithAlpha(rim, 0x44)), 1);
        foreach (var q in new[] { 0.25, 0.5, 0.75 })
        {
            double gy = bottomY - frontH * q;
            dc.DrawLine(guidePen, new Point(x, gy), new Point(x + frontW, gy));
            dc.DrawLine(guidePen, new Point(x + frontW, gy), new Point(x + frontW + d, gy - dy));
        }

        // Bar outline (front face).
        dc.DrawRectangle(null, outline, new Rect(x, topY, frontW, frontH));
        dc.DrawLine(outline, new Point(x + frontW, topY), new Point(x + frontW + d, topY - dy));
        dc.DrawLine(outline, new Point(x, topY), new Point(x + d, topY - dy));

        // --- Filled (compressed) prism ---
        if (fillH > 0.5)
        {
            // Right face — darker accent.
            Polygon(dc, new ImmutableSolidColorBrush(Shade(accent, 0.62f)), null,
                new Point(x + frontW, bottomY), new Point(x + frontW + d, bottomY - dy),
                new Point(x + frontW + d, ytop - dy), new Point(x + frontW, ytop));
            // Front face — horizontal accent gradient (light left, deep right).
            dc.DrawRectangle(BuildFrontBrush(accent), null, new Rect(x, ytop, frontW, fillH));
            // Top face (the fill level surface) — brightest accent.
            Polygon(dc, new ImmutableSolidColorBrush(Lerp(accent, Colors.White, 0.5f)), null,
                new Point(x, ytop), new Point(x + frontW, ytop),
                new Point(x + frontW + d, ytop - dy), new Point(x + d, ytop - dy));

            // Crisp level edge.
            var levelPen = new Pen(new ImmutableSolidColorBrush(Lerp(accent, Colors.White, 0.62f)), 1.4);
            dc.DrawLine(levelPen, new Point(x, ytop), new Point(x + frontW, ytop));
            dc.DrawLine(levelPen, new Point(x + frontW, ytop), new Point(x + frontW + d, ytop - dy));

            // Gloss stripe on the front face.
            var gloss = new Rect(x + frontW * 0.16, ytop, frontW * 0.14, fillH);
            dc.DrawRectangle(new ImmutableSolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF)), null, gloss);

            // Bottom baseline.
            dc.DrawLine(new Pen(new ImmutableSolidColorBrush(Shade(accent, 0.8f)), 1),
                new Point(x, bottomY), new Point(x + frontW, bottomY));
        }

        // Right face boundary (rear-bottom, rear-top, front-top, front-bottom).
        // Stroked last so the two edges that no other stroke covers — the rear
        // vertical and the bottom-right — stay visible over the translucent ghost.
        Polygon(dc, null, outline,
            new Point(x + frontW + d, bottomY - dy), new Point(x + frontW + d, topY - dy),
            new Point(x + frontW, topY), new Point(x + frontW, bottomY));

        // --- Readout: compression percentage (savings) below the bar ---
        var savings = 1 - fill;
        var label = new FormattedText(
            $"{savings * 100:0.#}%",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            15,
            TextBrush);
        double lx = x + (frontW + d) / 2.0 - label.Width / 2.0;
        double ly = bottomY + 6;
        var chip = new RoundedRect(new Rect(lx - 8, ly - 3, label.Width + 16, label.Height + 6), 7);
        dc.DrawRectangle(new ImmutableSolidColorBrush(Color.FromArgb(0x78, 0, 0, 0)), null, chip);
        dc.DrawText(label, new Point(lx, ly));
    }

    private static IBrush BuildFrontBrush(Color accent)
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Lerp(accent, Colors.White, 0.28f), 0),
                new GradientStop(accent, 0.5),
                new GradientStop(Shade(accent, 0.66f), 1),
            },
        };
    }

    private static void Polygon(DrawingContext dc, IBrush? fill, IPen? pen, params Point[] points)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], true);
            for (var i = 1; i < points.Length; i++)
                ctx.LineTo(points[i]);
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(fill, pen, geometry);
    }

    private static Color Shade(Color c, float factor)
        => Color.FromArgb(c.A, (byte)Math.Min(255, c.R * factor), (byte)Math.Min(255, c.G * factor), (byte)Math.Min(255, c.B * factor));

    private static Color Lerp(Color a, Color b, float t)
        => Color.FromArgb(
            (byte)(a.A + (b.A - a.A) * t),
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));

    private static Color WithAlpha(Color color, byte alpha)
        => new(alpha, color.R, color.G, color.B);
}
