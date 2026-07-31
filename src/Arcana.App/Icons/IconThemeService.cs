using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Arcana.App.Services;
using Serilog;

namespace Arcana.App.Icons;

/// <summary>
/// Manages icon themes. Built-in action themes are "Numix" (default), "Tango",
/// "La Capitaine" and "Material"; the filesystem icons (folders and file
/// mimetypes) always come from the "Papirus" set. "Papirus" is still accepted
/// as a legacy whole-app theme value. WinRAR-style themes can be installed as
/// .theme.rar and are extracted to %APPDATA%\Arcana\Themes; they replace the
/// action icons only. The last selected theme is persisted.
/// </summary>
public sealed class IconThemeService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<IconThemeService>();

    private readonly DefaultIconProvider _materialProvider;
    private readonly PapirusIconProvider _papirusProvider;
    private readonly SettingsService _settings;
    private readonly string _themesDir;
    private readonly string _settingsPath;
    private readonly Dictionary<string, IIconProvider> _actionProviders;
    private IIconProvider _currentAction;
    private CompositeIconProvider _current;

    public event EventHandler? Changed;

    public IconThemeService(DefaultIconProvider materialProvider, SettingsService settings)
    {
        _materialProvider = materialProvider;
        _papirusProvider = new PapirusIconProvider(materialProvider);
        _settings = settings;

        var numix = new PngIconThemeProvider(
            PngIconThemeProvider.NumixName, "Numix", NumixSlots, materialProvider);
        var tango = new PngIconThemeProvider(
            PngIconThemeProvider.TangoName, "Tango", TangoSlots, materialProvider);
        var laCapitaine = new PngIconThemeProvider(
            PngIconThemeProvider.LaCapitaineName, "LaCapitaine", LaCapitaineSlots, materialProvider);
        _actionProviders = new Dictionary<string, IIconProvider>(StringComparer.OrdinalIgnoreCase)
        {
            [PngIconThemeProvider.NumixName] = numix,
            [PngIconThemeProvider.TangoName] = tango,
            [PngIconThemeProvider.LaCapitaineName] = laCapitaine,
            [PapirusIconProvider.MaterialName] = materialProvider,
            [PapirusIconProvider.BuiltInName] = _papirusProvider,
        };

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDir = Path.Combine(appData, "Arcana", "Themes");
        _settingsPath = Path.Combine(appData, "Arcana", "settings.json");
        _currentAction = numix;
        _current = new CompositeIconProvider(_papirusProvider, _currentAction);

        var saved = ReadSavedTheme();
        if (saved != null)
            ApplyTheme(saved);
    }

    public IIconProvider Current => _current;
    public IIconProvider FilesystemProvider => _papirusProvider;
    public IIconProvider CurrentAction => _currentAction;
    public string ThemesDirectory => _themesDir;

    /// <summary>Selectable built-in action themes, in menu order.</summary>
    public IReadOnlyList<string> BuiltInThemes { get; } = new[]
    {
        PngIconThemeProvider.NumixName,
        PngIconThemeProvider.TangoName,
        PngIconThemeProvider.LaCapitaineName,
        PapirusIconProvider.MaterialName,
    };

    public IReadOnlyList<IconThemeInfo> InstalledThemes
    {
        get
        {
            if (!Directory.Exists(_themesDir))
                return Array.Empty<IconThemeInfo>();

            return Directory.EnumerateDirectories(_themesDir)
                .Where(dir => HasToolbar(dir))
                .Select(BuildInfo)
                .ToList();
        }
    }

    public bool ApplyDefault()
    {
        return ApplyTheme(PngIconThemeProvider.NumixName);
    }

    public bool ApplyTheme(string name)
    {
        IIconProvider? next = null;

        if (_actionProviders.TryGetValue(name, out var builtIn))
        {
            next = builtIn;
        }
        else
        {
            var dir = FindThemeDir(name);
            if (dir != null)
                next = WinRarThemeProvider.TryCreateFromDirectory(dir, _materialProvider);
        }

        if (next == null)
        {
            Log.Warning("Theme {Theme} not found", name);
            return false;
        }

        _currentAction = next;
        _current = new CompositeIconProvider(_papirusProvider, _currentAction);
        SaveTheme(name);
        Log.Information("Applied theme {Theme}", next.Name);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool InstallTheme(string themeFile)
    {
        if (!File.Exists(themeFile))
            return false;

        try
        {
            Directory.CreateDirectory(_themesDir);
            var fileName = Path.GetFileName(themeFile);
            var dest = Path.Combine(_themesDir, fileName);
            File.Copy(themeFile, dest, overwrite: true);

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extractDir = Path.Combine(_themesDir, name);
            var provider = WinRarThemeProvider.TryCreate(dest, extractDir, _materialProvider);
            if (provider == null)
            {
                Log.Warning("Theme {Theme} could not be loaded; removed", fileName);
                File.Delete(dest);
                return false;
            }

            Log.Information("Installed theme {Theme}", provider.Name);
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to install theme {Theme}", themeFile);
            return false;
        }
    }

    public void OpenThemesFolder()
    {
        Directory.CreateDirectory(_themesDir);
        Process.Start(new ProcessStartInfo
        {
            FileName = _themesDir,
            UseShellExecute = true,
        });
    }

    public string? ResolveWindowIconPath(string themeDir)
    {
        var candidates = new[] { "RAR.ico", "File.ico" };
        foreach (var name in candidates)
        {
            var path = Directory.EnumerateFiles(themeDir, name, SearchOption.AllDirectories)
                .FirstOrDefault();
            if (path != null)
                return path;
        }
        return null;
    }

    public string? CurrentWindowIconPath
    {
        get
        {
            if (_currentAction is WinRarThemeProvider)
            {
                var info = InstalledThemes.FirstOrDefault(t =>
                    t.Title.Equals(_currentAction.Name, StringComparison.OrdinalIgnoreCase));
                if (info != null)
                    return ResolveWindowIconPath(info.Directory);
            }
            return null;
        }
    }

    private string? FindThemeDir(string name)
    {
        if (!Directory.Exists(_themesDir))
            return null;

        var direct = Path.Combine(_themesDir, name);
        if (Directory.Exists(direct) && HasToolbar(direct))
            return direct;

        return Directory.EnumerateDirectories(_themesDir)
            .FirstOrDefault(dir =>
                Path.GetFileName(dir).Equals(name, StringComparison.OrdinalIgnoreCase)
                && HasToolbar(dir));
    }

    private static bool HasToolbar(string dir)
        => Directory.Exists(Path.Combine(dir, "Toolbar"))
           || Directory.EnumerateDirectories(dir, "Toolbar", SearchOption.AllDirectories).Any();

    private IconThemeInfo BuildInfo(string dir)
    {
        var desc = WinRarThemeProvider.TryCreateFromDirectory(dir, _materialProvider) is { } p
            ? ReadDescription(dir)
            : null;
        return new IconThemeInfo
        {
            Name = Path.GetFileName(dir),
            Title = desc?.Title ?? Path.GetFileName(dir),
            Version = desc?.Version ?? "",
            Description = desc?.Description ?? "",
            Directory = dir,
        };
    }

    private static (string? Title, string? Version, string? Description)? ReadDescription(string themeDir)
    {
        var file = Directory.EnumerateFiles(themeDir, "*description*.txt", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (file == null)
            return null;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadAllLines(file))
        {
            var line = raw.Trim();
            int eq = line.IndexOf('=');
            if (eq > 0)
                values[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }

        return (
            values.TryGetValue("title", out var t) ? t : null,
            values.TryGetValue("version", out var v) ? v : null,
            values.TryGetValue("description", out var d) ? d : null
        );
    }

    private string? ReadSavedTheme()
    {
        try
        {
            var saved = _settings.Load().IconTheme;
            if (!string.IsNullOrEmpty(saved))
                return saved;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read theme from settings");
        }

        // Legacy: pre-AppSettings persistence wrote {"theme": "..."} to the same file.
        try
        {
            if (!File.Exists(_settingsPath))
                return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            if (doc.RootElement.TryGetProperty("theme", out var prop))
                return prop.GetString();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read legacy theme settings");
        }
        return null;
    }

    private void SaveTheme(string name)
    {
        try
        {
            var settings = _settings.Load();
            settings.IconTheme = name;
            _settings.Save(settings);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not save theme settings");
        }
    }

    private static readonly IReadOnlyDictionary<IconKey, string> NumixSlots =
        new Dictionary<IconKey, string>
        {
            [IconKey.Open] = "open",
            [IconKey.Add] = "add",
            [IconKey.Extract] = "extract",
            [IconKey.Test] = "test",
            [IconKey.View] = "view",
            [IconKey.Delete] = "delete",
            [IconKey.Find] = "find",
            [IconKey.Info] = "info",
            [IconKey.Save] = "save",
            [IconKey.Settings] = "settings",
            [IconKey.Help] = "help",
            [IconKey.SortUp] = "sort-up",
            [IconKey.SortDown] = "sort-down",
        };

    private static readonly IReadOnlyDictionary<IconKey, string> TangoSlots =
        new Dictionary<IconKey, string>
        {
            [IconKey.Add] = "add",
            [IconKey.Delete] = "delete",
            [IconKey.Find] = "find",
            [IconKey.Info] = "info",
            [IconKey.Save] = "save",
        };

    private static readonly IReadOnlyDictionary<IconKey, string> LaCapitaineSlots =
        new Dictionary<IconKey, string>
        {
            [IconKey.Open] = "open",
            [IconKey.Add] = "add",
            [IconKey.Extract] = "extract",
            [IconKey.Test] = "test",
            [IconKey.View] = "view",
            [IconKey.Delete] = "delete",
            [IconKey.Find] = "find",
            [IconKey.Info] = "info",
            [IconKey.Save] = "save",
            [IconKey.Settings] = "settings",
            [IconKey.SortUp] = "sort-up",
            [IconKey.SortDown] = "sort-down",
        };
}
