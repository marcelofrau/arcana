using System.Collections.Generic;
using Avalonia.Media;
using Serilog;

namespace Arcana.App.Icons;

/// <summary>
/// Modern icon set rendered as vectors (DrawingImage). Material Design path data (Apache 2.0),
/// 24x24 coordinate space. Used as fallback for any slot a WinRAR theme does not supply.
/// </summary>
public sealed class DefaultIconProvider : IIconProvider
{
    private static readonly ILogger Log = Serilog.Log.ForContext<DefaultIconProvider>();

    public const double NativeSize = 24;

    public string Name => PapirusIconProvider.MaterialName;
    public double ToolbarSize => NativeSize;

    private static readonly IReadOnlyDictionary<IconKey, string> Paths =
        new Dictionary<IconKey, string>
        {
            [IconKey.Open] = "M20.55 5.22l-1.39-1.68C18.88 3.21 18.47 3 18 3H6c-.47 0-.88.21-1.15.55L3.46 5.22C3.17 5.57 3 6.01 3 6.5V19c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V6.5c0-.49-.17-.93-.45-1.28zM12 9.5l5.5 5.5H14v2h-4v-2H6.5L12 9.5zM5.12 5l.82-1h12l.93 1H5.12z",
            [IconKey.Add] = "M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z",
            [IconKey.Extract] = "M20 12l-1.41-1.41L13 16.17V4h-2v12.17l-5.58-5.59L4 12l8 8 8-8z",
            [IconKey.Test] = "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z",
            [IconKey.View] = "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z",
            [IconKey.Delete] = "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z",
            [IconKey.Find] = "M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z",
            [IconKey.Info] = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z",
            [IconKey.FileGeneric] = "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z",
            [IconKey.FileArchive] = "M20.54 5.23l-1.39-1.68C18.88 3.21 18.47 3 18 3H6c-.47 0-.88.21-1.15.55L3.46 5.23C3.17 5.57 3 6.02 3 6.5V19c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V6.5c0-.48-.17-.93-.46-1.27zM6 19v-9h12v9H6z",
            [IconKey.FileImage] = "M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z",
            [IconKey.FileCode] = "M9.4 16.6L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zm5.2 0l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z",
            [IconKey.FileMedia] = "M18 3v2h-2V3H8v2H6V3H4v18h2v-2h2v2h8v-2h2v2h2V3h-2zM6 17v-8h12v8H6z",
            [IconKey.FileDoc] = "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z",
            [IconKey.Folder] = "M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z",
            [IconKey.Rar] = "M20.54 5.23l-1.39-1.68C18.88 3.21 18.47 3 18 3H6c-.47 0-.88.21-1.15.55L3.46 5.23C3.17 5.57 3 6.02 3 6.5V19c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V6.5c0-.48-.17-.93-.46-1.27zM6 19v-9h12v9H6z",
            [IconKey.SortUp] = "M7 14l5-5 5 5z",
            [IconKey.SortDown] = "M7 10l5 5 5-5z",
            [IconKey.Save] = "M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z",
            [IconKey.Close] = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z",
            [IconKey.Settings] = "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z",
            [IconKey.Help] = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm2.07-7.75l-.9.92C13.45 12.9 13 13.5 13 15h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H8c0-2.21 1.79-4 4-4s4 1.79 4 4c0 .88-.36 1.68-.93 2.25z",
            [IconKey.Split] = "M14 4l2.29 2.29-2.88 2.88 1.42 1.42 2.88-2.88L20 10V4zm-4 0H4v6l2.29-2.29 4.71 4.7V20h2v-8.41l-5.29-5.3z",
            [IconKey.Convert] = "M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z",
            [IconKey.Password] = "M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3.1-9H8.9V6c0-1.71 1.39-3.1 3.1-3.1 1.71 0 3.1 1.39 3.1 3.1v2z",
            [IconKey.Rename] = "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z",
            [IconKey.SelectAll] = "M3 5h2V3c-1.1 0-2 .9-2 2zm0 8h2v-2H3v2zm4 8h2v-2H7v2zM3 9h2V7H3v2zm8-6h2V1h-2v2zm-4 0h2V1H7v2zm4 12h2v-2h-2v2zm8-8h2V7h-2v2zm0 4h2v-2h-2v2zm0-10v2h2c0-1.1-.9-2-2-2zm0 12h2v-2h-2v2zm-8 4h2v-2h-2v2zm4-4h2v-2h-2v2zm4 4h2c0-1.1-.9-2-2-2v2zM7 19h2v-2H7v2zm8 2h2v-2h-2v2zm-8-6h2v-2H7v2zm0 4h2v-2H7v2zm4-16h2V1h-2v2zm4 0h2V1h-2v2z",
            [IconKey.Compare] = "M9.01 14H2v2h7.01v3L13 15l-3.99-4v3zm5.98-1v-3H22V8h-7.01V5L11 9l3.99 4z",
            [IconKey.Favorite] = "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z",
            [IconKey.Join] = "M17 20.41L18.41 19 15 15.59 13.59 17 17 20.41zM7.5 8H11v5.59L5.59 19 7 20.41l6-6V8h3.5L12 3.5 7.5 8z",
            [IconKey.Hash] = "M1 6h2v12H1V6zm4 0h2v12H5V6zm4 0h3v12H9V6zm5 0h1v12h-1V6zm3 0h2v12h-2V6zm3 0h1v12h-1V6z",
            [IconKey.Optimize] = "M19 12h-1V4h-2v8h-1l3 4 3-4h-1V4h-2v8zM12 4H5v6h7v2l4-5-4-5v2zm0 12H5v6h7v2l4-5-4-5v2z",
        };

    private readonly Dictionary<IconKey, DrawingImage> _cache = new();
    private readonly object _lock = new();
    private Color _glyphColor = Color.Parse("#E4E4EE");

    public IImage? GetIcon(IconKey key)
    {
        if (!Paths.TryGetValue(key, out var data))
        {
            Log.Warning("No Material path defined for icon {Key}", key);
            return null;
        }

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            Log.Verbose("Building Material glyph for {Key}", key);
            var geometry = Geometry.Parse(data);
            var drawing = new GeometryDrawing
            {
                Brush = new SolidColorBrush(_glyphColor),
                Geometry = geometry
            };
            var image = new DrawingImage { Drawing = drawing };
            _cache[key] = image;
            return image;
        }
    }

    /// <summary>
    /// Recolors every cached glyph so Material fallback icons stay readable when
    /// the color theme switches (e.g. dark glyphs on the light retro themes).
    /// </summary>
    public void UpdateGlyphColor(Color color)
    {
        lock (_lock)
        {
            _glyphColor = color;
            foreach (var image in _cache.Values)
            {
                if (image.Drawing is GeometryDrawing drawing)
                    drawing.Brush = new SolidColorBrush(color);
            }
        }
        Log.Debug("Recolored {Count} Material glyphs to {Color}", _cache.Count, color);
    }
}
