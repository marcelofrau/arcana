using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class ConvertViewModel : ObservableObject
{
    public string[] Formats { get; } = { "zip", "7z", "zstd" };
    public int[] Levels { get; } = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    [ObservableProperty]
    private string _sourceName = "";

    [ObservableProperty]
    private string _format = "zip";

    [ObservableProperty]
    private int _level = 5;

    public bool Confirmed { get; private set; }

    public void Confirm() => Confirmed = true;
}
