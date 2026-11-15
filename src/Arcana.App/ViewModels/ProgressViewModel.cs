using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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

    /// <summary>Window used to average the throughput readout; longer = steadier, slower to react.</summary>
    private const double SpeedWindowSeconds = 2.0;

    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly bool _showChart;
    private readonly long _totalOriginalBytes;
    private readonly Queue<(double Time, long Bytes)> _speedWindow = new();
    private readonly Queue<(double Time, long Bytes)> _archiveWindow = new();
    private long _lastCompressed;
    private double? _shownEta;
    private double _speedPerSecond;
    private double _archivePerSecond;
    private double _smoothedRatio;
    private double _lastSpeedSampleTime;
    private double _lastArchiveSampleTime;

    /// <summary>Samples kept per series; the chart only shows the newest minute anyway, so
    /// older points are dropped to keep the redraw cheap on hour-long operations.</summary>
    private const int MaxSamples = 300;

    /// <summary>Newest speed samples the ETA takes its median from: ten seconds of history.</summary>
    private const int EtaSampleCount = 20;

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

    /// <summary>Archive-side throughput (compressed bytes per second), shown as the second
    /// chart line. Extraction: how fast the archive is being read. Compression: how fast the
    /// output file grows.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasArchiveSpeed))]
    private string _archiveSpeedText = "";

    public bool HasArchiveSpeed => ArchiveSpeedText.Length > 0;

    [ObservableProperty]
    private string _elapsedText = "";

    [ObservableProperty]
    private string _remainingText = "";

    [ObservableProperty]
    private string _ratioText = "";

    /// <summary>Archive-side bytes as a share of the plain total, drawn as the thinner bar
    /// under the main one. The gap between the two bars is the compression ratio.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasArchiveBar))]
    private double _archivePercent;

    public bool HasArchiveBar => ArchivePercent > 0;

    [ObservableProperty]
    private ObservableCollection<RatioSample> _ratioSamples = [];

    [ObservableProperty]
    private ObservableCollection<SpeedSample> _speedSamples = [];

    /// <summary>Second series for the speed chart: archive-side throughput.</summary>
    [ObservableProperty]
    private ObservableCollection<SpeedSample> _archiveSamples = [];

    public void Update(ProgressReport r)
    {
        // Known total bytes: the accurate measure for byte-oriented operations.
        if (r.TotalBytes > 0)
        {
            OverallPercent = Math.Clamp((double)r.BytesProcessed / r.TotalBytes * 100, 0, 100);
            OverallText = r.TotalFiles > 0 ? T("progress.items", Math.Clamp(r.FilesProcessed, 0, r.TotalFiles), r.TotalFiles) : "";
            SizeText = T("progress.bytes", ByteFormat.Format(r.BytesProcessed), ByteFormat.Format(r.TotalBytes));
        }
        // Known total files, unknown bytes: count items.
        else if (r.TotalFiles > 0)
        {
            var done = Math.Clamp(r.FilesProcessed, 0, r.TotalFiles);
            OverallPercent = Math.Clamp((double)done / r.TotalFiles * 100, 0, 100);
            OverallText = T("progress.items", done, r.TotalFiles);
            SizeText = "";
        }
        // Indeterminate: only files processed count known
        else if (r.FilesProcessed > 0)
        {
            OverallPercent = -1; // Indeterminate
            OverallText = T("progress.itemsRead", r.FilesProcessed);
            SizeText = "";
        }

        if (!string.IsNullOrEmpty(r.CurrentFile))
        {
            CurrentFile = r.CurrentFile;
            // No per-file bar: reports carry cumulative bytes (extract/split/join) or only an
            // item counter (compress), so there is no single-file fraction to show.
            FilePercent = null;
        }

        var elapsed = _sw.Elapsed.TotalSeconds;
        ElapsedText = FormatElapsed(_sw.Elapsed);

        var compressed = Math.Max(r.CompressedBytes, _lastCompressed);
        if (compressed > _lastCompressed)
            _lastCompressed = compressed;

        // Plain (uncompressed) side of the operation: bytes extraction writes out and
        // bytes compression feeds in. Speed and ETA are derived from it, so the readout
        // already means "megabytes decompressed/compressed per second".
        var plainTotal = r.TotalBytes > 0 ? r.TotalBytes : _totalOriginalBytes;
        var plainDone = r.BytesProcessed;

        if (plainTotal > 0 && plainDone > 0 && elapsed > 0)
        {
            var speed = Smooth(_speedWindow, elapsed, plainDone, ref _speedPerSecond);
            SpeedText = FormatSpeed(speed);
            AddSpeedSample(SpeedSamples, ref _lastSpeedSampleTime, elapsed, speed);
            UpdateEta(plainTotal - plainDone, speed);
        }
        else if (compressed > 0 && elapsed > 0)
        {
            // Write-side only (compression): written bytes per second.
            var speed = Smooth(_speedWindow, elapsed, compressed, ref _speedPerSecond);
            SpeedText = FormatSpeed(speed);
            AddSpeedSample(SpeedSamples, ref _lastSpeedSampleTime, elapsed, speed);
            if (plainTotal > 0)
                UpdateEta(plainTotal - compressed, speed);
        }

        // Archive side: the compressed bytes actually moving. Derived from the same reports,
        // so it needs no extra plumbing from the engines.
        if (compressed > 0 && elapsed > 0)
        {
            var archiveSpeed = Smooth(_archiveWindow, elapsed, compressed, ref _archivePerSecond);
            ArchiveSpeedText = FormatSpeed(archiveSpeed);
            AddSpeedSample(ArchiveSamples, ref _lastArchiveSampleTime, elapsed, archiveSpeed);
        }

        // Compression ratio: archive-side bytes against the plain total. The archive side
        // is the compressed size for extraction and the written size for compression.
        if (plainTotal > 0 && compressed > 0)
        {
            var archiveTotal = r.TotalCompressedBytes > 0 ? r.TotalCompressedBytes : compressed;
            var ratio = (double)plainTotal / Math.Max(archiveTotal, 1);
            _smoothedRatio = _smoothedRatio <= 0 ? ratio : 0.75 * _smoothedRatio + 0.25 * ratio;
            RatioText = T("progress.ratioValue",
                _smoothedRatio.ToString("0.#", CultureInfo.InvariantCulture),
                ByteFormat.Format(compressed));
            ArchivePercent = Math.Clamp((double)compressed / plainTotal * 100, 0, 100);
            if (_showChart)
                RatioSamples.Add(new RatioSample(elapsed,
                    Math.Round(100.0 / Math.Max(_smoothedRatio, 0.01), 1)));
        }
    }

    public void Complete()
    {
        Log.Information("Progress complete: plain {Plain:0.0} MB/s, archive {Archive:0.0} MB/s, ratio {Ratio:0.00}:1 ({Samples} samples)",
            _speedPerSecond / (1024.0 * 1024.0), _archivePerSecond / (1024.0 * 1024.0), _smoothedRatio, SpeedSamples.Count);
        OverallPercent = 100;
        if (FilePercent.HasValue)
            FilePercent = 100;
        SpeedText = "";
        ArchiveSpeedText = "";
        RemainingText = "";
        _shownEta = null;
        _speedWindow.Clear();
        _archiveWindow.Clear();
    }

    /// <summary>Rate over a short sliding window. The old cumulative average lagged behind
    /// slowdowns and the raw per-report delta jumped, so neither was readable.</summary>
    private static double Smooth(Queue<(double Time, long Bytes)> window, double now, long bytes, ref double last)
    {
        window.Enqueue((now, bytes));
        while (window.Count > 2 && now - window.Peek().Time > SpeedWindowSeconds)
            window.Dequeue();
        var (startTime, startBytes) = window.Peek();
        var span = now - startTime;
        if (span >= 0.2 && bytes > startBytes)
            last = (bytes - startBytes) / span;
        else if (last <= 0 && now > 0)
            last = bytes / now; // seed on the first report, before a window exists
        return last;
    }

    /// <summary>Rounds to two significant digits so the label stops flickering in its last
    /// digit on every report.</summary>
    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
            return "";
        var step = Math.Pow(10, Math.Floor(Math.Log10(bytesPerSecond)) - 1);
        return $"{ByteFormat.Format((long)(Math.Round(bytesPerSecond / step) * step))}/s";
    }

    /// <summary>Estimated remaining time from the median of the newest speed samples. A mean
    /// follows every spike — one small file, one GC pause — and the estimate bounced around
    /// like a 1998 progress bar. The median ignores those outliers.</summary>
    private void UpdateEta(long remainingBytes, double fallbackSpeed)
    {
        if (remainingBytes <= 0)
        {
            _shownEta = null;
            RemainingText = "";
            return;
        }

        var rate = MedianSpeed(fallbackSpeed);
        if (rate <= 0)
            return;

        var eta = remainingBytes / rate;
        // Repaint only when the label visibly moves: 2% of its own magnitude, clamped to
        // 1-15 s. Short estimates stay responsive, long ones stop jittering every report.
        var threshold = Math.Clamp(eta * 0.02, 1.0, 15.0);
        if (_shownEta is { } shown && Math.Abs(shown - eta) < threshold)
            return;
        _shownEta = eta;
        RemainingText = eta >= 1 ? FormatEta(TimeSpan.FromSeconds(eta)) : "";
    }

    /// <summary>Median of the newest speed samples, in bytes per second. Below three samples
    /// there is no trend yet, so the live rate is used until then.</summary>
    private double MedianSpeed(double fallback)
    {
        var count = Math.Min(SpeedSamples.Count, EtaSampleCount);
        if (count < 3)
            return fallback;

        var values = new double[count];
        for (var i = 0; i < count; i++)
            values[i] = SpeedSamples[SpeedSamples.Count - count + i].MBps;
        Array.Sort(values);
        var median = count % 2 == 1
            ? values[count / 2]
            : (values[count / 2 - 1] + values[count / 2]) / 2.0;
        return median * 1024.0 * 1024.0;
    }

    private void AddSpeedSample(ObservableCollection<SpeedSample> target, ref double lastTime,
        double elapsed, double speed)
    {
        if (elapsed - lastTime < SpeedSampleIntervalSeconds)
            return;
        lastTime = elapsed;
        target.Add(new SpeedSample(elapsed, speed / (1024.0 * 1024.0)));
        while (target.Count > MaxSamples)
            target.RemoveAt(0);
        Log.Verbose("Speed sample @{Time:0.0}s: {Speed:0.0} MB/s", elapsed, speed / (1024.0 * 1024.0));
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
