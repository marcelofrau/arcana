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

        app.Resources["AppAccentGlowInner"] = WithAlpha(theme.Accent, 0x4D);
        app.Resources["AppAccentGlowOuter"] = WithAlpha(theme.Accent, 0x00);
        app.Resources["AppAccentShine"] = WithAlpha(theme.Accent, 0x73);
        app.Resources["AppAccentShineClear"] = WithAlpha(theme.Accent, 0x00);
        app.Resources["AppChipBorder"] = WithAlpha(theme.Accent, 0x3D);
        app.Resources["AppAccentSoft"] = WithAlpha(theme.Accent, 0x14);
        app.Resources["AppAccentSoftBorder"] = WithAlpha(theme.Accent, 0x20);

        // Derived brushes (previously static in Colors.axaml and never themed).
        app.Resources["AppSurfaceLowBrush"] = new SolidColorBrush(Blend(theme.Surface, Color.FromRgb(0, 0, 0), 0.14));
        app.Resources["AppDividerBrush"] = new SolidColorBrush(Blend(theme.Surface, theme.Border, 0.6));
        app.Resources["AppAccentDimBrush"] = new SolidColorBrush(WithAlpha(theme.Accent, 0x66));

        var accentGradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        };
        accentGradient.GradientStops.Add(new GradientStop(theme.AccentHover, 0));
        accentGradient.GradientStops.Add(new GradientStop(theme.Accent, 1));
        app.Resources["AppAccentGradientBrush"] = accentGradient;

        // Glass / murrine action-button brushes (colored glass: sheen -> solid -> depth).
        app.Resources["AppAccentGlassBrush"] = VerticalGradient(
            (0.00, Blend(theme.AccentHover, Colors.White, 0.50)),
            (0.40, theme.Accent),
            (1.00, Blend(theme.Accent, Colors.Black, 0.25)));
        app.Resources["AppAccentGlassHoverBrush"] = VerticalGradient(
            (0.00, Blend(theme.AccentHover, Colors.White, 0.60)),
            (0.40, theme.AccentHover),
            (1.00, theme.Accent));
        app.Resources["AppAccentGlassPressedBrush"] = VerticalGradient(
            (0.00, Blend(theme.Accent, Colors.White, 0.15)),
            (0.40, theme.Accent),
            (1.00, Blend(theme.Accent, Colors.Black, 0.45)));
        app.Resources["AppAccentGlassBorderBrush"] = VerticalGradient(
            (0.00, Color.FromArgb(0x66, 255, 255, 255)),
            (0.70, Color.FromArgb(0x14, 255, 255, 255)),
            (1.00, Color.FromArgb(0x00, 255, 255, 255)));
        app.Resources["AppGlassBrush"] = VerticalGradient(
            (0.00, Color.FromArgb(0x1F, 255, 255, 255)),
            (0.50, Color.FromArgb(0x08, 255, 255, 255)),
            (1.00, Color.FromArgb(0x38, 0, 0, 0)));
        app.Resources["AppGlassHoverBrush"] = VerticalGradient(
            (0.00, Color.FromArgb(0x2E, 255, 255, 255)),
            (0.50, Color.FromArgb(0x14, 255, 255, 255)),
            (1.00, Color.FromArgb(0x30, 0, 0, 0)));
        app.Resources["AppGlassPressedBrush"] = VerticalGradient(
            (0.00, Color.FromArgb(0x10, 255, 255, 255)),
            (0.50, Color.FromArgb(0x04, 255, 255, 255)),
            (1.00, Color.FromArgb(0x52, 0, 0, 0)));
        app.Resources["AppGlassBorderBrush"] = VerticalGradient(
            (0.00, Color.FromArgb(0x3D, 255, 255, 255)),
            (0.70, Color.FromArgb(0x0E, 255, 255, 255)),
            (1.00, Color.FromArgb(0x00, 255, 255, 255)));

        // Focus rings (accent-tinted gradient for plain buttons, bright rim for accent).
        app.Resources["AppFocusBorderBrush"] = VerticalGradient(
            (0.00, Blend(theme.Accent, Colors.White, 0.35)),
            (0.60, WithAlpha(theme.Accent, 0x55)),
            (1.00, WithAlpha(theme.Accent, 0x00)));
        app.Resources["AppFocusAccentBorderBrush"] = VerticalGradient(
            (0.00, Color.FromArgb(0xAA, 255, 255, 255)),
            (0.60, Color.FromArgb(0x33, 255, 255, 255)),
            (1.00, Color.FromArgb(0x00, 255, 255, 255)));

        // Selection highlight for ListBox/ListView/TreeView (ListBoxItem:selected etc.).
        // Strong accent blend so selection stays visible in every theme.
        app.Resources["AppSelectionStrong"] = Blend(theme.Surface, theme.Accent, 0.60);
        app.Resources["AppSelectionForegroundBrush"] = new SolidColorBrush(Colors.White);

        Brush toolbar;
        if (theme.ToolbarTop is { } top)
        {
            var linear = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            };
            linear.GradientStops.Add(new GradientStop(top, 0));
            linear.GradientStops.Add(new GradientStop(theme.ToolbarMid ?? top, 0.55));
            linear.GradientStops.Add(new GradientStop(theme.ToolbarBottom ?? top, 1));
            toolbar = linear;
        }
        else
        {
            toolbar = new RadialGradientBrush
            {
                Center = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.8, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(1.8, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(WithAlpha(theme.Accent, 0x26), 0),
                    new GradientStop(WithAlpha(theme.Accent, 0x00), 1),
                },
            };
        }
        app.Resources["AppToolbarGradientBrush"] = toolbar;

        app.Resources["DataGridBackgroundBrush"] = new SolidColorBrush(theme.Surface);
        app.Resources["DataGridHeaderBackgroundBrush"] = new SolidColorBrush(theme.SurfaceRaised);
        app.Resources["DataGridGridLinesBrush"] = new SolidColorBrush(theme.Border);
        app.Resources["DataGridRowHoverBackgroundBrush"] = new SolidColorBrush(theme.Hover);
        app.Resources["DataGridRowSelectedBackgroundBrush"] = new SolidColorBrush(Blend(theme.Surface, theme.Accent, 0.55));
        app.Resources["DataGridRowSelectedUnfocusedBackgroundBrush"] = new SolidColorBrush(Blend(theme.Surface, theme.Accent, 0.30));
        app.Resources["DataGridCellSelectedBackgroundBrush"] = new SolidColorBrush(Blend(theme.Surface, theme.Accent, 0.55));
        app.Resources["DataGridColumnHeaderForegroundBrush"] = new SolidColorBrush(theme.TextSecondary);
        app.Resources["DataGridColumnHeaderHoverBackgroundBrush"] = new SolidColorBrush(theme.Hover);
        app.Resources["DataGridColumnHeaderPressedBackgroundBrush"] = new SolidColorBrush(theme.Border);
        app.Resources["DataGridCellForegroundBrush"] = new SolidColorBrush(theme.TextPrimary);

        app.RequestedThemeVariant = theme.Variant;
    }

    private static Color WithAlpha(Color color, byte alpha)
        => new(alpha, color.R, color.G, color.B);

    private static LinearGradientBrush VerticalGradient(params (double Offset, Color Color)[] stops)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        foreach (var (offset, color) in stops)
            brush.GradientStops.Add(new GradientStop(color, offset));
        return brush;
    }

    private static Color Blend(Color from, Color to, double amount)
        => Color.FromArgb(255,
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));

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
