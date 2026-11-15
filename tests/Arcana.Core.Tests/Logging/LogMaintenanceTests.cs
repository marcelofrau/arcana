using System.IO.Compression;
using Arcana.Core.Logging;
using FluentAssertions;

namespace Arcana.Core.Tests.Logging;

public class LogMaintenanceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "arcana-logmaint-" + Guid.NewGuid().ToString("N"));

    public LogMaintenanceTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    private string WriteLog(DateTime date, string content = "line")
    {
        var path = Path.Combine(_directory, $"arcana-{date:yyyyMMdd}.log");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void KeepsFiveNewestLogs_AndCompressesTheRest()
    {
        var dates = Enumerable.Range(1, 8).Select(day => new DateTime(2026, 1, day)).ToList();
        foreach (var date in dates) WriteLog(date, $"content {date:yyyyMMdd}");

        LogMaintenance.Run(_directory);

        var plain = Directory.GetFiles(_directory, "arcana-*.log").Select(Path.GetFileName).Order().ToList();
        var archives = Directory.GetFiles(_directory, "arcana-*.log.gz").Select(Path.GetFileName).Order().ToList();

        plain.Should().BeEquivalentTo(dates.Skip(3).Select(d => $"arcana-{d:yyyyMMdd}.log"));
        archives.Should().BeEquivalentTo(dates.Take(3).Select(d => $"arcana-{d:yyyyMMdd}.log.gz"));

        using var gzip = new GZipStream(File.OpenRead(Path.Combine(_directory, "arcana-20260101.log.gz")), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        reader.ReadToEnd().Should().Be("content 20260101");
    }

    [Fact]
    public void NeverTouchesTheActiveLogOfToday()
    {
        var today = WriteLog(DateTime.Now, "active");
        foreach (var day in Enumerable.Range(1, 8)) WriteLog(new DateTime(2026, 1, day));

        LogMaintenance.Run(_directory);

        File.Exists(today).Should().BeTrue();
        File.ReadAllText(today).Should().Be("active");
    }

    [Fact]
    public void DeletesArchivesBeyondTheLimit()
    {
        foreach (var day in Enumerable.Range(1, 20))
            File.WriteAllText(Path.Combine(_directory, $"arcana-202601{day:00}.log.gz"), "gz");

        LogMaintenance.Run(_directory);

        Directory.GetFiles(_directory, "arcana-*.log.gz").Should().HaveCount(LogMaintenance.KeepArchives);
        File.Exists(Path.Combine(_directory, "arcana-20260120.log.gz")).Should().BeTrue();
        File.Exists(Path.Combine(_directory, "arcana-20260101.log.gz")).Should().BeFalse();
    }

    [Fact]
    public void MissingDirectory_DoesNotThrow()
    {
        LogMaintenance.Run(Path.Combine(_directory, "missing"));
    }
}
