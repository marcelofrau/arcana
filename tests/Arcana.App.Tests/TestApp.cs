using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace Arcana.App.Tests;

public static class TestAppBuilder
{
    [ModuleInitializer]
    public static void InitializeAvalonia()
    {
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
    }
}

public class TestApp : Avalonia.Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        var dataGridTheme = new StyleInclude(new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"),
        };
        Styles.Add(dataGridTheme);
        Resources["IconToImage"] = new Icons.IconKeyToImageConverter();
        Resources["NodeIcon"] = new Icons.NodeIconConverter();
        Resources["Equals"] = new Converters.EqualsConverter();
        Resources["Invert"] = new Converters.InvertBoolConverter();
    }
}
