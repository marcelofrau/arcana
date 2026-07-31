using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class JoinFileViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstPart = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private int _partCount;

    public bool Confirmed { get; private set; }

    public void Confirm() => Confirmed = true;
}
