using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arcana.Core.Compression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using static Arcana.App.Localization.LocalizationManager;

namespace Arcana.App.ViewModels;

/// <summary>
/// Backs the extraction-complete dialog: summary counts (extracted / skipped /
/// overwritten), the list of extracted files (relative paths with a status badge)
/// and an "open folder" action.
/// </summary>
public partial class ExtractCompleteViewModel : ObservableObject
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ExtractCompleteViewModel>();

    /// <summary>Rows added per "load more" click. The list is a plain (non-virtualized)
    /// ItemsControl, so binding every file at once built one container per file and
    /// blocked the UI thread for minutes on a 250k-entry extraction.</summary>
    public const int PageSize = 200;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _summary;

    [ObservableProperty]
    private string _folderPath;

    public IReadOnlyList<ExtractedFile> Files { get; }

    /// <summary>Rows currently bound to the list; grows by <see cref="PageSize"/> per click.</summary>
    public ObservableCollection<ExtractedFile> ShownFiles { get; } = new();

    public int RemainingFiles => Files.Count - ShownFiles.Count;

    public bool HasMore => RemainingFiles > 0;

    public string LoadMoreText => T("msg.extractFilesLoadMore", Math.Min(PageSize, RemainingFiles), RemainingFiles);

    public bool HasFiles => Files.Count > 0;

    public ExtractCompleteViewModel(string title, string summary, string folderPath, IReadOnlyList<ExtractedFile> files)
    {
        _title = title;
        _summary = summary;
        _folderPath = folderPath;
        Files = files;
        LoadMore();
    }

    [RelayCommand]
    private void LoadMore()
    {
        var end = Math.Min(ShownFiles.Count + PageSize, Files.Count);
        for (var i = ShownFiles.Count; i < end; i++)
            ShownFiles.Add(Files[i]);
        OnPropertyChanged(nameof(RemainingFiles));
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(LoadMoreText));
        Log.Debug("Extract complete list now shows {Shown} of {Total} files", ShownFiles.Count, Files.Count);
    }
}
