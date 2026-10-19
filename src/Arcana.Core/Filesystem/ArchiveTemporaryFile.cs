using Serilog;

namespace Arcana.Core.Filesystem;

internal static class ArchiveTemporaryFile
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ArchiveTemporaryFile));

    public static FileStream Create()
    {
        var path = Path.Combine(Path.GetTempPath(), $"arcana-content-{Guid.NewGuid():N}.tmp");
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.ReadWrite,
            Share = FileShare.Read | FileShare.Delete, Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        Log.Debug("Creating disk-backed archive content {Path}", path);
        return new FileStream(path, options);
    }
}
