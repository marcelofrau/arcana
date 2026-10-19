using Arcana.Core.Filesystem;
using Serilog;

namespace Arcana.Core.Compression;

public static class ArchiveWriter
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ArchiveWriter));

    public static Task SaveAsync(Archive archive, string path, CompressionOptions options,
        IProgress<ProgressReport>? progress = null, CancellationToken ct = default)
    {
        if (archive.IsMultiVolume && archive.SourcePaths.Any(p => Path.GetFullPath(p).Equals(Path.GetFullPath(path),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            throw new InvalidOperationException("Save a multi-volume archive to a new output path, not over an input volume.");
        if (archive.IsEncrypted && options.Encryption == null)
        {
            if (archive.Password == null)
                throw new InvalidOperationException("Reopen with a password before saving an encrypted archive.");
            options.Encryption = new Cryptography.EncryptionOptions { Password = archive.Password };
            Log.Debug("Preserving archive encryption");
        }
        var engine = ArchiveFactory.GetFormat(options.Format);
        if (!engine.CanWrite)
            throw new NotSupportedException($"Writing {engine.Name} is not supported.");
        if (ArchiveFormats.IsSingleFile(options.Format) && (archive.Vfs.Root.Children.Count != 1
            || archive.Vfs.Root.Children[0].Type != NodeType.File))
            throw new InvalidOperationException($"{engine.Name} holds one file. Choose ZIP, 7z or TAR for folders and multiple files.");
        if (options.Encryption?.Password != null && !engine.CanEncrypt)
            throw new NotSupportedException($"{engine.Name} does not support password-protected output.");
        if (options.Format is CompressionFormat.SevenZip or CompressionFormat.Xz or CompressionFormat.Lzma or CompressionFormat.TarXz
            && archive.Vfs.EnumerateNodes().Where(n => n.Type == NodeType.File).Sum(n => n.OriginalSize) > ArchiveLimits.MaxBufferedWriteBytes)
            throw new NotSupportedException("This managed encoder buffers input and is limited to 256 MiB. Use ZIP or TAR + Zstandard for larger jobs.");
        Log.Debug("Saving {Format} to {Path}", options.Format, path);
        return AtomicFile.WriteAsync(path, async (output, token) =>
        {
            foreach (var node in archive.Vfs.EnumerateNodes().Where(n => n.Type == NodeType.File && (n.Crc32.HasValue || archive.IsEncrypted)))
            {
                progress?.Report(new ProgressReport { CurrentFile = node.Name, CurrentOperation = "Verifying" });
                await ArchiveContent.CopyVerifiedAsync(node, Stream.Null, token).ConfigureAwait(false);
            }
            using var counted = new ProgressStream(output, progress, operation: "Compressing", isOutput: true, leaveOpen: true, cancellationToken: token);
            await engine.SaveAsync(archive, counted, options, progress, token).ConfigureAwait(false);
        }, ct);
    }
}
