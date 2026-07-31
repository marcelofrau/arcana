using Avalonia;
using Avalonia.Media;
using Arcana.App.Services;
using Serilog;

namespace Arcana.App.Themes;

/// <summary>
/// Applies a <see cref="ColorTheme"/> to the running application and persists the
/// selection in settings.json. Applying writes every token (raw Color + matching
/// "…Brush" SolidColorBrush), the DataGrid override brushes, SystemAccentColor and
/// the light/dark RequestedThemeVariant — so all DynamicResource consumers in
/// Themes/Controls.axaml and the views update live without a restart.
/// </summary>
public sealed class ColorThemeService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ColorThemeService>();
    private readonly SettingsService _settings;

    public ColorThemeService(SettingsService settings)
    {
        _settings = settings;
        var saved = settings.Load().ColorTheme;
        Current = ColorThemeCatalog.Find(saved) ?? ColorThemeCatalog.Default;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<ColorTheme> Themes => ColorThemeCatalog.All;

    public ColorTheme Current { get; private set; }

    public bool Apply(string id)
    {
        var theme = ColorThemeCatalog.Find(id) ?? ColorThemeCatalog.Default;
        if (theme.Id.Equals(Current.Id, StringComparison.OrdinalIgnoreCase))
            return true;

        Current = theme;
        ApplyToApplication(theme);
        Persist(theme.Id);
        Log.Information("Applied color theme {Theme}", theme.DisplayName);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Pushes the currently selected theme into Application.Resources
    /// (used at startup so the first window is already themed).</summary>
    public void ApplyCurrent()
        => ApplyToApplication(Current);

    private static void ApplyToApplication(ColorTheme theme)
    {
        if (Application.Current is not { } app)
            return;

        foreach (var (key, color) in theme.TokenColors())
        {
            app.Resources[key] = color;
            app.Resources[key + "Brush"] = new SolidColorBrush(color);
        }

        app.Resources["SystemAccentColor"] = theme.Accent;

        app.Resources["DataGridBackgroundBrush"] = new SolidColorBrush(theme.Surface);
        app.Resources["DataGridHeaderBackgroundBrush"] = new SolidColorBrush(theme.SurfaceRaised);
        app.Resources["DataGridGridLinesBrush"] = new SolidColorBrush(theme.Border);
        app.Resources["DataGridRowHoverBackgroundBrush"] = new SolidColorBrush(theme.Hover);
        app.Resources["DataGridRowSelectedBackgroundBrush"] = new SolidColorBrush(theme.Selection);
        app.Resources["DataGridRowSelectedUnfocusedBackgroundBrush"] = new SolidColorBrush(theme.SelectionUnfocused);
        app.Resources["DataGridCellSelectedBackgroundBrush"] = new SolidColorBrush(theme.Selection);
        app.Resources["DataGridColumnHeaderForegroundBrush"] = new SolidColorBrush(theme.TextSecondary);
        app.Resources["DataGridColumnHeaderHoverBackgroundBrush"] = new SolidColorBrush(theme.Hover);
        app.Resources["DataGridColumnHeaderPressedBackgroundBrush"] = new SolidColorBrush(theme.Border);
        app.Resources["DataGridCellForegroundBrush"] = new SolidColorBrush(theme.TextPrimary);

        app.RequestedThemeVariant = theme.Variant;
    }

    private void Persist(string id)
    {
        try
        {
            var settings = _settings.Load();
            settings.ColorTheme = id;
            _settings.Save(settings);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not save color theme settings");
        }
    }
}
