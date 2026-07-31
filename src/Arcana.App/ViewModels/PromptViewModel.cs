using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

public partial class PromptViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _value;

    public bool Confirmed { get; private set; }

    public PromptViewModel(string title, string initial = "")
    {
        _title = title;
        _value = initial;
    }

    [RelayCommand]
    private void Ok()
    {
        Confirmed = true;
    }
}
