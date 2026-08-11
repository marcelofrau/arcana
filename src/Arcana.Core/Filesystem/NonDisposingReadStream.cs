using System.IO;

namespace Arcana.Core.Filesystem;

/// <summary>
/// Read-only forwarding stream whose Dispose/Close are no-ops. VFS node content is
/// a shared backing stream owned by the node's <see cref="ArchiveNode.ContentFactory"/>;
/// callers may use this wrapper in using blocks without destroying the shared stream.
/// </summary>
public sealed class NonDisposingReadStream : Stream
{
    private readonly Stream _inner;
    private bool _disposed;

    public NonDisposingReadStream(Stream inner)
    {
        _inner = inner;
    }

    public override bool CanRead => true;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
        => _inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer)
        => _inner.Read(buffer);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(buffer, cancellationToken);

    public override void CopyTo(Stream destination, int bufferSize)
        => _inner.CopyTo(destination, bufferSize);

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => await _inner.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);

    public override long Seek(long offset, SeekOrigin origin)
        => _inner.Seek(offset, origin);

    public override void Flush()
    {
    }

    public override void SetLength(long value)
        => throw new NotSupportedException("NonDisposingReadStream is read-only.");

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("NonDisposingReadStream is read-only.");

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;
        // Intentionally do NOT dispose _inner.
    }

    public override ValueTask DisposeAsync()
    {
        if (!_disposed)
            _disposed = true;
        return default;
    }
}
