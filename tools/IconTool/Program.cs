using System.Drawing;
using System.Drawing.Imaging;
using Svg;

if (args.Length < 3)
{
    Console.Error.WriteLine("usage: IconTool <srcDir> <outDir> <size>");
    return 1;
}

var srcDir = args[0];
var outDir = args[1];
var size = int.Parse(args[2]);

Directory.CreateDirectory(outDir);

var rendered = 0;
var failed = 0;
foreach (var svgPath in Directory.EnumerateFiles(srcDir, "*.svg"))
{
    var name = Path.GetFileNameWithoutExtension(svgPath);
    var outPath = Path.Combine(outDir, name + ".png");
    try
    {
        var doc = SvgDocument.Open(svgPath);
        using var bitmap = doc.Draw(size, size);
        bitmap.Save(outPath, ImageFormat.Png);
        rendered++;
        Console.WriteLine($"ok  {name}.png ({size}x{size})");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

Console.WriteLine($"rendered={rendered} failed={failed} -> {outDir}");
return failed == 0 ? 0 : 2;
