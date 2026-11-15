using Arcana.Core.Compression;
using FluentAssertions;

namespace Arcana.Core.Tests.Compression;

public class ProgressThrottleTests
{
    [Fact]
    public void FirstCall_AlwaysReports()
    {
        new ProgressThrottle(countStep: 50).ShouldReport(1).Should().BeTrue();
    }

    [Fact]
    public void SmallIncrements_AreSkippedUntilCountStep()
    {
        var throttle = new ProgressThrottle(countStep: 50);
        throttle.ShouldReport(1).Should().BeTrue();
        throttle.ShouldReport(49).Should().BeFalse();
        throttle.ShouldReport(51).Should().BeTrue();
    }

    [Fact]
    public void CountStepOne_ReportsEveryItem()
    {
        var throttle = new ProgressThrottle(countStep: 1);
        throttle.ShouldReport(1).Should().BeTrue();
        throttle.ShouldReport(2).Should().BeTrue();
        throttle.ShouldReport(3).Should().BeTrue();
    }

    [Fact]
    public void TimeInterval_ReportsEvenWithoutCountStep()
    {
        var throttle = new ProgressThrottle(countStep: 1000);
        throttle.ShouldReport(1).Should().BeTrue();
        Thread.Sleep(120);
        throttle.ShouldReport(2).Should().BeTrue();
    }
}
