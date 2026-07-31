using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class SplitFileViewModel : ObservableObject
{
    public double[] PartSizesMb { get; } = { 100, 650, 700, 1000, 2048, 4096 };

    [ObservableProperty]
    private string _sourcePath = "";

    [ObservableProperty]
    private string _destinationDir = "";

    [ObservableProperty]
    private double _partSizeMb = 100;

    [ObservableProperty]
    private bool _hjsplitMode = true;

    public bool Confirmed { get; private set; }

    public void Confirm() => Confirmed = true;
}
