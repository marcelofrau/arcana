using Arcana.Core.Compression;
using Arcana.Core.Filesystem;

namespace Arcana.App.Services;

public class ArchiveService
{
    public Archive? CurrentArchive { get; private set; }

    public async Task<Archive> OpenAsync(string path, CancellationToken ct = default)
    {
        CurrentArchive = await ArchiveFactory.OpenAsync(path, AccessMode.Read, ct);
        return CurrentArchive;
    }

    public async Task SaveAsync(Archive archive, string path, CompressionOptions options,
                                IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        await using var stream = File.Create(path);
        await archive.FormatEngine.SaveAsync(archive, stream, options, progress, ct);
    }

    public void Close()
    {
        CurrentArchive?.Dispose();
        CurrentArchive = null;
    }
}
