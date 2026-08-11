using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Arcana.App.Icons;
using Arcana.App.Services;
using Arcana.Core.Compression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.ViewModels;

/// <summary>One point on the live compression-ratio chart.</summary>
public readonly record struct RatioSample(double Time, double Ratio);

/// <summary>One point on the live speed chart, in megabytes per second.</summary>
public readonly record struct SpeedSample(double Time, double MBps);

/// <summary>
/// Drives the modal progress dialog. Consumes <see cref="ProgressReport"/> updates from
/// an operation and aggregates them into overall %, per-file %, speed, elapsed / ETA,
/// the live speed series (DownThemAll-style chart) and, for compress operations, the
/// live compression-ratio series.
/// </summary>
public partial class ProgressViewModel : ObservableObject
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ProgressViewModel>();

    private const double SpeedSampleIntervalSeconds = 0.5;

    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly bool _showChart;
    private readonly long _totalOriginalBytes;
    private long _lastCompressed;
    private double _lastEta;
    private double _speedPerSecond;
    private double _smoothedRatio = 100;
    private double _lastSpeedSampleTime;

    public ProgressViewModel(string title, bool showChart = false, long totalOriginalBytes = 0,
        IconKey headerIcon = IconKey.FileArchive)
    {
        Title = title;
        _showChart = showChart;
        _totalOriginalBytes = totalOriginalBytes;
        HeaderIcon = headerIcon;
    }

    public string Title { get; }
    public IconKey HeaderIcon { get; }
    public bool ShowChart => _showChart;

    public CancellationTokenSource CancelSource { get; } = new();
    public bool IsCanceled { get; private set; }

    [ObservableProperty]
    private double _overallPercent;

    [ObservableProperty]
    private string _overallText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSizeText))]
    private string _sizeText = "";

    public bool HasSizeText => SizeText.Length > 0;

    [ObservableProperty]
    private string _currentFile = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFileProgress))]
    private double? _filePercent;

    public bool HasFileProgress => FilePercent.HasValue;

    [ObservableProperty]
    private string _speedText = "";

    [ObservableProperty]
    private string _elapsedText = "";

    [ObservableProperty]
    private string _remainingText = "";

    [ObservableProperty]
    private string _ratioText = "";

    [ObservableProperty]
    private ObservableCollection<RatioSample> _ratioSamples = [];

    [ObservableProperty]
    private ObservableCollection<SpeedSample> _speedSamples = [];

    public void Update(ProgressReport r)
    {
        if (r.TotalFiles > 0)
        {
            var done = Math.Clamp(r.FilesProcessed, 0, r.TotalFiles);
            var fileFraction = r.TotalBytes > 0 ? Math.Clamp((double)r.BytesProcessed / r.TotalBytes, 0, 1) : 0;
            OverallPercent = Math.Clamp(((done + fileFraction) / r.TotalFiles) * 100, 0, 100);
            OverallText = T("progress.items", done, r.TotalFiles);
            SizeText = r.TotalBytes > 0
                ? T("progress.bytes", ByteFormat.Format(r.BytesProcessed), ByteFormat.Format(r.TotalBytes))
                : "";
        }
        else if (r.TotalBytes > 0)
        {
            OverallPercent = Math.Clamp((double)r.BytesProcessed / r.TotalBytes * 100, 0, 100);
            OverallText = "";
            SizeText = T("progress.bytes", ByteFormat.Format(r.BytesProcessed), ByteFormat.Format(r.TotalBytes));
        }

        if (!string.IsNullOrEmpty(r.CurrentFile))
        {
            CurrentFile = r.CurrentFile;
            FilePercent = r.TotalBytes > 0
                ? Math.Clamp((double)r.BytesProcessed / r.TotalBytes * 100, 0, 100)
                : null;
        }

        var elapsed = _sw.Elapsed.TotalSeconds;
        ElapsedText = FormatElapsed(_sw.Elapsed);

        var compressed = Math.Max(r.CompressedBytes, _lastCompressed);
        if (compressed > 0 && elapsed > 0)
        {
            _speedPerSecond = _speedPerSecond == 0
                ? compressed / elapsed
                : 0.7 * _speedPerSecond + 0.3 * (compressed / elapsed);
            SpeedText = $"{ByteFormat.Format((long)_speedPerSecond)}/s";

            if (elapsed - _lastSpeedSampleTime >= SpeedSampleIntervalSeconds)
            {
                _lastSpeedSampleTime = elapsed;
                if (compressed > 0)
                {
                    SpeedSamples.Add(new SpeedSample(elapsed, _speedPerSecond / (1024.0 * 1024.0)));
                    Log.Verbose("Speed sample @{Time:0.0}s: {Speed:0.0} MB/s (compressed {Compressed:N0} B)",
                        elapsed, _speedPerSecond / (1024.0 * 1024.0), compressed);
                }
                else
                {
                    Log.Verbose("Speed skipped @{Time:0.0}s: CompressedBytes=0, BytesProcessed={Processed:N0}",
                        elapsed, r.BytesProcessed);
                }
            }

            if (_totalOriginalBytes > 0 && compressed > 0)
            {
                var ratio = (double)compressed / _totalOriginalBytes * 100;
                if (ratio < 1) ratio = 1;
                _smoothedRatio = _smoothedRatio == 100
                    ? ratio
                    : 0.75 * _smoothedRatio + 0.25 * ratio;
                RatioSamples.Add(new RatioSample(elapsed, Math.Round(_smoothedRatio, 1)));
                RatioText = $"{_smoothedRatio:0}%";

                var remaining = Math.Max(0, (_totalOriginalBytes - compressed) / Math.Max(_speedPerSecond, 1));
                _lastEta = _lastEta == 0
                    ? remaining
                    : 0.7 * _lastEta + 0.3 * remaining;
                if (_lastEta >= 1)
                    RemainingText = FormatEta(TimeSpan.FromSeconds(_lastEta));
                else if (compressed >= _totalOriginalBytes)
                    RemainingText = "";
            }
        }

        if (compressed > _lastCompressed)
            _lastCompressed = compressed;
    }

    public void Complete()
    {
        Log.Verbose("Progress complete: {Samples} speed samples, final speed {Speed}", SpeedSamples.Count, _speedPerSecond);
        OverallPercent = 100;
        if (FilePercent.HasValue)
            FilePercent = 100;
        SpeedText = "";
        RemainingText = "";
    }

    [RelayCommand]
    private void Cancel()
    {
        IsCanceled = true;
        CancelSource.Cancel();
    }

    private static string FormatElapsed(TimeSpan t)
        => t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes}:{t.Seconds:00}";

    private static string FormatEta(TimeSpan t)
        => t.TotalHours >= 1 ? $"~{t.Hours}h {t.Minutes}m" : t.TotalMinutes >= 1 ? $"~{t.Minutes}m {t.Seconds}s" : $"~{t.Seconds}s";
}
