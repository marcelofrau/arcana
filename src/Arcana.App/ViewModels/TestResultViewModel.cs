using System;
using System.Collections.Generic;
using System.Linq;
using Arcana.App.Services;

namespace Arcana.App.ViewModels;

/// <summary>
/// Data for the archive integrity-test summary dialog: totals, pass percentage
/// and the list of failed entries (empty when everything passed).
/// </summary>
public sealed class TestResultViewModel
{
    public string Title { get; init; } = "";
    public int Total { get; init; }
    public int Ok { get; init; }
    public int Failed => Total - Ok;
    public bool Passed => Failed == 0;
    public double PassPercent => Total > 0 ? (double)Ok / Total * 100 : 100;
    public double ProgressMax => Math.Max(Total, 1);
    public IReadOnlyList<TestResult> FailedItems { get; init; } = [];
    public bool HasFailures => FailedItems.Count > 0;
    public bool HasMultipleFailures => FailedItems.Count > 1;
}
