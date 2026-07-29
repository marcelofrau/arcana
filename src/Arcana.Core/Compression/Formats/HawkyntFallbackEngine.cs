using Compression.Registry;
using Serilog;

namespace Arcana.Core.Compression.Formats;

public class HawkyntFallbackEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<HawkyntFallbackEngine>();
    public string Name => "Hawkynt";
    public string Extension => "";
    public bool CanRead => true;
    public bool CanWrite => false;
    public bool CanEncrypt => false;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => false;
    public string? Password { get; set; }

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        try
        {
            HawkyntInit.Ensure();

            var descriptor = FindDescriptor(path, stream);
            if (descriptor is null)
            {
                _log.Warning("No Hawkynt descriptor matched for {Path}, falling back", path);
                throw new NotSupportedException($"No Hawkynt format descriptor matched for '{path}'");
            }

            _log.Debug("Detected format via descriptor {DescriptorId}", descriptor.Id);
            var ops = FormatRegistry.GetArchiveOps(descriptor.Id);
            var entries = ops.List(stream, Password);

            var result = new List<ArchiveEntry>();
            var vfs = new Filesystem.VirtualFileSystem();

            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = entry.Name.Replace('\\', '/');

                if (entry.IsDirectory)
                {
                    vfs.AddDirectory(key);
                    result.Add(new ArchiveEntry
                    {
                        Path = key,
                        Name = key.Trim('/').Split('/').Last(),
                        Size = entry.OriginalSize,
                        CompressedSize = entry.CompressedSize,
                        IsDirectory = true,
                        LastModified = NormalizeTimestamp(entry.LastModified),
                    });
                    continue;
                }

                var data = ops.ExtractEntryToMemory(stream, entry.Name, Password);
                vfs.AddFile(key, new MemoryStream(data));

                result.Add(new ArchiveEntry
                {
                    Path = key,
                    Name = System.IO.Path.GetFileName(key),
                    Size = entry.OriginalSize,
                    CompressedSize = entry.CompressedSize,
                    IsEncrypted = entry.IsEncrypted,
                    IsDirectory = false,
                    LastModified = NormalizeTimestamp(entry.LastModified),
                });
            }

            stream.Position = 0;

            _log.Information("Opened {Path} with {EntryCount} entries via fallback descriptor {DescriptorId}", path, result.Count, descriptor.Id);
            return new Archive
            {
                Format = CompressionFormat.Hawkynt,
                FormatEngine = this,
                Entries = result,
                Vfs = vfs,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open archive via Hawkynt fallback {Path}", path);
            throw;
        }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("Hawkynt fallback writing is not supported");

    public Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                          IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => throw new NotSupportedException("Hawkynt fallback writing is not supported");

    public IFormatDescriptor? FindDescriptor(string path, Stream stream)
    {
        HawkyntInit.Ensure();

        var ext = System.IO.Path.GetExtension(path);
        if (!string.IsNullOrEmpty(ext))
        {
            foreach (var desc in FormatRegistry.All)
            {
                if (desc.Category != FormatCategory.Archive) continue;
                if (desc.Extensions.Any(e =>
                        e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                    return desc;
            }
        }

        var header = new byte[32];
        var pos = stream.Position;
        var read = stream.Read(header, 0, header.Length);
        stream.Position = pos;

        if (read == 0) return null;

        foreach (var desc in FormatRegistry.All)
        {
            if (desc.Category != FormatCategory.Archive) continue;
            if (MatchesMagic(header, desc.MagicSignatures))
                return desc;
        }

        return null;
    }

    private static bool MatchesMagic(byte[] header, IEnumerable<MagicSignature> signatures)
    {
        foreach (var sig in signatures)
        {
            var bytes = sig.Bytes;
            var offset = sig.Offset;
            if (offset + bytes.Length > header.Length) continue;

            var match = true;
            for (var i = 0; i < bytes.Length; i++)
                if (header[offset + i] != bytes[i])
                {
                    match = false;
                    break;
                }

            if (match) return true;
        }
        return false;
    }

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
