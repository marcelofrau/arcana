using System;
using System.Collections.Generic;
using Arcana.App.Icons;
using Arcana.App.Services;

namespace Arcana.App.ViewModels;

/// <summary>A single labelled row in the properties list.</summary>
public sealed record PropertyRow(string Label, string Value);

/// <summary>
/// Data for the archive/file properties dialog: header (icon, title, description),
/// a labelled property list and the sizes feeding the WinACE-style cylinder chart.
/// </summary>
public sealed class PropertiesViewModel
{
    public string Title { get; init; } = "";
    public IconKey Icon { get; init; } = IconKey.FileGeneric;
    public string Description { get; init; } = "";
    public IReadOnlyList<PropertyRow> Rows { get; init; } = [];

    public long OriginalBytes { get; init; }
    public long CompressedBytes { get; init; }
    public string OriginalText => ByteFormat.Format(OriginalBytes);
    public string CompressedText => ByteFormat.Format(CompressedBytes);

    /// <summary>Compressed / original in 0..1 (1 = no savings).</summary>
    public double Ratio => OriginalBytes > 0 ? Math.Clamp((double)CompressedBytes / OriginalBytes, 0, 1) : 1;

    public double SavingsRatio => 1 - Ratio;
}
