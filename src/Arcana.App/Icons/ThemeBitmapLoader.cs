using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Arcana.App.Icons;

/// <summary>
/// Decodes WinRAR 24-bit BMP toolbar strips into Avalonia bitmaps.
/// WinRAR bitmaps use a magenta chroma key (#FF00FF) that must be made transparent.
/// </summary>
public static class ThemeBitmapLoader
{
    private static readonly Vector Dpi = new(96, 96);

    public static Bitmap? LoadStrip(Stream stream)
    {
        if (stream is null || !stream.CanRead)
            return null;

        try
        {
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Splits a horizontal BMP strip into <paramref name="count"/> equal-width icons,
    /// converting the magenta chroma key to transparency.
    /// </summary>
    public static List<Bitmap>? SplitStrip(Bitmap strip, int count)
    {
        if (strip is null || count <= 0)
            return null;

        int width = strip.PixelSize.Width;
        int height = strip.PixelSize.Height;
        if (width % count != 0)
            return null;

        int iconWidth = width / count;
        var stripBytes = ReadBgra(strip, width, height);
        if (stripBytes == null)
            return null;

        var result = new List<Bitmap>(count);
        for (int i = 0; i < count; i++)
            result.Add(BuildIcon(stripBytes, width, height, i * iconWidth, iconWidth));
        return result;
    }

    private static byte[]? ReadBgra(Bitmap source, int width, int height)
    {
        try
        {
            var decode = new WriteableBitmap(
                new PixelSize(width, height), Dpi,
                PixelFormats.Bgra8888, AlphaFormat.Premul);
            using (var fb = decode.Lock())
            {
                source.CopyPixels(fb);
                var bytes = new byte[fb.RowBytes * height];
                Marshal.Copy(fb.Address, bytes, 0, bytes.Length);
                return bytes;
            }
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap BuildIcon(byte[] stripBgra, int stripWidth, int stripHeight,
                                    int offsetX, int iconWidth)
    {
        var output = new byte[iconWidth * stripHeight * 4];
        for (int y = 0; y < stripHeight; y++)
        {
            int srcRow = y * stripWidth * 4 + offsetX * 4;
            int dstRow = y * iconWidth * 4;
            for (int x = 0; x < iconWidth; x++)
            {
                int si = srcRow + x * 4;
                int di = dstRow + x * 4;
                byte b = stripBgra[si];
                byte g = stripBgra[si + 1];
                byte r = stripBgra[si + 2];

                if (r == 0xFF && g == 0x00 && b == 0xFF)
                {
                    output[di] = 0;
                    output[di + 1] = 0;
                    output[di + 2] = 0;
                    output[di + 3] = 0;
                }
                else
                {
                    output[di] = b;
                    output[di + 1] = g;
                    output[di + 2] = r;
                    output[di + 3] = 255;
                }
            }
        }

        return FromBgra(output, iconWidth, stripHeight);
    }

    private static Bitmap FromBgra(byte[] bgra, int width, int height)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            Dpi,
            PixelFormats.Bgra8888,
            AlphaFormat.Premul);

        using (var fb = bitmap.Lock())
        {
            Marshal.Copy(bgra, 0, fb.Address, bgra.Length);
        }

        return bitmap;
    }
}
