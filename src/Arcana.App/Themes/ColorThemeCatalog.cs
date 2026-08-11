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
        SurfaceRaised = Color.Parse("#ECE9D8"),
        Border = Color.Parse("#808080"),
        TextPrimary = Color.Parse("#000000"),
        TextSecondary = Color.Parse("#808080"),
        Accent = Color.Parse("#000080"),
        AccentHover = Color.Parse("#1084D0"),
        Success = Color.Parse("#007000"),
        Warning = Color.Parse("#C06000"),
        Error = Color.Parse("#C00000"),
        Hover = Color.Parse("#C8C4BC"),
        Selection = Color.Parse("#A8BEE8"),
        SelectionUnfocused = Color.Parse("#D0D8E4"),
        ToolbarTop = Color.Parse("#FFFFFF"),
        ToolbarMid = Color.Parse("#ECE9D8"),
        ToolbarBottom = Color.Parse("#D4D0C8"),
    };

    public static ColorTheme WindowsXp { get; } = new()
    {
        Id = "windowsxp",
        DisplayName = "Windows XP",
        Variant = ThemeVariant.Light,
        Background = Color.Parse("#ECE9D8"),
        Surface = Color.Parse("#FFFFFF"),
        SurfaceRaised = Color.Parse("#FFFFFF"),
        Border = Color.Parse("#7BA2E7"),
        TextPrimary = Color.Parse("#000000"),
        TextSecondary = Color.Parse("#666666"),
        Accent = Color.Parse("#0054E3"),
        AccentHover = Color.Parse("#3D67B8"),
        Success = Color.Parse("#339900"),
        Warning = Color.Parse("#F3B21B"),
        Error = Color.Parse("#CC3300"),
        Hover = Color.Parse("#FFE8A0"),
        Selection = Color.Parse("#7FA8E8"),
        SelectionUnfocused = Color.Parse("#D6E4F7"),
        ToolbarTop = Color.Parse("#FFFFFF"),
        ToolbarMid = Color.Parse("#FDFBF3"),
        ToolbarBottom = Color.Parse("#ECE9D8"),
    };

    public static ColorTheme BeOS { get; } = new()
    {
        Id = "beos",
        DisplayName = "BeOS",
        Variant = ThemeVariant.Light,
        Background = Color.Parse("#BFBFBF"),
        Surface = Color.Parse("#C7C7C7"),
        SurfaceRaised = Color.Parse("#D0D0D0"),
        Border = Color.Parse("#808080"),
        TextPrimary = Color.Parse("#000000"),
        TextSecondary = Color.Parse("#6E6E6E"),
        Accent = Color.Parse("#1639BC"),
        AccentHover = Color.Parse("#3D6FD0"),
        Success = Color.Parse("#2E8B57"),
        Warning = Color.Parse("#D2691E"),
        Error = Color.Parse("#C00000"),
        Hover = Color.Parse("#F7E27E"),
        Selection = Color.Parse("#7C9CE0"),
        SelectionUnfocused = Color.Parse("#B8BEC8"),
        ToolbarTop = Color.Parse("#FFDF4D"),
        ToolbarMid = Color.Parse("#FFC500"),
        ToolbarBottom = Color.Parse("#E0AC00"),
    };

    /// <summary>Follows the OS light/dark mode (Windows-flavored light palette,
    /// Windows dark palette when the OS is dark).</summary>
    public static ColorTheme System { get; } = new()
    {
        Id = "system",
        DisplayName = "System",
        Variant = ThemeVariant.Default,
        Background = Color.Parse("#F0F0F0"),
        Surface = Color.Parse("#FFFFFF"),
        SurfaceRaised = Color.Parse("#FAFAFA"),
        Border = Color.Parse("#D9D9D9"),
        TextPrimary = Color.Parse("#1A1A1A"),
        TextSecondary = Color.Parse("#6B6B6B"),
        Accent = Color.Parse("#0078D4"),
        AccentHover = Color.Parse("#106EBE"),
        Success = Color.Parse("#0F7B0F"),
        Warning = Color.Parse("#9D5D00"),
        Error = Color.Parse("#C42B1C"),
        Hover = Color.Parse("#EAF0F7"),
        Selection = Color.Parse("#CDE6FA"),
        SelectionUnfocused = Color.Parse("#E4EEF6"),
        ToolbarTop = Color.Parse("#FFFFFF"),
        ToolbarMid = Color.Parse("#FFFFFF"),
        ToolbarBottom = Color.Parse("#F0F0F0"),
        DarkTheme = new()
        {
            Id = "system-dark",
            DisplayName = "System (dark)",
            Variant = ThemeVariant.Dark,
            Background = Color.Parse("#202020"),
            Surface = Color.Parse("#2B2B2B"),
            SurfaceRaised = Color.Parse("#323232"),
            Border = Color.Parse("#454545"),
            TextPrimary = Color.Parse("#F3F3F3"),
            TextSecondary = Color.Parse("#A0A0A0"),
            Accent = Color.Parse("#4CC2FF"),
            AccentHover = Color.Parse("#3FA9EA"),
            Success = Color.Parse("#6CCB5F"),
            Warning = Color.Parse("#FCE100"),
            Error = Color.Parse("#FF99A4"),
            Hover = Color.Parse("#3A3A3A"),
            Selection = Color.Parse("#2D5A87"),
            SelectionUnfocused = Color.Parse("#2B3440"),
        },
    };

    public static ColorTheme Light { get; } = new()
    {
        Id = "light",
        DisplayName = "Light",
        Variant = ThemeVariant.Light,
        Background = Color.Parse("#F4F6FA"),
        Surface = Color.Parse("#FFFFFF"),
        SurfaceRaised = Color.Parse("#FAFBFD"),
        Border = Color.Parse("#D8DCE4"),
        TextPrimary = Color.Parse("#17202A"),
        TextSecondary = Color.Parse("#5D6B7A"),
        Accent = Color.Parse("#2E86DE"),
        AccentHover = Color.Parse("#1F6FC0"),
        Success = Color.Parse("#219653"),
        Warning = Color.Parse("#B7791F"),
        Error = Color.Parse("#D13438"),
        Hover = Color.Parse("#EAF0F7"),
        Selection = Color.Parse("#CBE1F7"),
        SelectionUnfocused = Color.Parse("#DFE8F2"),
    };

    public static ColorTheme Dark { get; } = new()
    {
        Id = "dark",
        DisplayName = "Dark",
        Variant = ThemeVariant.Dark,
        Background = Color.Parse("#101014"),
        Surface = Color.Parse("#1A1A20"),
        SurfaceRaised = Color.Parse("#22222A"),
        Border = Color.Parse("#33333F"),
        TextPrimary = Color.Parse("#E8E8EE"),
        TextSecondary = Color.Parse("#9A9AA8"),
        Accent = Color.Parse("#5E7CE2"),
        AccentHover = Color.Parse("#738FF0"),
        Success = Color.Parse("#3FB981"),
        Warning = Color.Parse("#D9A441"),
        Error = Color.Parse("#E26A6A"),
        Hover = Color.Parse("#26262E"),
        Selection = Color.Parse("#33436B"),
        SelectionUnfocused = Color.Parse("#272B3A"),
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
        list.Add(System);
        list.Add(Light);
        list.Add(Dark);
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
