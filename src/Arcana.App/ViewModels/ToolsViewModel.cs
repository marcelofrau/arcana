using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Arcana.Core.Tools;
using Serilog;

namespace Arcana.App.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    private static readonly ILogger Log = Serilog.Log.ForContext<ToolsViewModel>();

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
        Log.Debug("SplitFile invoked (stub — not implemented yet)");
        Progress = 0;
        // TODO: Call FileSplitter
        await Task.Delay(100);
        Progress = 100;
    }

    [RelayCommand]
    private async Task JoinFiles()
    {
        Log.Debug("JoinFiles invoked (stub — not implemented yet)");
        Progress = 0;
        // TODO: Call FileJoiner
        await Task.Delay(100);
        Progress = 100;
    }

    [RelayCommand]
    private async Task ComputeHash()
    {
        Log.Debug("ComputeHash invoked (stub — not implemented yet)");
        // TODO: Call HashCalculator
        HashResult = "Pending...";
        await Task.Delay(100);
    }
}
