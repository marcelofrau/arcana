using Arcana.Core.Compression;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class ProgressStreamTests
{
    private sealed class Collector : IProgress<ProgressReport>
    {
        public List<ProgressReport> Reports { get; } = [];

        public void Report(ProgressReport value) => Reports.Add(value);
    }

    [Fact]
    public async Task ExtractionStream_EstimatesTheArchiveSideFromTheRatio()
    {
        var collector = new Collector();
        await using (var stream = new ProgressStream(new MemoryStream(), collector, "big.bin",
                         totalBytes: 2000, operation: "Extracting", isOutput: true, leaveOpen: true,
                         compressedBaseBytes: 1000, compressedScale: 0.25))
        {
            await stream.WriteAsync(new byte[1000]);
            await stream.WriteAsync(new byte[1000]);
        }

        var last = collector.Reports[^1];
        last.BytesProcessed.Should().Be(2000);
        last.CompressedBytes.Should().Be(1500); // 1000 already extracted + 25% of these 2000
    }

    [Fact]
    public async Task ExtractionStream_WithoutARatio_ReportsNoArchiveBytes()
    {
        var collector = new Collector();
        await using (var stream = new ProgressStream(new MemoryStream(), collector, "solid.bin",
                         totalBytes: 100, operation: "Extracting", isOutput: true, leaveOpen: true))
        {
            await stream.WriteAsync(new byte[100]);
        }

        collector.Reports[^1].CompressedBytes.Should().Be(0);
    }
}
