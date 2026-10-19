using Serilog;

namespace Arcana.Core.Compression.Formats;

/// <summary>
/// ISO9660 (CD/DVD disk image) engine. ISO stores files raw, so opening is pure
/// metadata parsing (Primary Volume Descriptor + directory records) and entry
/// content is served lazily straight from the source file via
/// <see cref="Filesystem.FileSliceStream"/> — nothing is extracted into memory.
/// </summary>
public class IsoEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<IsoEngine>();
    public string Name => "ISO";
    public string Extension => ".iso";
    public bool CanRead => true;
    public bool CanWrite => false;
    public bool CanEncrypt => false;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => false;
    public string? Password { get; set; }

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        try
        {
            if (!Iso9660Reader.TryCreate(stream, out var reader) || reader is null)
                throw new NotSupportedException($"'{path}' is not a valid ISO9660 volume");

            var entries = new List<ArchiveEntry>();
            var vfs = new Filesystem.VirtualFileSystem();

            foreach (var iso in reader.Walk(ct))
            {
                ct.ThrowIfCancellationRequested();
                if (iso.IsDirectory)
                {
                    vfs.AddDirectory(iso.FullPath);
                    entries.Add(new ArchiveEntry
                    {
                        Path = iso.FullPath,
                        Name = iso.Name,
                        Size = iso.Length,
                        CompressedSize = iso.Length,
                        IsDirectory = true,
                        LastModified = iso.LastModified,
                    });
                    continue;
                }

                var filePath = path;
                var offset = iso.Offset;
                var length = iso.Length;
                vfs.AddLazyFile(iso.FullPath, () => new Filesystem.FileSliceStream(filePath, offset, length));

                entries.Add(new ArchiveEntry
                {
                    Path = iso.FullPath,
                    Name = iso.Name,
                    Size = length,
                    CompressedSize = length,
                    IsDirectory = false,
                    LastModified = iso.LastModified,
                });
            }

            _log.Information("Opened {Path} with {EntryCount} entries ({Names}) as ISO",
                path, entries.Count, reader.Joliet ? "Joliet" : "ISO9660");
            return new Archive
            {
                Format = CompressionFormat.Iso,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open ISO {Path}", path);
            throw;
        }
    }

    public Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => Task.FromResult(Open(path, stream, mode, ct));

    public void Save(Archive archive, Stream stream, CompressionOptions options,
        IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("ISO writing is not supported");

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
        IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("ISO writing is not supported");
}
