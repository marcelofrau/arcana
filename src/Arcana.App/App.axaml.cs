using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Arcana.App.Icons;
using Arcana.App.Services;
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
        Log.Information("Arcana starting (log level {Level})", settings.LogLevel);

        var services = new ServiceCollection();
        services.AddSingleton<ArchiveService>();
        services.AddSingleton<PreviewService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<FavoritesService>();
        services.AddSingleton<DefaultIconProvider>();
        services.AddSingleton<IconThemeService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ArchiveViewModel>();
        services.AddTransient<PreviewViewModel>();
        services.AddTransient<ToolsViewModel>();
        services.AddTransient<SettingsViewModel>();

        var provider = services.BuildServiceProvider();

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
