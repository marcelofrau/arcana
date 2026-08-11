using System.Collections.Generic;
using Arcana.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

/// <summary>
/// Backs the extraction-complete dialog: summary counts (extracted / skipped /
/// overwritten), the list of extracted files (relative paths with a status badge)
/// and an "open folder" action.
/// </summary>
public partial class ExtractCompleteViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _summary;

    [ObservableProperty]
    private string _folderPath;

    public IReadOnlyList<ExtractedFile> Files { get; }

    public bool HasFiles => Files.Count > 0;

    public ExtractCompleteViewModel(string title, string summary, string folderPath, IReadOnlyList<ExtractedFile> files)
    {
        _title = title;
        _summary = summary;
        _folderPath = folderPath;
        Files = files;
    }
}
