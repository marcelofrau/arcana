using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Arcana.App.Services;
using Arcana.App.ViewModels;
using Arcana.App.Views;

namespace Arcana.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ArchiveService>();
        services.AddSingleton<PreviewService>();
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
