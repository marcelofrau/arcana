using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class ThemeMenuItem : ObservableObject
{
    public required string Name { get; init; }

    /// <summary>Optional display name; falls back to <see cref="Name"/>.</summary>
    public string? Label { get; init; }

    public required ICommand ApplyCommand { get; init; }

    [ObservableProperty]
    private bool _isCurrent;

    public string DisplayName => IsCurrent ? "✓ " + (Label ?? Name) : (Label ?? Name);
}
