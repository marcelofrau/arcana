namespace Arcana.Core.Compression;

public interface ICompressionEngine
{
    string Name { get; }
    CompressionLevel MinLevel { get; }
    CompressionLevel MaxLevel { get; }
    bool SupportsParallel { get; }

    void Compress(Stream source, Stream destination, CompressionLevel level,
                  IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    Task CompressAsync(Stream source, Stream destination, CompressionLevel level,
                       IProgress<ProgressReport>? progress = null, CancellationToken ct = default);

    void Decompress(Stream source, Stream destination,
                    IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
    Task DecompressAsync(Stream source, Stream destination,
                         IProgress<ProgressReport>? progress = null, CancellationToken ct = default);
}
