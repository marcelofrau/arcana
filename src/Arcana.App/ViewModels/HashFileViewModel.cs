using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class HashFileViewModel : ObservableObject
{
    public string[] Algorithms { get; } = { "MD5", "SHA-1", "SHA-256", "SHA-512" };

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _algorithm = "SHA-256";

    public bool Confirmed { get; private set; }

    public void Confirm() => Confirmed = true;
}
