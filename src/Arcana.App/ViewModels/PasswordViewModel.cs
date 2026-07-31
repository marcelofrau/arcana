using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

public partial class PasswordViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Set password";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _confirm = "";

    public bool Confirmed { get; private set; }

    public bool CanConfirm => Password.Length > 0 && Password == Confirm;

    [RelayCommand]
    private void Ok()
    {
        if (!CanConfirm)
            return;
        Confirmed = true;
    }
}
