namespace Arcana.Core.Compression;

/// <summary>
/// Decides when an operation should push the next <see cref="ProgressReport"/>.
/// The first call always reports, then reports are spaced by <c>countStep</c> items
/// or 100 ms, whichever comes first. Small jobs therefore update on every item
/// while large ones stop flooding the UI thread.
/// </summary>
public sealed class ProgressThrottle
{
    private const int IntervalMs = 100;

    private readonly int _countStep;
    private readonly long _start = Environment.TickCount64;
    private readonly Lock _gate = new();
    private long _lastMs;
    private long _lastCount;
    private bool _reported;

    /// <param name="countStep">Items between reports. Pass about 1% of the known total so
    /// small archives still update per item.</param>
    public ProgressThrottle(int countStep = 50) => _countStep = Math.Max(1, countStep);

    public bool ShouldReport(long processed)
    {
        lock (_gate)
        {
            var ms = Environment.TickCount64 - _start;
            if (_reported && processed - _lastCount < _countStep && ms - _lastMs < IntervalMs)
                return false;
            _reported = true;
            _lastCount = processed;
            _lastMs = ms;
            return true;
        }
    }
}
