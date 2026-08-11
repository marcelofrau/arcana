namespace Arcana.Core.Compression.Formats;

/// <summary>
/// Read-only view of a byte range of an ISO file on disk. ISO9660 stores files
/// raw, so no decompression is needed — the stream seeks straight to the file's
/// extent (sector * 2048) and reads up to its declared length. A fresh
/// FileStream is opened on demand and closed on Dispose, so ISO entries never
/// occupy memory.
/// </summary>
internal sealed class IsoEntryStream : Stream
{
    private readonly string _path;
    private readonly long _offset;
    private readonly long _length;
    private FileStream? _file;
    private long _position;

    public IsoEntryStream(string path, long offset, long length)
    {
        _path = path;
        _offset = offset;
        _length = length;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get => _position;
        set => _position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
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
        _position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        return _position;
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
            _file = File.OpenRead(_path);
            _file.Position = _offset;
        }
        return _file;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _file?.Dispose();
        _file = null;
        base.Dispose(disposing);
    }
}
