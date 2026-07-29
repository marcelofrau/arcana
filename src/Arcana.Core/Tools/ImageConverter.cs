namespace Arcana.Core.Tools;

public class ImageConverter
{
    public void Convert(string sourcePath, string outputPath,
                        IProgress<int>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("ImageConverter.Convert");
    }

    public Task ConvertAsync(string sourcePath, string outputPath,
                             IProgress<int>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("ImageConverter.ConvertAsync");
    }
}
