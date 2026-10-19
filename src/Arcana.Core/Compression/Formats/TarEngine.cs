using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors.Xz;
using SharpCompress.Writers;
using Serilog;
using ZstdSharp;

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
        var vfs = new Filesystem.VirtualFileSystem();
        try
        {
            var detectedFormat = DetectFormat(path);
            _log.Debug("Detected tar format {Format} for {Path}", detectedFormat, path);
            using var tarStream = await DecompressToDiskAsync(stream, detectedFormat, ct).ConfigureAwait(false);
            using var archive = (IArchive)(object)TarArchive.OpenArchive(tarStream, null);

            var entries = new List<ArchiveEntry>();

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
                await vfs.AddSpoolFileAsync(key, entryStream, ct).ConfigureAwait(false);

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
            vfs.Dispose();
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
                _log.Debug("Writing TAR with managed XZ compression");
                using var xz = new FileFormat.Xz.XzStream(stream,
                    global::Compression.Core.Streams.CompressionStreamMode.Compress, leaveOpen: true);
                using var tarWriter = WriterFactory.OpenWriter(xz, ArchiveType.Tar,
                    new WriterOptions(CompressionType.None) { LeaveStreamOpen = true });
                await WriteNodesAsync(tarWriter, archive.Vfs.Root, progress, ct).ConfigureAwait(false);
                return;
            }

            if (options.Format == CompressionFormat.TarZstd)
            {
                _log.Debug("Saving tar archive with Zstd compression");
                using var compressor = ZstdEngine.CreateCompressor(stream, options);
                using var tarWriter = WriterFactory.OpenWriter(compressor, ArchiveType.Tar,
                    new WriterOptions(CompressionType.None) { LeaveStreamOpen = true });
                await WriteNodesAsync(tarWriter, archive.Vfs.Root, progress, ct).ConfigureAwait(false);
                _log.Information("Saved TAR + Zstandard stream");
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

    private static async Task<Stream> DecompressToDiskAsync(Stream stream, CompressionFormat format, CancellationToken ct)
    {
        var result = Filesystem.ArchiveTemporaryFile.Create();
        try
        {
            using Stream input = format switch
            {
                CompressionFormat.Tar => new Filesystem.NonDisposingReadStream(stream),
                CompressionFormat.TarGz => new GZipStream(stream, CompressionMode.Decompress),
                CompressionFormat.TarBz2 => BZip2Stream.Create(stream, CompressionMode.Decompress, false, false, false),
                CompressionFormat.TarXz => new XZStream(stream),
                CompressionFormat.TarZstd => new DecompressionStream(stream),
                _ => throw new NotSupportedException($"Unsupported TAR wrapper: {format}"),
            };
            await ArchiveContent.CopyBoundedAsync(input, result, ArchiveLimits.MaxExpandedBytes, ct).ConfigureAwait(false);
            result.Position = 0;
            return result;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not spool TAR content");
            await result.DisposeAsync();
            throw;
        }
    }

    private static async Task WriteNodesAsync(IWriter writer, Filesystem.ArchiveNode node,
        IProgress<ProgressReport>? progress, CancellationToken ct)
    {
        var nodes = Filesystem.VirtualFileSystem.Walk(node).Skip(1).ToList();
        var totalFiles = nodes.Count(n => n.Type == Filesystem.NodeType.File);
        var filesDone = 0;

        foreach (var child in nodes)
        {
            ct.ThrowIfCancellationRequested();
            var entryPath = child.FullPath.TrimStart('/');

            if (child.Type == Filesystem.NodeType.Directory)
            {
                writer.WriteDirectory(entryPath, child.LastModified);
            }
            else
            {
                await using var content = new ProgressStream(child.OpenRead(), progress, child.Name, child.OriginalSize,
                    filesDone, totalFiles, "Compressing", cancellationToken: ct);
                if (writer is SharpCompress.Writers.Tar.TarWriter tar)
                    tar.Write(entryPath, content, child.LastModified, content.CanSeek ? content.Length : child.OriginalSize);
                else
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
