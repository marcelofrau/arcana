using System.IO;
using System.Text;
using FluentAssertions;
using Arcana.App.Services;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace Arcana.App.Tests.Preview;

public class PreviewServiceTests
{
    private readonly PreviewService _sut = new();

    [Fact]
    public void InvalidTextEncoding_ShowsErrorInsteadOfGuessingLatin1()
    {
        using var input = new MemoryStream([0xC3, 0x28]);
        _sut.LoadText(input, input.Length).Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void OfficeXmlExpansion_IsBounded()
    {
        using var zip = BuildZip(("word/document.xml", "<w:t>" + new string('a', 1024 * 1024) + "</w:t>"));
        var result = _sut.LoadOfficeText(zip, zip.Length, "word/document.xml", null, "docx");
        result.Text.Length.Should().BeLessThanOrEqualTo(262144);
        result.Info.Should().Contain("truncated");
    }

    [Theory]
    [InlineData("report.pdf", PreviewKind.Text)]
    [InlineData("letter.docx", PreviewKind.Text)]
    [InlineData("sheet.xlsx", PreviewKind.Text)]
    [InlineData("deck.pptx", PreviewKind.Text)]
    [InlineData("photo.png", PreviewKind.Image)]
    [InlineData("anim.gif", PreviewKind.Gif)]
    [InlineData("data.bin", PreviewKind.Hex)]
    public void DetectKind_ShouldClassifyByExtension(string fileName, PreviewKind expected)
    {
        _sut.DetectKind(fileName).Should().Be(expected);
    }

    [Fact]
    public void LoadOfficeText_Docx_ShouldExtractParagraphText()
    {
        using var zip = BuildZip(("word/document.xml",
            "<w:document><w:body><w:p><w:r><w:t>Hello World</w:t></w:r></w:p>" +
            "<w:p><w:r><w:t>Second &amp; line</w:t></w:r></w:p></w:body></w:document>"));

        var result = _sut.LoadOfficeText(zip, zip.Length, "word/document.xml",
            s => s.Replace("</w:p>", "\n"), "docx");

        result.Kind.Should().Be(PreviewKind.Text);
        result.Text.Should().Contain("Hello World");
        result.Text.Should().Contain("Second & line");
        result.Info.Should().Contain("docx");
    }

    [Fact]
    public void LoadOfficeText_MissingEntry_ShouldFailWithHexFallback()
    {
        using var zip = BuildZip(("other/file.bin", "x"));

        var result = _sut.LoadOfficeText(zip, zip.Length, "word/document.xml", null, "docx");

        result.Kind.Should().Be(PreviewKind.Hex);
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void LoadPdf_ShouldExtractTextAndPageCount()
    {
        using var pdf = BuildPdf("Hello PDF World");

        var result = _sut.LoadPdf(pdf, pdf.Length);

        result.Kind.Should().Be(PreviewKind.Text);
        result.Text.Should().Contain("Hello PDF World");
        result.Info.Should().Contain("1 page");
    }

    private static MemoryStream BuildPdf(string text)
    {
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        var offsets = new List<int>();
        void Obj(int n, string body)
        {
            offsets.Add(sb.Length);
            sb.Append($"{n} 0 obj\n").Append(body).Append("\nendobj\n");
        }

        Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        var streamData = $"BT /F1 24 Tf 72 700 Td ({text}) Tj ET";
        Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
        Obj(4, $"<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream");
        Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var xrefPos = sb.Length;
        sb.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var o in offsets)
            sb.Append(o.ToString("D10")).Append(" 00000 n \n");
        sb.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n").Append(xrefPos).Append("\n%%EOF");

        return new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
    }

    private static MemoryStream BuildZip(params (string Key, string Content)[] entries)
    {
        var ms = new MemoryStream();
        using (var writer = WriterFactory.OpenWriter(ms, ArchiveType.Zip, new WriterOptions(CompressionType.None)))
        {
            foreach (var (key, content) in entries)
                writer.Write(key, new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
        ms.Position = 0;
        return ms;
    }
}
