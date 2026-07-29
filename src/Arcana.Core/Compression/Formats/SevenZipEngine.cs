namespace Arcana.Core.Compression.Formats;

public class SevenZipEngine : IArchiveFormat
{
    public string Name => "7z";
    public string Extension => ".7z";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => true;
    public bool SupportsSolid => true;
    public bool SupportsVolumes => true;

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        throw new NotImplementedException("SevenZipEngine.OpenAsync");
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => SaveAsync(archive, stream, options, progress, ct).GetAwaiter().GetResult();

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("SevenZipEngine.SaveAsync");
    }
}
