using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private string _language = "en-US";

    [ObservableProperty]
    private int _defaultCompressionLevel = 5;

    [ObservableProperty]
    private string _defaultFormat = "zip";

    [ObservableProperty]
    private int _threadCount = Environment.ProcessorCount;

    [ObservableProperty]
    private bool _enableParallel = true;
}
