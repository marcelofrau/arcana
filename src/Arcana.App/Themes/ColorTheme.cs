using Avalonia.Media;
using Avalonia.Styling;

namespace Arcana.App.Themes;

/// <summary>
/// A named color theme: a semantic set of colors plus a light/dark variant.
/// Tokens map 1:1 to the resource keys declared in Themes/Colors.axaml and are
/// pushed into Application.Resources at runtime (see ColorThemeService).
/// </summary>
public sealed class ColorTheme
{
    /// <summary>Stable identifier used for persistence and lookups (e.g. "brewerviridis").</summary>
    public required string Id { get; init; }

    /// <summary>Human-friendly name shown in the dropdown menu (e.g. "brewerViridis").</summary>
    public required string DisplayName { get; init; }

    /// <summary>Light or dark — drives FluentTheme via RequestedThemeVariant.</summary>
    public ThemeVariant Variant { get; init; } = ThemeVariant.Dark;

    public required Color Background { get; init; }
    public required Color Surface { get; init; }
    public required Color SurfaceRaised { get; init; }
    public required Color Border { get; init; }
    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }
    public required Color Accent { get; init; }
    public required Color AccentHover { get; init; }
    public required Color Success { get; init; }
    public required Color Warning { get; init; }
    public required Color Error { get; init; }
    public required Color Hover { get; init; }
    public required Color Selection { get; init; }
    public required Color SelectionUnfocused { get; init; }

    /// <summary>
    /// Semantic tokens as (resource key, color) pairs. The keys match the ones in
    /// Themes/Colors.axaml; ColorThemeService writes both the raw Color and a
    /// matching "…Brush" SolidColorBrush for every token.
    /// </summary>
    public IEnumerable<(string Key, Color Color)> TokenColors()
    {
        yield return ("AppBackground", Background);
        yield return ("AppSurface", Surface);
        yield return ("AppSurfaceRaised", SurfaceRaised);
        yield return ("AppBorder", Border);
        yield return ("AppTextPrimary", TextPrimary);
        yield return ("AppTextSecondary", TextSecondary);
        yield return ("AppAccent", Accent);
        yield return ("AppAccentHover", AccentHover);
        yield return ("AppSuccess", Success);
        yield return ("AppWarning", Warning);
        yield return ("AppError", Error);
        yield return ("AppHover", Hover);
        yield return ("AppSelection", Selection);
        yield return ("AppSelectionUnfocused", SelectionUnfocused);
    }
}
