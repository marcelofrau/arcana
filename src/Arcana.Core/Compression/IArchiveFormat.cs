namespace Arcana.Core.Compression;

public interface IArchiveFormat
{
    string Name { get; }
    string Extension { get; }
    bool CanRead { get; }
    bool CanWrite { get; }
    bool CanEncrypt { get; }
    bool SupportsSolid { get; }
    bool SupportsVolumes { get; }

    Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default);
    Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default);

    void Save(Archive archive, Stream stream, CompressionOptions options,
              IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                   IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
}
