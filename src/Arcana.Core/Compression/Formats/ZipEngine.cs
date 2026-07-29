namespace Arcana.Core.Compression.Formats;

public class ZipEngine : IArchiveFormat
{
    public string Name => "ZIP";
    public string Extension => ".zip";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => true;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => false;

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        // Stub: will use SharpCompress to read ZIP entries
        throw new NotImplementedException("ZipEngine.OpenAsync");
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => SaveAsync(archive, stream, options, progress, ct).GetAwaiter().GetResult();

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        // Stub: will use SharpCompress to write ZIP
        throw new NotImplementedException("ZipEngine.SaveAsync");
    }
}
