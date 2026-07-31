using System.IO;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Styling;

namespace Arcana.App.Themes;

/// <summary>
/// Registry of every built-in color theme. Themes come from three sources:
///
///  1. "Arcana Mystic" — the original hand-tuned palette (hardcoded below).
///  2. docs/palletes/*.hex — palette files embedded as resources and turned into
///     themes by <see cref="PaletteThemeFactory"/>; dropping a new .hex file into
///     docs/palletes and rebuilding adds a new theme automatically.
///  3. Hand-crafted retro themes (Windows 2000, Windows XP, BeOS) — light themes
///     whose colors mirror the classic OS look.
///
/// The default theme is "brewerviridis".
/// </summary>
public static class ColorThemeCatalog
{
    public const string DefaultId = "brewerviridis";
    public const string ArcanaMysticId = "arcanamystic";

    /// <summary>Order + display name per palette file stem (docs/palletes/*.hex).</summary>
    private static readonly (string Id, string DisplayName)[] PaletteEntries =
    {
        ("akc12", "akc12"),
        ("aquaverse", "AquaVerse"),
        ("blk-36", "blk-36"),
        ("brewerviridis", "brewerViridis"),
        ("neon-space", "Neon Space"),
        ("shido-cyberneon", "Shido CyberNeon"),
        ("slimy-05", "slimy-05"),
        ("soapy-10", "soapy-10"),
    };

    public static ColorTheme ArcanaMystic { get; } = new()
    {
        Id = ArcanaMysticId,
        DisplayName = "Arcana Mystic",
        Background = Color.Parse("#16161E"),
        Surface = Color.Parse("#1E1E28"),
        SurfaceRaised = Color.Parse("#262631"),
        Border = Color.Parse("#33334A"),
        TextPrimary = Color.Parse("#E4E4EE"),
        TextSecondary = Color.Parse("#9A9AB0"),
        Accent = Color.Parse("#8B5CF6"),
        AccentHover = Color.Parse("#9F7AFF"),
        Success = Color.Parse("#34D399"),
        Warning = Color.Parse("#FBBF24"),
        Error = Color.Parse("#F87171"),
        Hover = Color.Parse("#2A2A3A"),
        Selection = Color.Parse("#3A2E6E"),
        SelectionUnfocused = Color.Parse("#2C2448"),
    };

    public static ColorTheme Windows2000 { get; } = new()
    {
        Id = "windows2000",
        DisplayName = "Windows 2000",
        Variant = ThemeVariant.Light,
        Background = Color.Parse("#D4D0C8"),
        Surface = Color.Parse("#ECE9D8"),
        SurfaceRaised = Color.Parse("#F0EDE4"),
        Border = Color.Parse("#808080"),
        TextPrimary = Color.Parse("#000000"),
        TextSecondary = Color.Parse("#404040"),
        Accent = Color.Parse("#000080"),
        AccentHover = Color.Parse("#1084D0"),
        Success = Color.Parse("#007000"),
        Warning = Color.Parse("#C06000"),
        Error = Color.Parse("#C00000"),
        Hover = Color.Parse("#C8C4BC"),
        Selection = Color.Parse("#C8D4F0"),
        SelectionUnfocused = Color.Parse("#D6DEE8"),
    };

    public static ColorTheme WindowsXp { get; } = new()
    {
        Id = "windowsxp",
        DisplayName = "Windows XP",
        Variant = ThemeVariant.Light,
        Background = Color.Parse("#ECE9D8"),
        Surface = Color.Parse("#F5F4EC"),
        SurfaceRaised = Color.Parse("#FFFFFF"),
        Border = Color.Parse("#7BA2E7"),
        TextPrimary = Color.Parse("#000000"),
        TextSecondary = Color.Parse("#404040"),
        Accent = Color.Parse("#3D67B8"),
        AccentHover = Color.Parse("#1F6BE5"),
        Success = Color.Parse("#339900"),
        Warning = Color.Parse("#F3B21B"),
        Error = Color.Parse("#CC3300"),
        Hover = Color.Parse("#D6E4F7"),
        Selection = Color.Parse("#B5C9F0"),
        SelectionUnfocused = Color.Parse("#C9D9F5"),
    };

    public static ColorTheme BeOS { get; } = new()
    {
        Id = "beos",
        DisplayName = "BeOS",
        Variant = ThemeVariant.Light,
        Background = Color.Parse("#D8D8D8"),
        Surface = Color.Parse("#E8E8E8"),
        SurfaceRaised = Color.Parse("#F0F0F0"),
        Border = Color.Parse("#A0A0A0"),
        TextPrimary = Color.Parse("#000000"),
        TextSecondary = Color.Parse("#404040"),
        Accent = Color.Parse("#1643C4"),
        AccentHover = Color.Parse("#3D6FD0"),
        Success = Color.Parse("#2E8B57"),
        Warning = Color.Parse("#D2691E"),
        Error = Color.Parse("#C00000"),
        Hover = Color.Parse("#C0C0C0"),
        Selection = Color.Parse("#B8C8E8"),
        SelectionUnfocused = Color.Parse("#CDD6EA"),
    };

    public static IReadOnlyList<ColorTheme> All { get; } = BuildAll();

    public static ColorTheme Default => Find(DefaultId) ?? ArcanaMystic;

    public static ColorTheme? Find(string id)
        => All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<ColorTheme> BuildAll()
    {
        var list = new List<ColorTheme> { ArcanaMystic };

        foreach (var (id, display) in PaletteEntries)
        {
            var palette = LoadPalette(id);
            if (palette.Count == 0)
                continue;
            list.Add(PaletteThemeFactory.Create(id, display, palette));
        }

        list.Add(Windows2000);
        list.Add(WindowsXp);
        list.Add(BeOS);
        return list;
    }

    /// <summary>Reads one embedded docs/palletes/*.hex file (one hex color per line).</summary>
    private static List<Color> LoadPalette(string id)
    {
        var assembly = typeof(ColorThemeCatalog).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(".Palettes." + id + ".hex", StringComparison.OrdinalIgnoreCase));
        if (resourceName == null)
            return new List<Color>();

        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return new List<Color>();

            using var reader = new StreamReader(stream);
            var colors = new List<Color>();
            while (reader.ReadLine() is { } raw)
            {
                var hex = raw.Trim();
                if (hex.Length == 0 || hex.StartsWith('#'))
                    continue;
                if (hex.Length == 6)
                    hex = "FF" + hex;
                if (Color.TryParse("#" + hex, out var color))
                    colors.Add(color);
            }
            return colors;
        }
        catch (Exception)
        {
            return new List<Color>();
        }
    }
}
