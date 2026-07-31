using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Arcana.App.Icons;

/// <summary>
/// Built-in "Papirus" icon theme: PNGs rendered from the Papirus icon theme
/// (https://github.com/PapirusDevelopmentTeam/papirus-icon-theme, GPL-3.0).
/// Rasterized at 48px and displayed at 48px for crisp, high-DPI icons.
/// </summary>
public sealed class PapirusIconProvider : IIconProvider
{
    public const string BuiltInName = "Papirus";
    public const string MaterialName = "Material";

    public string Name => BuiltInName;
    public double ToolbarSize => 48;

    private static readonly IReadOnlyDictionary<IconKey, string> Slots =
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
            [IconKey.Folder] = "folder",
            [IconKey.FileGeneric] = "file-generic",
            [IconKey.FileArchive] = "file-archive",
            [IconKey.FileImage] = "file-image",
            [IconKey.FileCode] = "file-code",
            [IconKey.FileMedia] = "file-media",
            [IconKey.FileDoc] = "file-doc",
            [IconKey.Rar] = "file-rar",
            [IconKey.SortUp] = "sort-up",
            [IconKey.SortDown] = "sort-down",
        };

    private readonly DefaultIconProvider _fallback;
    private readonly Dictionary<IconKey, Bitmap> _cache = new();
    private readonly object _lock = new();

    public PapirusIconProvider(DefaultIconProvider fallback)
    {
        _fallback = fallback;
    }

    public IImage? GetIcon(IconKey key)
    {
        if (!Slots.TryGetValue(key, out var file))
            return _fallback.GetIcon(key);

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            using var stream = AssetLoader.Open(new Uri($"avares://Arcana.App/Assets/Papirus/{file}.png"));
            var bitmap = new Bitmap(stream);
            _cache[key] = bitmap;
            return bitmap;
        }
    }
}
