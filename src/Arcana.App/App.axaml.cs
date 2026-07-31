using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Arcana.App.Icons;
using Arcana.App.Localization;
using Arcana.App.Services;
using Arcana.App.Themes;
using Arcana.App.ViewModels;
using Arcana.App.Views;
using Arcana.Core.Logging;

namespace Arcana.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        LogConfig.SetLevel(settings.LogLevel);
        Log.Information("Console bridge: redirected={Redirected} attached={Attached}",
            ConsoleAttach.LastState.Redirected, ConsoleAttach.LastState.Attached);
        Log.Information("Arcana starting (log level {Level}, language {Language})",
            settings.LogLevel, settings.Language);

        LocalizationManager.Instance.LoadResources();
        LocalizationManager.Instance.SetCulture(settings.Language);

        GlobalExceptionHandler.Attach();

        var services = new ServiceCollection();
        services.AddSingleton<ArchiveService>();
        services.AddSingleton<PreviewService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<FavoritesService>();
        services.AddSingleton<DefaultIconProvider>();
        services.AddSingleton<IconThemeService>();
        services.AddSingleton<ColorThemeService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ArchiveViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<ToolsViewModel>();
        services.AddTransient<SettingsViewModel>();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ColorThemeService>().ApplyCurrent();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
