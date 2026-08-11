using System.Diagnostics;
using System.IO;

namespace Arcana.Core.Compression;

/// <summary>
/// Forwarding stream that counts bytes read (or written) and reports progress with
/// throttling (every <see cref="ReportIntervalBytes"/> bytes or ~100 ms). Used to wrap
/// entry content streams (read side) and the compressed output stream (write side),
/// feeding a merged <see cref="ProgressReport"/> to the UI.
/// </summary>
public sealed class ProgressStream : Stream
{
    private const long ReportIntervalBytes = 128 * 1024;
    private const long ReportIntervalMs = 100;

    private readonly Stream _inner;
    private readonly IProgress<ProgressReport>? _progress;
    private readonly string? _currentFile;
    private readonly int _filesProcessed;
    private readonly int _totalFiles;
    private readonly string? _operation;
    private readonly long _totalBytes;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly bool _isOutput;

    private long _read;
    private long _written;
    private long _bytesSinceReport;
    private long _lastReportMs;

    public ProgressStream(Stream inner, IProgress<ProgressReport>? progress,
        string? currentFile = null, long totalBytes = 0,
        int filesProcessed = 0, int totalFiles = 0,
        string? operation = null, bool isOutput = false)
    {
        _inner = inner;
        _progress = progress;
        _currentFile = currentFile;
        _totalBytes = totalBytes;
        _filesProcessed = filesProcessed;
        _totalFiles = totalFiles;
        _operation = operation;
        _isOutput = isOutput;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _inner.Read(buffer, offset, count);
        if (n > 0)
        {
            _read += n;
            MaybeReport();
        }
        return n;
    }

    public override int Read(Span<byte> buffer)
    {
        var n = _inner.Read(buffer);
        if (n > 0)
        {
            _read += n;
            MaybeReport();
        }
        return n;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (n > 0)
        {
            _read += n;
            MaybeReport();
        }
        return n;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
        _written += count;
        MaybeReport();
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Write(buffer);
        _written += buffer.Length;
        MaybeReport();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        _written += buffer.Length;
        MaybeReport();
    }

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
    public override void SetLength(long value) => _inner.SetLength(value);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Report(final: true);
        _inner.Dispose();
        base.Dispose(disposing);
    }

    private void MaybeReport()
    {
        _bytesSinceReport += _isOutput ? 1 : 1; // counted via _written/_read deltas below
        var now = _stopwatch.ElapsedMilliseconds;
        var changed = _isOutput ? _written : _read;
        if (changed - _lastBytesAtReport >= ReportIntervalBytes || now - _lastReportMs >= ReportIntervalMs)
            Report(final: false);
    }

    private long _lastBytesAtReport;

    private void Report(bool final)
    {
        var processed = _isOutput ? _written : _read;
        var now = _stopwatch.ElapsedMilliseconds;
        if (!final && processed - _lastBytesAtReport < ReportIntervalBytes && now - _lastReportMs < ReportIntervalMs)
            return;

        _progress?.Report(new ProgressReport
        {
            CurrentFile = _currentFile,
            BytesProcessed = processed,
            TotalBytes = _totalBytes,
            FilesProcessed = _filesProcessed,
            TotalFiles = _totalFiles,
            CurrentOperation = _operation,
            Elapsed = _stopwatch.Elapsed,
            CompressedBytes = _isOutput ? _written : 0,
        });

        _lastBytesAtReport = processed;
        _lastReportMs = now;
    }
}
