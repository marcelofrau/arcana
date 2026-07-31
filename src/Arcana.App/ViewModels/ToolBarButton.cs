using System.Windows.Input;
using Avalonia.Media;
using Arcana.App.Icons;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class ToolBarButton : ObservableObject
{
    public required IconKey Icon { get; init; }
    public required string Label { get; init; }
    public required string ToolTip { get; init; }
    public required ICommand Command { get; init; }

    [ObservableProperty]
    private IImage? _image;

    [ObservableProperty]
    private double _size = 24;
}
