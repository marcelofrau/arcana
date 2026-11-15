using System.Collections.Generic;
using System.Linq;
using Arcana.App.ViewModels;
using Arcana.Core.Compression;
using FluentAssertions;
using Xunit;

namespace Arcana.App.Tests.ViewModels;

public class ProgressViewModelTests
{
    // The view model formats its labels through the localization manager; load the real
    // strings so assertions read what the dialog shows.
    static ProgressViewModelTests() => Arcana.App.Localization.LocalizationManager.Instance.LoadResources();

    [Fact]
    public void ByteReport_DrivesPercentSpeedAndChart()
    {
        var vm = new ProgressViewModel("test");

        vm.Update(new ProgressReport
        {
            CurrentFile = "a.bin",
            BytesProcessed = 512L * 1024 * 1024,
            TotalBytes = 1024L * 1024 * 1024,
            FilesProcessed = 1,
            TotalFiles = 10,
            CurrentOperation = "Extracting",
        });

        vm.OverallPercent.Should().BeApproximately(50, 0.001);
        vm.SpeedText.Should().EndWith("/s");
        vm.FilePercent.Should().BeNull();

        // The chart samples once per half second, so the second report must be what lands.
        Thread.Sleep(520);
        vm.Update(new ProgressReport
        {
            BytesProcessed = 768L * 1024 * 1024,
            TotalBytes = 100L * 1024 * 1024 * 1024,
            FilesProcessed = 2,
            TotalFiles = 10,
            CurrentOperation = "Extracting",
        });
        vm.SpeedSamples.Should().HaveCount(1);
        vm.RemainingText.Should().StartWith("~");
    }

    [Fact]
    public void ItemOnlyReport_StaysIndeterminateWithoutSpeed()
    {
        var vm = new ProgressViewModel("test");

        vm.Update(new ProgressReport { CurrentFile = "a.bin", FilesProcessed = 5, CurrentOperation = "Preparing" });

        vm.OverallPercent.Should().Be(-1);
        vm.SpeedText.Should().BeEmpty();
        vm.SpeedSamples.Should().BeEmpty();
    }

    [Fact]
    public void CompressedTotals_DriveRatioAndArchiveBar()
    {
        var vm = new ProgressViewModel("test");

        vm.Update(new ProgressReport
        {
            BytesProcessed = 1L * 1024 * 1024 * 1024,
            TotalBytes = 4L * 1024 * 1024 * 1024,
            CompressedBytes = 400L * 1024 * 1024,
            TotalCompressedBytes = 1600L * 1024 * 1024,
            FilesProcessed = 1,
            TotalFiles = 4,
            CurrentOperation = "Extracting",
        });

        vm.RatioText.Should().StartWith("2.6:1"); // 4096 MB plain / 1600 MB archive
        vm.HasArchiveBar.Should().BeTrue();
        vm.ArchivePercent.Should().BeApproximately(400.0 / 4096 * 100, 0.01);
    }

    [Fact]
    public void ReportWithoutCompressedTotals_HidesRatio()
    {
        var vm = new ProgressViewModel("test");

        vm.Update(new ProgressReport
        {
            BytesProcessed = 512L * 1024 * 1024,
            TotalBytes = 1024L * 1024 * 1024,
            FilesProcessed = 1,
            TotalFiles = 2,
            CurrentOperation = "Extracting",
        });

        vm.HasArchiveBar.Should().BeFalse();
        vm.RatioText.Should().BeEmpty();
    }

    [Fact]
    public void ArchiveSideReports_FillTheSecondSeries()
    {
        var vm = new ProgressViewModel("test");

        vm.Update(new ProgressReport
        {
            BytesProcessed = 512L * 1024 * 1024,
            TotalBytes = 1024L * 1024 * 1024,
            CompressedBytes = 128L * 1024 * 1024,
            TotalCompressedBytes = 256L * 1024 * 1024,
            FilesProcessed = 1,
            TotalFiles = 2,
            CurrentOperation = "Extracting",
        });

        vm.HasArchiveSpeed.Should().BeTrue();
        vm.ArchiveSpeedText.Should().EndWith("/s");

        Thread.Sleep(520);
        vm.Update(new ProgressReport
        {
            BytesProcessed = 768L * 1024 * 1024,
            TotalBytes = 1024L * 1024 * 1024,
            CompressedBytes = 192L * 1024 * 1024,
            TotalCompressedBytes = 256L * 1024 * 1024,
            FilesProcessed = 2,
            TotalFiles = 2,
            CurrentOperation = "Extracting",
        });

        vm.SpeedSamples.Should().HaveCount(1);
        vm.ArchiveSamples.Should().HaveCount(1);
        vm.ArchiveSamples[0].MBps.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Eta_IgnoresSingleBurst()
    {
        var vm = new ProgressViewModel("test");
        const long total = 1000L * 1024 * 1024 * 1024;
        long bytes = 0;

        for (var i = 0; i < 8; i++)
        {
            bytes += 50L * 1024 * 1024;
            vm.Update(new ProgressReport
            {
                BytesProcessed = bytes,
                TotalBytes = total,
                FilesProcessed = i + 1,
                TotalFiles = 100,
                CurrentOperation = "Extracting",
            });
            Thread.Sleep(260);
        }

        var steady = vm.RemainingText;
        steady.Should().StartWith("~");

        // One report jumps ten times ahead. A mean-based estimate collapses with it (the ETA
        // would drop ~10x); the median keeps the steady reading.
        bytes += 500L * 1024 * 1024;
        vm.Update(new ProgressReport
        {
            BytesProcessed = bytes,
            TotalBytes = total,
            FilesProcessed = 9,
            TotalFiles = 100,
            CurrentOperation = "Extracting",
        });

        EtaSeconds(vm.RemainingText).Should().BeGreaterThan(EtaSeconds(steady) * 0.8);
    }

    /// <summary>Parses "~1h 5m" / "~12m 30s" / "~45s" back to seconds.</summary>
    private static double EtaSeconds(string text)
    {
        var total = 0.0;
        foreach (var part in text.TrimStart('~').Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            total += part[^1] switch
            {
                'h' => int.Parse(part[..^1]) * 3600.0,
                'm' => int.Parse(part[..^1]) * 60.0,
                's' => int.Parse(part[..^1]),
                _ => 0,
            };
        }
        return total;
    }

    [Fact]
    public void SpeedText_StaysSteadyAcrossReports()
    {
        var vm = new ProgressViewModel("test");
        var texts = new List<string>();
        long bytes = 0;

        for (var i = 0; i < 6; i++)
        {
            bytes += 20L * 1024 * 1024;
            vm.Update(new ProgressReport
            {
                BytesProcessed = bytes,
                TotalBytes = 10L * 1024 * 1024 * 1024,
                FilesProcessed = i + 1,
                TotalFiles = 100,
                CurrentOperation = "Extracting",
            });
            texts.Add(vm.SpeedText);
            Thread.Sleep(120);
        }

        texts[^1].Should().EndWith("/s");
        // Two significant digits: steady input must not repaint a new value per report.
        texts.Distinct().Count().Should().BeLessThan(texts.Count);
    }
}
