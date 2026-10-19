using System.Collections.Generic;

namespace Arcana.App.ViewModels.Wizard;

/// <summary>An archive format choice shown in create/convert wizards.</summary>
public sealed record FormatOption(string Label, string Value, string Extension);

public static class WizardFormats
{
    public static IReadOnlyList<FormatOption> All { get; } = new[]
    {
        new FormatOption("ZIP", "zip", ".zip"),
        new FormatOption("7Z", "7z", ".7z"),
        new FormatOption("ZSTD", "zstd", ".zst"),
        new FormatOption("TAR", "tar", ".tar"),
        new FormatOption("TAR + GZIP", "tar.gz", ".tar.gz"),
        new FormatOption("TAR + BZIP2", "tar.bz2", ".tar.bz2"),
        new FormatOption("TAR + XZ", "tar.xz", ".tar.xz"),
        new FormatOption("TAR + ZSTD", "tar.zst", ".tar.zst"),
    };

    public static FormatOption Default => All[0];
}

public sealed record CreateWizardResult(
    string Path, string Format, int Level, IReadOnlyList<string> Files, string Password);

public sealed record ExtractWizardResult(
    string DestinationDir, string SubfolderName, bool Overwrite, bool PreserveStructure, bool SmartSubfolder);

public sealed record ConvertWizardResult(string Format, int Level, string Path);
