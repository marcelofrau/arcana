using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Arcana.Core.Compression;

namespace Arcana.App.ViewModels;

public partial class ArchiveViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ArchiveEntry> _entries = [];

    [ObservableProperty]
    private ArchiveEntry? _selectedEntry;

    [ObservableProperty]
    private string _archivePath = string.Empty;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private long _totalSize;
}
