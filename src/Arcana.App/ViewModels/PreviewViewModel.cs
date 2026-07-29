using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string _contentType = string.Empty;

    [ObservableProperty]
    private string _textContent = string.Empty;

    [ObservableProperty]
    private bool _canEdit;

    [ObservableProperty]
    private bool _isImage;

    [ObservableProperty]
    private string _imageSource = string.Empty;
}
