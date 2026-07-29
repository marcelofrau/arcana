using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Arcana.Core.Tools;

namespace Arcana.App.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _hashResult = string.Empty;

    [ObservableProperty]
    private int _progress;

    [RelayCommand]
    private async Task SplitFile()
    {
        Progress = 0;
        // TODO: Call FileSplitter
        await Task.Delay(100);
        Progress = 100;
    }

    [RelayCommand]
    private async Task JoinFiles()
    {
        Progress = 0;
        // TODO: Call FileJoiner
        await Task.Delay(100);
        Progress = 100;
    }

    [RelayCommand]
    private async Task ComputeHash()
    {
        // TODO: Call HashCalculator
        HashResult = "Pending...";
        await Task.Delay(100);
    }
}
