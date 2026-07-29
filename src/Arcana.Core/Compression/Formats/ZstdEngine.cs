namespace Arcana.Core.Compression.Formats;

public class ZstdEngine : IArchiveFormat
{
    public string Name => "Zstandard";
    public string Extension => ".zst";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => true;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => false;

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        throw new NotImplementedException("ZstdEngine.OpenAsync");
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => SaveAsync(archive, stream, options, progress, ct).GetAwaiter().GetResult();

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("ZstdEngine.SaveAsync");
    }
}
