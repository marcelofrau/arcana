using System;
using System.IO;
using System.Text;
using Avalonia.Media.Imaging;
using Serilog;

namespace Arcana.App.Services;

public enum PreviewKind
{
    None,
    Text,
    Image,
    Hex
}

public sealed record PreviewResult(PreviewKind Kind, string Text, Bitmap? Image, string Info);

public class PreviewService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<PreviewService>();

    private const int MaxTextBytes = 262144;
    private const int MaxHexBytes = 65536;

    public PreviewKind DetectKind(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".txt" or ".md" or ".xml" or ".json" or ".csv" or ".cs" or ".js" or ".ts"
                or ".html" or ".css" or ".py" or ".sh" or ".yaml" or ".yml" or ".ini"
                or ".cfg" or ".log" or ".sql" or ".bat" or ".ps1" or ".toml" or ".xaml"
                or ".axaml" or ".sln" or ".csproj" or ".props" or ".targets" or ".gitignore"
                or ".editorconfig" or ".license" or ".nuspec" => PreviewKind.Text,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico" => PreviewKind.Image,
            _ => PreviewKind.Hex,
        };
    }

    public PreviewResult LoadPreview(Stream stream, string fileName, long size)
    {
        var kind = DetectKind(fileName);
        Log.Debug("Loading preview for {File} ({Size} bytes, kind {Kind})", fileName, size, kind);
        switch (kind)
        {
            case PreviewKind.Text:
                return LoadText(stream, size);
            case PreviewKind.Image:
                return LoadImage(stream, size);
            default:
                return LoadHex(stream, size);
        }
    }

    public PreviewResult LoadText(Stream stream, long size)
    {
        var bytes = ReadUpTo(stream, MaxTextBytes);
        var text = DecodeText(bytes);

        var info = $"{ByteFormat.Format(size)} · text";
        if (size > MaxTextBytes)
            info += $" (truncated)";

        return new PreviewResult(PreviewKind.Text, text, null, info);
    }

    public PreviewResult LoadHex(Stream stream, long size)
    {
        var bytes = ReadUpTo(stream, MaxHexBytes);
        var sb = new StringBuilder(bytes.Length * 3);

        for (int i = 0; i < bytes.Length; i += 16)
        {
            sb.Append($"{i:X8}  ");
            for (int j = 0; j < 16; j++)
            {
                if (i + j < bytes.Length)
                    sb.Append($"{bytes[i + j]:X2} ");
                else
                    sb.Append("   ");
            }
            sb.Append(' ');
            for (int j = 0; j < 16 && i + j < bytes.Length; j++)
            {
                var b = bytes[i + j];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }
            sb.AppendLine();
        }

        var info = $"{ByteFormat.Format(size)} · binary";
        if (size > MaxHexBytes)
            info += " (truncated)";

        return new PreviewResult(PreviewKind.Hex, sb.ToString(), null, info);
    }

    public PreviewResult LoadImage(Stream stream, long size)
    {
        try
        {
            stream.Position = 0;
            var bitmap = new Bitmap(stream);
            var info = $"{ByteFormat.Format(size)} · {bitmap.PixelSize.Width}×{bitmap.PixelSize.Height}";
            return new PreviewResult(PreviewKind.Image, "", bitmap, info);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Image preview decode failed");
            return new PreviewResult(PreviewKind.Hex, "(image could not be decoded)", null, ByteFormat.Format(size));
        }
    }

    private static byte[] ReadUpTo(Stream stream, int maxBytes)
    {
        stream.Position = 0;
        var buffer = new byte[Math.Min(stream.Length, maxBytes)];
        var read = 0;
        while (read < buffer.Length)
        {
            var n = stream.Read(buffer, read, buffer.Length - read);
            if (n <= 0)
                break;
            read += n;
        }
        if (read < buffer.Length)
            Array.Resize(ref buffer, read);
        return buffer;
    }

    private static string DecodeText(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        try
        {
            var utf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);
            return utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }
}
