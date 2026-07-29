namespace Arcana.App.Services;

public class PreviewService
{
    public enum PreviewType
    {
        Text,
        Image,
        Hex,
        Markdown,
        Unsupported
    }

    public PreviewType DetectType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".txt" or ".md" or ".xml" or ".json" or ".csv" or ".cs" or ".js" or ".ts"
                or ".html" or ".css" or ".py" or ".sh" or ".yaml" or ".yml" or ".ini"
                or ".cfg" or ".log" or ".sql" or ".bat" or ".ps1" => PreviewType.Text,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" => PreviewType.Image,
            _ => PreviewType.Hex
        };
    }

    public bool CanEdit(string fileName)
    {
        var type = DetectType(fileName);
        return type is PreviewType.Text or PreviewType.Hex;
    }
}
