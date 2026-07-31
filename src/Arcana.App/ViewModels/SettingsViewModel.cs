using Arcana.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;

    public string[] Formats { get; } = { "zip", "7z", "zstd", "tar" };
    public int[] Levels { get; } = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    public string[] LogLevels { get; } = { "trace", "debug", "info", "warn", "error", "fatal" };

    [ObservableProperty]
    private string _defaultFormat = "zip";

    [ObservableProperty]
    private int _defaultCompressionLevel = 5;

    [ObservableProperty]
    private int _threadCount = Environment.ProcessorCount;

    [ObservableProperty]
    private bool _enableParallel = true;

    [ObservableProperty]
    private string _logLevel = "info";

    public SettingsViewModel(SettingsService service)
    {
        _service = service;
        var s = service.Load();
        DefaultFormat = s.DefaultFormat;
        DefaultCompressionLevel = s.DefaultCompressionLevel;
        ThreadCount = s.ThreadCount;
        EnableParallel = s.EnableParallel;
        LogLevel = s.LogLevel;
    }

    public void Save()
    {
        _service.Save(new AppSettings
        {
            DefaultFormat = DefaultFormat,
            DefaultCompressionLevel = DefaultCompressionLevel,
            ThreadCount = ThreadCount,
            EnableParallel = EnableParallel,
            LogLevel = LogLevel,
        });
    }
}
