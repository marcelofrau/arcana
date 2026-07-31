using System;
using System.IO;
using Avalonia.Media.Imaging;
using Arcana.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly PreviewService _preview;

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

    private MemoryStream? _imageStream;

    public void Show(FileEntryItem? item)
    {
        Clear();

        if (item == null || item.IsDirectory || item.Node.ContentFactory == null)
            return;

        IsVisible = true;
        IsLoading = true;
        FileName = item.Name;
        Kind = _preview.DetectKind(item.Name);

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
            Kind = PreviewKind.Hex;
            TextContent = "(could not read entry)";
            FileInfo = ByteFormat.Format(item.Node.OriginalSize);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Clear()
    {
        DisposeImage();
        IsVisible = false;
        IsLoading = false;
        FileName = "";
        FileInfo = "";
        Kind = PreviewKind.None;
        TextContent = "";
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
