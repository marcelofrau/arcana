using System;
using Avalonia.Labs.Gif;
using Avalonia.Media.Imaging;
using Arcana.App.Icons;
using Arcana.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Arcana.App.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private static readonly ILogger Log = Serilog.Log.ForContext<PreviewViewModel>();

    private readonly PreviewService _preview;
    private FileEntryItem? _item;

    public PreviewViewModel(PreviewService preview)
    {
        _preview = preview;
    }

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private string _fileInfo = "";

    [ObservableProperty]
    private PreviewKind _kind = PreviewKind.None;

    [ObservableProperty]
    private string _textContent = "";

    [ObservableProperty]
    private Bitmap? _image;

    [ObservableProperty]
    private IGifSource? _gifSource;

    [ObservableProperty]
    private bool _isBinaryPlaceholder;

    [ObservableProperty]
    private string _previewError = "";

    public bool HasPreviewError => PreviewError.Length > 0;

    partial void OnPreviewErrorChanged(string value)
        => OnPropertyChanged(nameof(HasPreviewError));

    public IconKey PlaceholderIcon =>
        _item is { } item ? IconResolver.ForNode(item.Node) : IconKey.FileGeneric;

    public void Show(FileEntryItem? item)
    {
        Clear();

        if (item == null || item.IsDirectory || item.Node.ContentFactory == null)
            return;

        _item = item;
        IsVisible = true;
        FileName = item.Name;
        FileInfo = ByteFormat.Format(item.Node.OriginalSize);
        Kind = _preview.DetectKind(item.Name);
        Log.Debug("Showing preview for {File} (kind {Kind})", item.Name, Kind);

        if (Kind == PreviewKind.Hex)
        {
            IsBinaryPlaceholder = true;
            return;
        }

        IsLoading = true;
        try
        {
            using var content = item.Node.OpenRead();
            var result = _preview.LoadPreview(content, item.Name, item.Node.OriginalSize);
            Kind = result.Kind;
            FileInfo = result.Info;
            TextContent = result.Text;
            PreviewError = result.Error ?? "";

            if (result.GifSource != null)
            {
                DisposeGif();
                GifSource = result.GifSource;
            }
            else if (result.Image != null)
            {
                try
                {
                    Image = result.Image;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not build preview bitmap for {File}", item.Name);
                    PreviewError = ex.Message;
                    DisposeImage();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load preview for {File}", item.Name);
            PreviewError = ex.Message;
            IsBinaryPlaceholder = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void LoadBinary()
    {
        if (_item is not { Node.ContentFactory: { } } item)
            return;

        IsLoading = true;
        try
        {
            using var content = item.Node.OpenRead();
            var result = _preview.LoadHex(content, item.Node.OriginalSize);
            Kind = result.Kind;
            FileInfo = result.Info;
            TextContent = result.Text;
            IsBinaryPlaceholder = false;
            Log.Debug("Loaded hex view for {File}", item.Name);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read entry {File} for hex view", item.Name);
            TextContent = "(could not read entry)";
            IsBinaryPlaceholder = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Clear()
    {
        DisposeImage();
        DisposeGif();
        _item = null;
        IsVisible = false;
        IsLoading = false;
        FileName = "";
        FileInfo = "";
        Kind = PreviewKind.None;
        TextContent = "";
        IsBinaryPlaceholder = false;
        PreviewError = "";
    }

    private void DisposeImage()
    {
        Image?.Dispose();
        Image = null;
    }

    private void DisposeGif()
    {
        if (GifSource is IDisposable disposable)
            disposable.Dispose();
        GifSource = null;
    }
}
