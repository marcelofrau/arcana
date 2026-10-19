using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class InfoViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _message;

    /// <summary>Optional action button label. When null, only OK is shown.</summary>
    public string? ActionButtonText { get; init; }

    /// <summary>Invoked when the action button is pressed, before the dialog closes.</summary>
    public Action? Action { get; init; }
    public string? SecondActionButtonText { get; init; }
    public Action? SecondAction { get; init; }
    public string DismissText { get; init; } = Localization.LocalizationManager.T("common.ok");
    public Action? DismissAction { get; init; }
    public bool ShowApplyToAll { get; init; }
    [ObservableProperty]
    private bool _applyToAll;

    public InfoViewModel(string title, string message, string? actionButtonText = null, Action? action = null)
    {
        _title = title;
        _message = message;
        ActionButtonText = actionButtonText;
        Action = action;
    }
}
