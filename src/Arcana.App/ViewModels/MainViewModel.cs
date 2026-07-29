using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Arcana.Core.Filesystem;

namespace Arcana.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private ObservableCollection<ArchiveNode> _archiveTree = [];

    [ObservableProperty]
    private object? _currentView;

    [RelayCommand]
    private async Task OpenArchive()
    {
        // TODO: Show file picker, call ArchiveService
        StatusText = "Opening archive...";
        await Task.Delay(100);
        StatusText = "Ready";
    }

    [RelayCommand]
    private void NewArchive()
    {
        // TODO: Show new archive dialog
    }

    [RelayCommand]
    private void OpenSplitTool()
    {
        StatusText = "Split tool opened";
    }

    [RelayCommand]
    private void OpenJoinTool()
    {
        StatusText = "Join tool opened";
    }

    [RelayCommand]
    private void OpenHashTool()
    {
        StatusText = "Hash calculator opened";
    }
}
