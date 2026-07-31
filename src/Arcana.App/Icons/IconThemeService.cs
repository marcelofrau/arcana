using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog;

namespace Arcana.App.Icons;

/// <summary>
/// Manages icon themes. Built-ins are "Papirus" (default) and "Material";
/// WinRAR-style themes can be installed as .theme.rar and are extracted to
/// %APPDATA%\Arcana\Themes. The last selected theme is persisted.
/// </summary>
public sealed class IconThemeService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<IconThemeService>();

    private readonly DefaultIconProvider _materialProvider;
    private readonly PapirusIconProvider _papirusProvider;
    private readonly string _themesDir;
    private readonly string _settingsPath;
    private IIconProvider _current;

    public event EventHandler? Changed;

    public IconThemeService(DefaultIconProvider materialProvider)
    {
        _materialProvider = materialProvider;
        _papirusProvider = new PapirusIconProvider(materialProvider);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDir = Path.Combine(appData, "Arcana", "Themes");
        _settingsPath = Path.Combine(appData, "Arcana", "settings.json");
        _current = _papirusProvider;

        var saved = ReadSavedTheme();
        if (saved != null && saved != PapirusIconProvider.BuiltInName)
            ApplyTheme(saved);
    }

    public IIconProvider Current => _current;
    public string ThemesDirectory => _themesDir;

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
        return ApplyTheme(PapirusIconProvider.BuiltInName);
    }

    public bool ApplyTheme(string name)
    {
        IIconProvider? next = null;

        if (string.Equals(name, PapirusIconProvider.BuiltInName, StringComparison.OrdinalIgnoreCase))
        {
            next = _papirusProvider;
        }
        else if (string.Equals(name, PapirusIconProvider.MaterialName, StringComparison.OrdinalIgnoreCase))
        {
            next = _materialProvider;
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

        _current = next;
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
            if (_current is WinRarThemeProvider)
            {
                var info = InstalledThemes.FirstOrDefault(t =>
                    t.Title.Equals(_current.Name, StringComparison.OrdinalIgnoreCase));
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
            if (!File.Exists(_settingsPath))
                return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            if (doc.RootElement.TryGetProperty("theme", out var prop))
                return prop.GetString();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read theme settings");
        }
        return null;
    }

    private void SaveTheme(string name)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new { theme = name }));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not save theme settings");
        }
    }
}
