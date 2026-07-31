using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Avalonia.Platform;
using Serilog;

namespace Arcana.App.Localization;

/// <summary>
/// Loads UI strings from <c>avares://Arcana.App/Resources/{code}.json</c> and
/// raises <see cref="PropertyChanged"/> for <c>Current</c> so bindings using
/// <see cref="LocalizeExtension"/> re-resolve on culture switch.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static readonly ILogger Log = Serilog.Log.ForContext<LocalizationManager>();

    public static LocalizationManager Instance { get; } = new();

    public sealed record LanguageInfo(string Code, string DisplayName);

    public IReadOnlyList<LanguageInfo> Languages { get; } = new[]
    {
        new LanguageInfo("en", "English"),
        new LanguageInfo("es", "Español"),
        new LanguageInfo("pt-BR", "Português (Brasil)"),
    };

    private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private string _currentCode = "en";

    public string Current => _currentCode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Get(string key)
    {
        if (_resources.TryGetValue(_currentCode, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        if (_resources.TryGetValue("en", out var en) && en.TryGetValue(key, out var enValue))
            return enValue;
        return key;
    }

    /// <summary>Shortcut for VM/command code paths.</summary>
    public static string T(string key) => Instance.Get(key);

    /// <summary>Shortcut with format arguments, e.g. T("msg.extractedTo", path).</summary>
    public static string T(string key, params object?[] args) => string.Format(Instance.Get(key), args);

    public void LoadResources()
    {
        foreach (var lang in Languages)
            _resources[lang.Code] = LoadFile(lang.Code);
    }

    public void SetCulture(string code)
    {
        if (string.IsNullOrEmpty(code) || !_resources.ContainsKey(code))
        {
            if (!string.IsNullOrEmpty(code))
                Log.Warning("Unknown culture {Code}; keeping {Current}", code, _currentCode);
            return;
        }
        if (code == _currentCode)
            return;

        _currentCode = code;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(code);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Invalid culture {Code}", code);
        }
        Log.Information("Language switched to {Code}", code);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
    }

    private static Dictionary<string, string> LoadFile(string code)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri($"avares://Arcana.App/Resources/{code}.json"));
            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load language resources {Code}", code);
            return new Dictionary<string, string>();
        }
    }
}
