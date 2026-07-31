using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Arcana.Core.Compression;
using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.App.Icons;

/// <summary>
/// Renders toolbar icons from a WinRAR theme (.theme.rar extracted to disk).
/// WinRAR toolbar bitmaps use a magenta chroma key (#FF00FF) that is converted to
/// transparency. Strip layout and per-button files are both handled.
/// </summary>
public sealed class WinRarThemeProvider : IIconProvider
{
    private static readonly ILogger Log = Serilog.Log.ForContext<WinRarThemeProvider>();

    private static readonly string[] SlotNames =
    {
        "Open", "Save", "Add", "Extract", "Test", "View", "Delete", "Find", "Wizard", "Info"
    };

    private readonly DefaultIconProvider _fallback;
    private readonly Dictionary<IconKey, IImage> _icons = new();

    public string Name { get; }
    public double ToolbarSize { get; }

    private WinRarThemeProvider(string name, double toolbarSize, DefaultIconProvider fallback,
                                Dictionary<IconKey, IImage> icons)
    {
        Name = name;
        ToolbarSize = toolbarSize;
        _fallback = fallback;
        _icons = icons;
    }

    public IImage? GetIcon(IconKey key)
        => _icons.TryGetValue(key, out var icon) ? icon : _fallback.GetIcon(key);

    /// <summary>
    /// Extracts a .theme.rar into <paramref name="destinationDir"/> and builds a provider.
    /// Returns null when no usable toolbar is found.
    /// </summary>
    public static WinRarThemeProvider? TryCreate(string themeFile, string destinationDir,
                                                 DefaultIconProvider fallback)
    {
        try
        {
            Directory.CreateDirectory(destinationDir);
            using var archive = ArchiveFactory.OpenAsync(themeFile).GetAwaiter().GetResult();
            ExtractVfs(archive.Vfs.Root, destinationDir);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to extract theme {ThemeFile}: {Message}", themeFile, ex.Message);
            return null;
        }

        return TryCreateFromDirectory(destinationDir, fallback);
    }

    public static WinRarThemeProvider? TryCreateFromDirectory(string themeDir, DefaultIconProvider fallback)
    {
        var info = ReadDescription(themeDir);
        var toolbarDir = FindDir(themeDir, "Toolbar");
        if (toolbarDir == null)
        {
            Log.Warning("Theme {Theme} has no Toolbar directory", themeDir);
            return null;
        }

        var bitmaps = LoadBitmaps(toolbarDir);
        if (bitmaps.Count == 0)
        {
            Log.Warning("Theme {Theme} has no usable toolbar bitmaps", themeDir);
            return null;
        }

        var icons = MapSlots(bitmaps);
        var size = PickToolbarSize(icons, bitmaps);
        var name = info is { } d && !string.IsNullOrEmpty(d.Title) ? d.Title : Path.GetFileName(themeDir);
        return new WinRarThemeProvider(name, size, fallback, icons);
    }

    private static List<(string Name, Bitmap Strip)> LoadBitmaps(string toolbarDir)
    {
        var result = new List<(string, Bitmap)>();
        foreach (var file in Directory.EnumerateFiles(toolbarDir, "*.bmp", SearchOption.AllDirectories))
        {
            try
            {
                using var fs = File.OpenRead(file);
                var strip = ThemeBitmapLoader.LoadStrip(fs);
                if (strip != null)
                    result.Add((Path.GetFileNameWithoutExtension(file), strip));
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Skipping unreadable bitmap {File}", file);
            }
        }
        return result;
    }

    private static Dictionary<IconKey, IImage> MapSlots(List<(string Name, Bitmap Strip)> bitmaps)
    {
        var icons = new Dictionary<IconKey, IImage>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, strip) in bitmaps)
        {
            var stem = StripSizeSuffix(name);
            if (stem is null)
                continue;

            var key = SlotKey(stem);
            if (key is null || usedNames.Contains(stem))
                continue;

            var iconsList = ThemeBitmapLoader.SplitStrip(strip, CountInStrip(strip));
            if (iconsList is { Count: > 0 })
            {
                icons[key.Value] = iconsList[0];
                usedNames.Add(stem);
            }
        }

        if (!icons.ContainsKey(IconKey.SortUp))
            icons[IconKey.SortUp] = icons.Values.FirstOrDefault(v => v is Bitmap b && b.PixelSize.Width <= 16)
                                    ?? icons.Values.FirstOrDefault()!;
        if (!icons.ContainsKey(IconKey.SortDown))
            icons[IconKey.SortDown] = icons[IconKey.SortUp];

        return icons;
    }

    private static int CountInStrip(Bitmap strip)
    {
        int width = strip.PixelSize.Width;
        int height = strip.PixelSize.Height;
        if (height <= 0)
            return 1;
        int count = width / height;
        return count is >= 1 and <= 16 ? count : 1;
    }

    private static string? StripSizeSuffix(string name)
    {
        var lower = name.ToLowerInvariant();
        int idx = lower.IndexOf("48x48", StringComparison.Ordinal);
        if (idx <= 0) idx = lower.IndexOf("32x32", StringComparison.Ordinal);
        if (idx <= 0) idx = lower.IndexOf("24x24", StringComparison.Ordinal);
        if (idx <= 0) idx = lower.IndexOf("16x16", StringComparison.Ordinal);
        if (idx > 0)
            name = name[..idx];
        return name;
    }

    private static IconKey? SlotKey(string name)
    {
        for (int i = 0; i < SlotNames.Length; i++)
        {
            if (SlotNames[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return SlotNames[i] switch
                {
                    "Open" => IconKey.Open,
                    "Add" => IconKey.Add,
                    "Extract" => IconKey.Extract,
                    "Test" => IconKey.Test,
                    "View" => IconKey.View,
                    "Delete" => IconKey.Delete,
                    "Find" => IconKey.Find,
                    "Info" => IconKey.Info,
                    _ => null,
                };
        }
        return null;
    }

    private static double PickToolbarSize(Dictionary<IconKey, IImage> icons, List<(string, Bitmap)> bitmaps)
    {
        double size = 24;
        foreach (var (name, strip) in bitmaps)
        {
            if (name.StartsWith("Add", StringComparison.OrdinalIgnoreCase))
            {
                size = strip.PixelSize.Height;
                break;
            }
        }
        if (size <= 0 || size > 64)
            size = 24;
        return size;
    }

    private static (string? Title, string? Version, string? Description)? ReadDescription(string themeDir)
    {
        var file = Directory.EnumerateFiles(themeDir, "*description*.txt", SearchOption.TopDirectoryOnly)
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

    private static string? FindDir(string root, string name)
        => Directory.EnumerateDirectories(root, name, SearchOption.AllDirectories).FirstOrDefault();

    private static void ExtractVfs(ArchiveNode node, string destRoot)
    {
        var stack = new Stack<ArchiveNode>();
        foreach (var child in node.Children)
            stack.Push(child);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            var target = Path.Combine(destRoot, current.FullPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (current.Type == NodeType.Directory)
            {
                Directory.CreateDirectory(target);
                foreach (var child in current.Children)
                    stack.Push(child);
                continue;
            }

            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            try
            {
                using var src = current.OpenRead();
                using var dst = File.Create(target);
                src.CopyTo(dst);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Skipping theme file {Path}", current.FullPath);
            }
        }
    }
}
