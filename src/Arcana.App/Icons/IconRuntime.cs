namespace Arcana.App.Icons;

/// <summary>
/// Current icon provider, updated when the theme changes. Converters read this
/// instead of holding a provider reference so rows re-render on theme switch.
/// </summary>
public static class IconRuntime
{
    public static IIconProvider Current { get; set; } = new DefaultIconProvider();
}
