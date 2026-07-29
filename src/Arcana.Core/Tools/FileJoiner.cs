using Arcana.Core.Compression;

namespace Arcana.Core.Tools;

public class FileJoiner
{
    public void Join(IEnumerable<string> parts, string outputPath,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("FileJoiner.Join");
    }

    public Task JoinAsync(IEnumerable<string> parts, string outputPath,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("FileJoiner.JoinAsync");
    }
}
