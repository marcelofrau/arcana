using Arcana.Core.Cryptography;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;
using Serilog;
using ICSharpCode.SharpZipLib.Zip;

namespace Arcana.Core.Compression.Formats;

public class ZipEngine : IArchiveFormat
{
    private readonly ILogger _log = Log.ForContext<ZipEngine>();
    public string Name => "ZIP";
    public string Extension => ".zip";
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanEncrypt => true;
    public bool SupportsSolid => false;
    public bool SupportsVolumes => false;
    public string? Password { get; set; }

    public Archive Open(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
        => OpenAsync(path, stream, mode, ct).GetAwaiter().GetResult();

    public async Task<Archive> OpenAsync(string path, Stream stream, AccessMode mode, CancellationToken ct = default)
    {
        _log.Verbose("Opening {Path} with mode {Mode}", path, mode);
        var vfs = new Filesystem.VirtualFileSystem();
        try
        {
            Stream owned;
            if (stream is FileStream file)
                owned = new FileStream(file.Name, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            else
            {
                owned = new MemoryStream();
                await stream.CopyToAsync(owned, ct).ConfigureAwait(false);
                owned.Position = 0;
            }
            vfs.Own(owned);
            var readStream = LegacyArchiveEncryption.Open(owned, Password, [0x50, 0x4B]);
            if (!ReferenceEquals(readStream, owned)) vfs.Own(readStream);

            _log.Debug("Detected ZIP format for {Path}", path);
            var readerOptions = new ReaderOptions { Password = Password };

            var multiVolume = stream is FileStream && ReferenceEquals(readStream, owned)
                && (path.EndsWith(".001", StringComparison.OrdinalIgnoreCase) || File.Exists(Path.ChangeExtension(path, ".z01")));
            var zip = multiVolume
                ? (IArchive)SharpCompress.Archives.Zip.ZipArchive.OpenArchive(new FileInfo(path),
                    new ReaderOptions { Password = Password, LeaveStreamOpen = false })
                : OpenForReading(readStream, readerOptions);
            vfs.Own(zip);
            var gate = new SemaphoreSlim(1, 1);
            var entries = new List<ArchiveEntry>();
            var passwordChecked = false;

            foreach (var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();
                var key = entry.Key ?? "";
                if (entry.IsEncrypted && Password == null)
                    throw new SharpCompress.Common.CryptographicException("Password required.");
                if (entry.IsEncrypted && !entry.IsDirectory && !passwordChecked)
                {
                    using var check = entry.OpenEntryStream();
                    _ = check.ReadByte();
                    passwordChecked = true;
                }

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

                var node = vfs.AddLazyFile(key, () =>
                {
                    gate.Wait();
                    try { return new Filesystem.ArchiveEntryStream(entry.OpenEntryStream(), entry.Size, gate); }
                    catch (Exception ex) { gate.Release(); _log.Warning(ex, "Could not open ZIP entry {Path}", key); throw; }
                });
                node.OriginalSize = entry.Size;
                node.Crc32 = entry.IsEncrypted && entry.Crc == 0 ? null : (uint)entry.Crc;
                node.LastModified = NormalizeTimestamp(entry.LastModifiedTime);

                entries.Add(new ArchiveEntry
                {
                    Path = key,
                    Name = System.IO.Path.GetFileName(key),
                    Size = entry.Size,
                    CompressedSize = entry.CompressedSize,
                    Crc32 = (uint)entry.Crc,
                    IsEncrypted = entry.IsEncrypted,
                    IsDirectory = false,
                    LastModified = NormalizeTimestamp(entry.LastModifiedTime),
                });
            }

            _log.Information("Opened {Path} with {EntryCount} entries", path, entries.Count);
            return new Archive
            {
                Format = CompressionFormat.Zip,
                FormatEngine = this,
                Entries = entries,
                Vfs = vfs,
                IsEncrypted = zip.IsEncrypted || !ReferenceEquals(readStream, owned),
                Password = Password,
                SourcePaths = zip.Volumes.Select(v => v.FileName).OfType<string>().Select(Path.GetFullPath).ToArray(),
            };
        }
        catch (Exception ex)
        {
            vfs.Dispose();
            _log.Error(ex, "Failed to open ZIP archive {Path}", path);
            throw;
        }
    }

    public void Save(Archive archive, Stream stream, CompressionOptions options,
                     IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
        => SaveAsync(archive, stream, options, progress, ct).GetAwaiter().GetResult();

    public async Task SaveAsync(Archive archive, Stream stream, CompressionOptions options,
                                IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        _log.Verbose("Saving archive with {FileCount} files, level {Level}", archive.Vfs.Root.Children.Count, options.Level);
        try
        {
            var password = options.Encryption?.Password;
            using var writer = new ZipOutputStream(stream) { IsStreamOwner = false, Password = password,
                UseZip64 = UseZip64.Dynamic };
            writer.SetLevel(Math.Clamp((int)options.Level, 0, 9));
            if (password != null) _log.Information("Writing standard ZIP AES-256 encryption");
            var nodes = Flatten(archive.Vfs.Root).ToList();
            var total = nodes.Count(n => n.Type == Filesystem.NodeType.File);
            var done = 0;
            foreach (var node in nodes)
            {
                ct.ThrowIfCancellationRequested();
                var directory = node.Type == Filesystem.NodeType.Directory;
                var name = node.FullPath.TrimStart('/') + (directory ? "/" : "");
                var entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(name)
                {
                    IsUnicodeText = true,
                    DateTime = node.LastModified.Year >= 1980 ? node.LastModified : new DateTime(1980, 1, 1),
                    AESKeySize = !directory && password != null ? 256 : 0,
                };
                writer.Password = directory ? null : password;
                if (directory)
                {
                    entry.Size = 0;
                    writer.PutNextEntry(entry);
                }
                else
                {
                    await using var content = node.OpenRead();
                    if (content.CanSeek) entry.Size = content.Length;
                    writer.PutNextEntry(entry);
                    await content.CopyToAsync(writer, ct).ConfigureAwait(false);
                    progress?.Report(new ProgressReport { CurrentFile = node.Name, FilesProcessed = ++done,
                        TotalFiles = total, CurrentOperation = "Compressing" });
                }
                writer.CloseEntry();
            }
            ct.ThrowIfCancellationRequested();
            writer.Finish();

            _log.Information("Saved ZIP archive to stream");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save ZIP archive");
            throw;
        }
    }

    private static IArchive OpenForReading(Stream stream, ReaderOptions options)
    {
        return (IArchive)(object)SharpCompress.Archives.Zip.ZipArchive.OpenArchive(stream, options);
    }

    private static IEnumerable<Filesystem.ArchiveNode> Flatten(Filesystem.ArchiveNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in Flatten(child)) yield return descendant;
        }
    }

    private static DateTime NormalizeTimestamp(DateTime? ts) =>
        ts is { Year: >= 1980 } ? ts.Value : DateTime.MinValue;
}
