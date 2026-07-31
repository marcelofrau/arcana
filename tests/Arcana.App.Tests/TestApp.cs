using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;

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
    }
}
