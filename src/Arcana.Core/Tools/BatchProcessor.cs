using Arcana.Core.Compression;

namespace Arcana.Core.Tools;

public class BatchProcessor
{
    public void ProcessBatch(IEnumerable<string> files, Func<string, Task> operation,
                             int maxParallel = 4, IProgress<ProgressReport>? progress = null,
                             CancellationToken ct = default)
    {
        throw new NotImplementedException("BatchProcessor.ProcessBatch");
    }

    public Task ProcessBatchAsync(IEnumerable<string> files, Func<string, Task> operation,
                                  int maxParallel = 4, IProgress<ProgressReport>? progress = null,
                                  CancellationToken ct = default)
    {
        throw new NotImplementedException("BatchProcessor.ProcessBatchAsync");
    }
}
