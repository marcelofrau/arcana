using Arcana.Core.Compression;

namespace Arcana.Core.Tools;

public class FileSplitter
{
    public void Split(string sourcePath, string outputDir, long partSize,
                      IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("FileSplitter.Split");
    }

    public Task SplitAsync(string sourcePath, string outputDir, long partSize,
                           IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("FileSplitter.SplitAsync");
    }
}
