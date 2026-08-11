using System;
using System.Linq;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _version;

    [ObservableProperty]
    private string _tagline;

    [ObservableProperty]
    private string _supportedFormats;

    [ObservableProperty]
    private string _formats;

    [ObservableProperty]
    private string _credits;

    public Bitmap Logo { get; }

    public AboutViewModel()
    {
        Logo = new Bitmap(AssetLoader.Open(new Uri("avares://Arcana.App/Assets/about-logo.png")));
        _title = T("msg.about");
        _version = VersionInfo;
        _tagline = T("msg.aboutTagline");
        _supportedFormats = T("msg.aboutSupported");
        _formats = T("msg.aboutFormats");
        _credits = BuildCredits();
    }

    private static string BuildCredits() => string.Join("\n", new[]
    {
        "Arcana",
        VersionInfo,
        T("msg.aboutTagline"),
        "",
        T("msg.aboutPoweredBy"),
        T("msg.aboutEngines"),
        T("msg.aboutIcons"),
        "",
        T("msg.aboutThanks"),
        "",
        T("msg.aboutStory"),
    });

    private static string VersionInfo
    {
        get
        {
            var attr = typeof(AboutViewModel).Assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            var version = attr?.InformationalVersion ?? typeof(AboutViewModel).Assembly.GetName().Version?.ToString() ?? "unknown";
            return version.Split('+')[0];
        }
    }
}
