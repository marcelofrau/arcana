namespace Arcana.App.Icons;

/// <summary>
/// Metadata + location for an installed WinRAR-style theme (extracted .theme.rar).
/// </summary>
public sealed class IconThemeInfo
{
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Directory { get; init; } = string.Empty;
}
