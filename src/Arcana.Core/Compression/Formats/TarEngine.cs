using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.Xz;
using SharpCompress.Writers;
using Serilog;
using ZstdNet;

namespace Arcana.Core.Compression.Formats;

public class TarEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<TarEngine>();
    public string Name => "Tar";
    public string Extension => ".tar";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => false;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => true;

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        try
        {
            var detectedFormat = DetectFormat(path);
            _log.Debug("Detected tar format {Format} for {Path}", detectedFormat, path);
            using var tarStream = await DecompressToMemoryAsync(stream, detectedFormat, ct).ConfigureAwait(false);
            using var archive = (IArchive)(object)TarArchive.OpenArchive(tarStream, null);

            var entries = new List<ArchiveEntry>();
            var vfs = new Filesystem.VirtualFileSystem();

            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = entry.Key ?? "";

                if (entry.IsDirectory)
                {
                    vfs.AddDirectory(key);
                    entries.Add(new ArchiveEntry
                    {
                        Path = key,
                        Name = key.Trim('/').Split('/').Last(),
                        Size = entry.Size,
                        CompressedSize = entry.CompressedSize,
                        IsDirectory = true,
                        LastModified = NormalizeTimestamp(entry.LastModifiedTime),
                    });
                    continue;
                }

                await using var entryStream = entry.OpenEntryStream();
                var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms, ct).ConfigureAwait(false);
                ms.Position = 0;

                vfs.AddFile(key, ms);

                entries.Add(new ArchiveEntry
                {
                    Path = key,
                    Name = System.IO.Path.GetFileName(key),
                    Size = entry.Size,
                    CompressedSize = entry.CompressedSize,
                    Crc32 = (uint)entry.Crc,
                    IsDirectory = false,
                    LastModified = NormalizeTimestamp(entry.LastModifiedTime),
                });
            }

            _log.Information("Opened {Path} with {EntryCount} entries as format {Format}", path, entries.Count, detectedFormat);
            return new Archive
            {
                Format = detectedFormat,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open tar archive {Path}", path);
            throw;
        }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => SaveAsync(archive, stream, options, progress, ct).GetAwaiter().GetResult();

    public async Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                                IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        _log.Verbose("Saving archive with {FileCount} files, format {Format}", archive.Vfs.Root.Children.Count, options.Format);
        try
        {
            if (options.Format == CompressionFormat.TarXz)
            {
                _log.Warning("Xz-compressed tar writing is not supported");
                throw new NotSupportedException("Xz-compressed tar writing is not supported");
            }

            if (options.Format == CompressionFormat.TarZstd)
            {
                _log.Debug("Saving tar archive with Zstd compression");
                using var ms = new MemoryStream();
                {
                    using var tarWriter = WriterFactory.OpenWriter(ms, ArchiveType.Tar,
                        new WriterOptions(CompressionType.None) { LeaveStreamOpen = true });
                    await WriteNodesAsync(tarWriter, archive.Vfs.Root, progress, ct).ConfigureAwait(false);
                }
                ms.Position = 0;
                var raw = ms.ToArray();
                using var compressor = new Compressor(new ZstdNet.CompressionOptions(3));
                var compressed = compressor.Wrap(raw);
                await stream.WriteAsync(compressed, ct).ConfigureAwait(false);

                _log.Information("Saved tar archive to stream with Zstd compression, size {Size} bytes", compressed.Length);
                return;
            }

            var (archiveType, compressionType) = GetFormatSettings(options.Format);
            _log.Debug("Saving tar with archiveType={ArchiveType}, compressionType={CompressionType}", archiveType, compressionType);
            using var writer = WriterFactory.OpenWriter(stream, archiveType,
                new WriterOptions(compressionType) { LeaveStreamOpen = true });
            await WriteNodesAsync(writer, archive.Vfs.Root, progress, ct).ConfigureAwait(false);

            _log.Information("Saved tar archive to stream");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save tar archive");
            throw;
        }
    }

    private static async Task<MemoryStream> DecompressToMemoryAsync(Stream stream, CompressionFormat format, CancellationToken ct)
    {
        var result = new MemoryStream();

        if (format == CompressionFormat.Tar)
        {
            await stream.CopyToAsync(result, ct).ConfigureAwait(false);
        }
        else if (format == CompressionFormat.TarGz)
        {
            using var gz = new GZipStream(stream, CompressionMode.Decompress);
            await gz.CopyToAsync(result, ct).ConfigureAwait(false);
        }
        else if (format == CompressionFormat.TarBz2)
        {
            using var bz2 = BZip2Stream.Create(stream, CompressionMode.Decompress, false, false, false);
            await bz2.CopyToAsync(result, ct).ConfigureAwait(false);
        }
        else if (format == CompressionFormat.TarXz)
        {
            using var xz = new XZStream(stream);
            await xz.CopyToAsync(result, ct).ConfigureAwait(false);
        }
        else if (format == CompressionFormat.TarZstd)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
            var raw = ms.ToArray();
            using var decompressor = new Decompressor();
            var uncompressed = decompressor.Unwrap(raw);
            await result.WriteAsync(uncompressed, ct).ConfigureAwait(false);
        }

        result.Position = 0;
        return result;
    }

    private static async Task WriteNodesAsync(IWriter writer, Filesystem.ArchiveNode node,
        IProgress<ProgressReport>? progress, CancellationToken ct)
    {
        var totalFiles = CountFiles(node);
        var filesDone = 0;

        foreach (var child in node.Children)
        {
            ct.ThrowIfCancellationRequested();
            var entryPath = child.FullPath.TrimStart('/');

            if (child.Type == Filesystem.NodeType.Directory)
            {
                writer.WriteDirectory(entryPath, child.LastModified);
                await WriteNodesAsync(writer, child, progress, ct).ConfigureAwait(false);
            }
            else
            {
                await using var content = child.OpenRead();
                writer.Write(entryPath, content, child.LastModified);

                filesDone++;
                progress?.Report(new ProgressReport
                {
                    CurrentFile = child.Name,
                    FilesProcessed = filesDone,
                    TotalFiles = totalFiles,
                    CurrentOperation = "Compressing",
                });
            }
        }
    }

    private static int CountFiles(Filesystem.ArchiveNode node)
    {
        var count = 0;
        foreach (var child in node.Children)
            count += child.Type == Filesystem.NodeType.File ? 1 : CountFiles(child);
        return count;
    }

    private static (ArchiveType, CompressionType) GetFormatSettings(CompressionFormat format) => format switch
    {
        CompressionFormat.Tar => (ArchiveType.Tar, CompressionType.None),
        CompressionFormat.TarGz => (ArchiveType.Tar, CompressionType.GZip),
        CompressionFormat.TarBz2 => (ArchiveType.Tar, CompressionType.BZip2),
        _ => (ArchiveType.Tar, CompressionType.None),
    };

    private static CompressionFormat DetectFormat(string path)
    {
        var lower = path.ToLowerInvariant();
        if (lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz")) return CompressionFormat.TarGz;
        if (lower.EndsWith(".tar.bz2") || lower.EndsWith(".tbz2")) return CompressionFormat.TarBz2;
        if (lower.EndsWith(".tar.xz") || lower.EndsWith(".txz")) return CompressionFormat.TarXz;
        if (lower.EndsWith(".tar.zst") || lower.EndsWith(".tzst")) return CompressionFormat.TarZstd;
        return CompressionFormat.Tar;
    }

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
