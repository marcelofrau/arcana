using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Serilog;

namespace Arcana.App.Icons;

/// <summary>
/// Generic built-in icon theme backed by committed PNGs (rasterized at 48px via
/// tools/IconTool, see build/update-*-icons.ps1). Slots it does not cover fall
/// back to the Material vector set. Used for the Numix, Tango and La Capitaine
/// themes.
/// </summary>
public sealed class PngIconThemeProvider : IIconProvider
{
    private static readonly ILogger Log = Serilog.Log.ForContext<PngIconThemeProvider>();

    public const string NumixName = "Numix";
    public const string TangoName = "Tango";
    public const string LaCapitaineName = "La Capitaine";

    public string Name { get; }
    public double ToolbarSize { get; }

    private readonly string _assetDir;
    private readonly IReadOnlyDictionary<IconKey, string> _slots;
    private readonly DefaultIconProvider _fallback;
    private readonly Dictionary<IconKey, Bitmap> _cache = new();
    private readonly object _lock = new();

    public PngIconThemeProvider(
        string name,
        string assetDir,
        IReadOnlyDictionary<IconKey, string> slots,
        DefaultIconProvider fallback,
        double toolbarSize = 48)
    {
        Name = name;
        _assetDir = assetDir;
        _slots = slots;
        _fallback = fallback;
        ToolbarSize = toolbarSize;
    }

    public IImage? GetIcon(IconKey key)
    {
        if (!_slots.TryGetValue(key, out var file))
        {
            Log.Verbose("{Theme} has no slot for {Key}; falling back to Material", Name, key);
            return _fallback.GetIcon(key);
        }

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            Log.Verbose("Loading {Theme} icon {File} for {Key}", Name, file, key);
            using var stream = AssetLoader.Open(new Uri($"avares://Arcana.App/Assets/{_assetDir}/{file}.png"));
            var bitmap = new Bitmap(stream);
            _cache[key] = bitmap;
            return bitmap;
        }
    }
}
