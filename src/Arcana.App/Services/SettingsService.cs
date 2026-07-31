using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
}

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _filePath;

    public SettingsService()
    {
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
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // corrupted settings fall back to defaults
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
        }
        catch (Exception)
        {
            // settings persistence is best-effort
        }
    }
}
