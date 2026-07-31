using Avalonia.Media;

namespace Arcana.App.Themes;

/// <summary>
/// Derives a complete semantic color theme from an arbitrary hex palette.
/// The mapping is deterministic so every palette in docs/palletes produces a
/// usable theme without hand-tuning:
///
///  - Background / Surface / SurfaceRaised  ← the darkest palette colors
///  - TextPrimary / TextSecondary           ← the lightest palette colors
///  - Border / Hover                        ← subtle lightening of the raised surface
///  - Accent                                ← most saturated mid-tone (max HSV saturation
///                                             among colors with luma in [0.18, 0.85])
///  - AccentHover                           ← accent blended toward text
///  - Success / Warning / Error             ← most saturated palette color in the
///                                             green / amber / red hue bands; falls back to
///                                             the Arcana defaults blended with the accent
///  - Selection / SelectionUnfocused        ← accent blended into the surface
/// </summary>
public static class PaletteThemeFactory
{
    public static ColorTheme Create(string id, string displayName, IReadOnlyList<Color> palette)
    {
        if (palette.Count == 0)
            throw new ArgumentException("Palette must contain at least one color.", nameof(palette));

        var ordered = palette.OrderBy(Luma).ToList();

        var background = ordered[0];
        var surface = At(ordered, 1, background);
        var surfaceRaised = At(ordered, 2, surface);
        var textPrimary = ordered[^1];
        var textSecondary = ordered.Count >= 2 ? ordered[^2] : Blend(textPrimary, background, 0.5);

        var accent = PickAccent(palette, background);
        var accentHover = Blend(accent, textPrimary, 0.18);

        var border = Blend(surfaceRaised, textPrimary, 0.12);
        var hover = Blend(surfaceRaised, textPrimary, 0.2);

        var success = PickHue(palette, accent, 80, 170, new Color(255, 0x34, 0xD3, 0x99));
        var warning = PickHue(palette, accent, 25, 75, new Color(255, 0xFB, 0xBF, 0x24));
        var error = PickHue(palette, accent, 320, 20, new Color(255, 0xF8, 0x71, 0x71));

        var selection = Blend(accent, surface, 0.35);
        var selectionUnfocused = Blend(accent, surface, 0.18);

        return new ColorTheme
        {
            Id = id,
            DisplayName = displayName,
            Background = background,
            Surface = surface,
            SurfaceRaised = surfaceRaised,
            Border = border,
            TextPrimary = textPrimary,
            TextSecondary = textSecondary,
            Accent = accent,
            AccentHover = accentHover,
            Success = success,
            Warning = warning,
            Error = error,
            Hover = hover,
            Selection = selection,
            SelectionUnfocused = selectionUnfocused,
        };
    }

    /// <summary>Most saturated color with a mid-range luma; the palette is sorted so
    /// the fallback (middle of the list) is a safe neutral pick.</summary>
    private static Color PickAccent(IReadOnlyList<Color> palette, Color background)
    {
        Color? best = null;
        var bestSat = -1.0;

        foreach (var c in palette)
        {
            var luma = Luma(c);
            if (luma < 0.18 || luma > 0.85)
                continue;
            var sat = Saturation(c);
            if (sat > bestSat)
            {
                bestSat = sat;
                best = c;
            }
        }

        if (best is { } accent && accent != background)
            return accent;

        var ordered = palette.OrderBy(Luma).ToList();
        var fallback = ordered[Math.Clamp(ordered.Count / 2, 0, ordered.Count - 1)];
        return fallback == background ? Blend(fallback, new Color(255, 0xFF, 0xFF, 0xFF), 0.5) : fallback;
    }

    /// <summary>Most saturated palette color whose hue falls inside [start, end]
    /// (wrap-around allowed, e.g. red = 320..20), excluding the accent. Falls back
    /// to the Arcana default blended with the accent when the palette has no match.</summary>
    private static Color PickHue(IReadOnlyList<Color> palette, Color accent, double start, double end, Color fallback)
    {
        Color? best = null;
        var bestSat = -1.0;

        foreach (var c in palette)
        {
            if (c == accent)
                continue;
            var hue = Hue(c);
            var sat = Saturation(c);
            if (sat <= 0.25 || !InHueBand(hue, start, end))
                continue;
            if (sat > bestSat)
            {
                bestSat = sat;
                best = c;
            }
        }

        return best ?? Blend(fallback, accent, 0.4);
    }

    private static bool InHueBand(double hue, double start, double end)
    {
        if (hue < 0)
            return false;
        return start <= end ? hue >= start && hue <= end : hue >= start || hue <= end;
    }

    private static Color At(IReadOnlyList<Color> list, int index, Color fallback)
        => index < list.Count ? list[index] : fallback;

    private static double Luma(Color c)
        => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;

    private static double Saturation(Color c)
    {
        double max = Math.Max(c.R, Math.Max(c.G, c.B));
        double min = Math.Min(c.R, Math.Min(c.G, c.B));
        return max == 0 ? 0 : (max - min) / max;
    }

    private static double Hue(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        if (delta == 0)
            return -1;

        double h;
        if (max == r)
            h = (g - b) / delta % 6;
        else if (max == g)
            h = (b - r) / delta + 2;
        else
            h = (r - g) / delta + 4;

        h *= 60;
        return h < 0 ? h + 360 : h;
    }

    private static Color Blend(Color a, Color b, double t)
    {
        return new Color(
            (byte)Math.Round(a.A + (b.A - a.A) * t),
            (byte)Math.Round(a.R + (b.R - a.R) * t),
            (byte)Math.Round(a.G + (b.G - a.G) * t),
            (byte)Math.Round(a.B + (b.B - a.B) * t));
    }
}
