using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Labs.Gif;
using Avalonia.Media.Imaging;
using SharpCompress.Archives.Zip;
using Serilog;
using UglyToad.PdfPig;

namespace Arcana.App.Services;

public enum PreviewKind
{
    None,
    Text,
    Image,
    Gif,
    Hex
}

public sealed record PreviewResult(PreviewKind Kind, string Text, Bitmap? Image, string Info, string? Error = null, IGifSource? GifSource = null);

public class PreviewService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<PreviewService>();

    private const int MaxTextBytes = 262144;
    private const int MaxHexBytes = 65536;
    private const int MaxImageBytes = 33554432;

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
            ".pdf" or ".docx" or ".xlsx" or ".pptx" => PreviewKind.Text,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".ico" => PreviewKind.Image,
            ".gif" => PreviewKind.Gif,
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
                return LoadStructuredText(stream, fileName, size);
            case PreviewKind.Image:
                return LoadImage(stream, size);
            case PreviewKind.Gif:
                return LoadGif(stream, size);
            default:
                return LoadHex(stream, size);
        }
    }

    /// <summary>
    /// Routes text-kind previews: PDF/docx/xlsx/pptx get structured extractors,
    /// everything else reads raw bytes as text.
    /// </summary>
    private PreviewResult LoadStructuredText(Stream stream, string fileName, long size)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => LoadPdf(stream, size),
            ".docx" => LoadOfficeText(stream, size, "word/document.xml",
                s => s.Replace("</w:p>", "\n").Replace("</w:tab>", "\t"), "docx"),
            ".xlsx" => LoadOfficeText(stream, size, "xl/sharedStrings.xml",
                s => s.Replace("</t>", "\n"), "xlsx"),
            ".pptx" => LoadPptx(stream, size),
            _ => LoadText(stream, size),
        };
    }

    public PreviewResult LoadPdf(Stream stream, long size)
    {
        try
        {
            var sb = new StringBuilder();
            using var document = ReadDocument(stream, size);
            using var doc = PdfDocument.Open(document);
            var pageCount = doc.NumberOfPages;
            var pagesToRead = Math.Min(pageCount, 20);
            for (var p = 1; p <= pagesToRead && sb.Length < MaxTextBytes; p++)
            {
                if (p > 1)
                    sb.Append('\n');
                sb.Append($"[Page {p}]\n");
                sb.Append(doc.GetPage(p).Text);
            }

            var text = Truncate(sb.ToString(), MaxTextBytes);
            var info = $"{ByteFormat.Format(size)} · pdf · {pageCount} page(s)";
            if (string.IsNullOrWhiteSpace(text))
                info += " (no extractable text)";

            return new PreviewResult(PreviewKind.Text, text, null, info);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "PDF text preview failed");
            return new PreviewResult(PreviewKind.Hex, "(pdf preview failed)", null, ByteFormat.Format(size), ex.Message);
        }
    }

    public PreviewResult LoadOfficeText(Stream stream, long size, string entryName, Func<string, string>? normalize, string format)
    {
        try
        {
            using var document = ReadDocument(stream, size);
            using var archive = ZipArchive.OpenArchive(document);
            var entry = archive.Entries.FirstOrDefault(e => e.Key?.Equals(entryName, StringComparison.OrdinalIgnoreCase) == true);
            if (entry == null)
                return new PreviewResult(PreviewKind.Hex, $"(no {entryName} found)", null, ByteFormat.Format(size), $"missing entry: {entryName}");

            using var content = entry.OpenEntryStream();
            var xml = Encoding.UTF8.GetString(ReadUpTo(content, MaxTextBytes));
            var body = normalize != null ? normalize(xml) : xml;
            var text = Truncate(StripXmlTags(body), MaxTextBytes);

            var info = $"{ByteFormat.Format(size)} · {format}";
            if (entry.Size > MaxTextBytes) info += " (truncated)";
            if (string.IsNullOrWhiteSpace(text))
                info += " (no extractable text)";

            return new PreviewResult(PreviewKind.Text, text, null, info);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "{Format} preview failed", format);
            return new PreviewResult(PreviewKind.Hex, $"({format} preview failed)", null, ByteFormat.Format(size), ex.Message);
        }
    }

    public PreviewResult LoadPptx(Stream stream, long size)
    {
        try
        {
            using var document = ReadDocument(stream, size);
            using var archive = ZipArchive.OpenArchive(document);
            var slides = archive.Entries
                .Where(e => e.Key?.StartsWith("ppt/slides/", StringComparison.OrdinalIgnoreCase) == true
                            && e.Key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sb = new StringBuilder();
            foreach (var slide in slides.Take(100))
            {
                if (sb.Length >= MaxTextBytes)
                    break;
                using var content = slide.OpenEntryStream();
                var xml = Encoding.UTF8.GetString(ReadUpTo(content, MaxTextBytes));
                var texts = Regex.Matches(xml, "<a:t>(.*?)</a:t>", RegexOptions.Singleline | RegexOptions.NonBacktracking,
                    TimeSpan.FromSeconds(1)).Select(m => m.Groups[1].Value);
                sb.Append($"--- {slide.Key} ---\n");
                sb.Append(string.Join('\n', texts));
                sb.Append('\n');
            }

            var text = Truncate(sb.ToString(), MaxTextBytes);
            var info = $"{ByteFormat.Format(size)} · pptx · {slides.Count} slide(s)";
            return new PreviewResult(PreviewKind.Text, text, null, info);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "PPTX preview failed");
            return new PreviewResult(PreviewKind.Hex, "(pptx preview failed)", null, ByteFormat.Format(size), ex.Message);
        }
    }

    private static string StripXmlTags(string xml)
    {
        xml = Regex.Replace(xml, "<[^>]+>", "", RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1));
        return xml.Replace("&amp;", "&")
                  .Replace("&lt;", "<")
                  .Replace("&gt;", ">")
                  .Replace("&quot;", "\"")
                  .Replace("&apos;", "'");
    }

    private static string Truncate(string value, int maxBytes) =>
        value.Length <= maxBytes ? value : value[..maxBytes];

    public PreviewResult LoadGif(Stream stream, long size)
    {
        try
        {
            var bytes = ReadDocumentBytes(stream, size);
            if (bytes.Length < 10 || (long)System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6))
                * System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8)) > 16_000_000)
                throw new InvalidDataException("GIF dimensions exceed the preview limit.");
            var source = GifStreamSource.FromStream(new MemoryStream(bytes, writable: false));
            var info = $"{ByteFormat.Format(size)} · {source.Size.Width}×{source.Size.Height} · gif (animated)";
            return new PreviewResult(PreviewKind.Gif, "", null, info, GifSource: source);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "GIF preview decode failed");
            return new PreviewResult(PreviewKind.Hex, "(gif could not be decoded)", null, ByteFormat.Format(size), ex.Message);
        }
    }

    public PreviewResult LoadText(Stream stream, long size)
    {
        var bytes = ReadUpTo(stream, MaxTextBytes);
        var info = $"{ByteFormat.Format(size)} · text";
        if (size > MaxTextBytes)
            info += $" (truncated)";
        try { return new PreviewResult(PreviewKind.Text, DecodeText(bytes, size > MaxTextBytes), null, info); }
        catch (DecoderFallbackException ex)
        {
            Log.Warning(ex, "Text preview has an unsupported or invalid encoding");
            return new PreviewResult(PreviewKind.Text, "", null, info, "Text encoding could not be decoded. Use Binary Preview.");
        }
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
            if (size > MaxImageBytes)
                return new PreviewResult(PreviewKind.Hex, "(image too large to preview)", null, ByteFormat.Format(size));

            if (stream.CanSeek) stream.Position = 0;
            var bitmap = new Bitmap(stream);
            var info = $"{ByteFormat.Format(size)} · {bitmap.PixelSize.Width}×{bitmap.PixelSize.Height}";
            return new PreviewResult(PreviewKind.Image, "", bitmap, info);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Image preview decode failed");
            return new PreviewResult(PreviewKind.Hex, "(image could not be decoded)", null, ByteFormat.Format(size), ex.Message);
        }
    }

    private static byte[] ReadUpTo(Stream stream, int maxBytes)
    {
        if (stream.CanSeek) stream.Position = 0;
        var buffer = new byte[maxBytes];
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

    private static MemoryStream ReadDocument(Stream stream, long size) => new(ReadDocumentBytes(stream, size), writable: false);

    private static byte[] ReadDocumentBytes(Stream stream, long size)
    {
        if (size > MaxImageBytes) throw new InvalidDataException("Document exceeds the 32 MiB preview limit.");
        var bytes = ReadUpTo(stream, MaxImageBytes + 1);
        if (bytes.Length > MaxImageBytes) throw new InvalidDataException("Document exceeds the 32 MiB preview limit.");
        return bytes;
    }

    private static string DecodeText(byte[] bytes, bool truncated)
    {
        Encoding encoding = new UTF8Encoding(false, throwOnInvalidBytes: true);
        var offset = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) offset = 3;
        else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        { encoding = new UnicodeEncoding(false, false, true); offset = 2; }
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        { encoding = new UnicodeEncoding(true, false, true); offset = 2; }
        var chars = new char[encoding.GetMaxCharCount(bytes.Length - offset)];
        var count = encoding.GetDecoder().GetChars(bytes, offset, bytes.Length - offset, chars, 0, flush: !truncated);
        return new string(chars, 0, count);
    }
}
