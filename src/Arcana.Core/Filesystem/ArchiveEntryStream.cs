using Serilog;

namespace Arcana.Core.Filesystem;

/// <summary>Releases an archive reader's exclusive stream lease when the entry is closed.</summary>
internal sealed class ArchiveEntryStream(Stream inner, long length, SemaphoreSlim gate) : Stream
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ArchiveEntryStream>();
    private bool _disposed;
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => inner.Position; set => inner.Position = value; }
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => inner.Read(buffer);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => inner.ReadAsync(buffer, ct);
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            try { inner.Dispose(); }
            finally { gate.Release(); Log.Verbose("Released archive entry reader"); }
        }
        base.Dispose(disposing);
    }
}
