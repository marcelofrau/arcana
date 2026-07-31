using System;
using System.IO;
using Avalonia.Media.Imaging;
using Arcana.App.Icons;
using Arcana.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
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
    private bool _isBinaryPlaceholder;

    private MemoryStream? _imageStream;

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

            if (result.Image != null)
            {
                _imageStream = CopyToMemory(content);
                _imageStream.Position = 0;
                try
                {
                    Image = new Bitmap(_imageStream);
                }
                catch
                {
                    DisposeImage();
                }
            }
        }
        catch (Exception)
        {
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
        }
        catch (Exception)
        {
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
        _item = null;
        IsVisible = false;
        IsLoading = false;
        FileName = "";
        FileInfo = "";
        Kind = PreviewKind.None;
        TextContent = "";
        IsBinaryPlaceholder = false;
    }

    private void DisposeImage()
    {
        Image?.Dispose();
        Image = null;
        _imageStream?.Dispose();
        _imageStream = null;
    }

    private static MemoryStream CopyToMemory(Stream source)
    {
        source.Position = 0;
        var ms = new MemoryStream();
        source.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
