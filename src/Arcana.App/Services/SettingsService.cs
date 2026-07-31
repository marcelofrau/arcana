using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace Arcana.App.Services;

public sealed class AppSettings
{
    public string DefaultFormat { get; set; } = "zip";
    public int DefaultCompressionLevel { get; set; } = 5;
    public int ThreadCount { get; set; } = System.Environment.ProcessorCount;
    public bool EnableParallel { get; set; } = true;
    public bool ShowMenuBar { get; set; } = true;
    public bool ShowToolbar { get; set; } = true;
    public bool ShowFileList { get; set; } = true;
    public bool ShowComments { get; set; } = true;
    public string LogLevel { get; set; } = "info";
    public string Language { get; set; } = "en";
    public string IconTheme { get; set; } = string.Empty;
    public string ColorTheme { get; set; } = "brewerviridis";
}

public sealed class SettingsService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<SettingsService>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _filePath;

    public SettingsService(string? filePath = null)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            _filePath = filePath;
            return;
        }
        var dir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "Arcana");
        _filePath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                Log.Debug("Loaded settings from {File} (log level {LogLevel})", _filePath, settings.LogLevel);
                return settings;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Settings file {File} is corrupted; falling back to defaults", _filePath);
        }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_filePath, json);
            Log.Debug("Saved settings to {File}", _filePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not persist settings to {File}", _filePath);
        }
    }
}
