using Serilog;

namespace Arcana.Core.Filesystem;

/// <summary>
/// Read-only, independently seekable slice of an ISO or a shared disk-backed archive cache.
/// </summary>
internal sealed class FileSliceStream : Stream
{
    private static readonly ILogger Log = Serilog.Log.ForContext<FileSliceStream>();
    private readonly string _path;
    private readonly long _offset;
    private readonly long _length;
    private FileStream? _file;
    private long _position;
    private readonly FileShare _share;
    private bool _disposed;

    public FileSliceStream(string path, long offset, long length, FileShare share = FileShare.Read)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        _ = checked(offset + length);
        _path = path;
        _offset = offset;
        _length = length;
        _share = share;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateBufferArguments(buffer, offset, count);
        if (_position >= _length)
            return 0;
        var toRead = (int)Math.Min(count, _length - _position);
        var file = EnsureOpen();
        file.Position = _offset + _position;
        var read = file.Read(buffer, offset, toRead);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(_length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (position < 0) throw new IOException("Cannot seek before the entry start.");
        _position = position;
        return _position;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ct.ThrowIfCancellationRequested();
        if (_position >= _length) return 0;
        var file = EnsureOpen();
        file.Position = _offset + _position;
        var read = await file.ReadAsync(buffer[..(int)Math.Min(buffer.Length, _length - _position)], ct).ConfigureAwait(false);
        _position += read;
        return read;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private FileStream EnsureOpen()
    {
        if (_file == null)
        {
            _file = new FileStream(_path, FileMode.Open, FileAccess.Read, _share, 81920, FileOptions.Asynchronous | FileOptions.RandomAccess);
            if (_file.Length < _offset + _length)
            {
                _file.Dispose();
                _file = null;
                throw new InvalidDataException("Archive content is truncated.");
            }
            _file.Position = _offset;
            Log.Verbose("Opened content slice {Path} at {Offset} ({Length} bytes)", _path, _offset, _length);
        }
        return _file;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _file?.Dispose();
        _disposed = true;
        _file = null;
        base.Dispose(disposing);
    }
}
