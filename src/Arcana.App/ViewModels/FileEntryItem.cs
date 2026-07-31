using System;
using System.Globalization;
using System.IO;
using Arcana.App.Icons;
using Arcana.App.Services;
using Arcana.Core.Filesystem;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

public partial class FileEntryItem : ObservableObject
{
    public ArchiveNode Node { get; }

    public FileEntryItem(ArchiveNode node)
    {
        Node = node;
    }

    public string Name => Node.Name;
    public bool IsDirectory => Node.Type == NodeType.Directory;
    public string Ext => IsDirectory ? "" : Path.GetExtension(Node.Name);

    public string TypeText => IsDirectory ? "Folder"
        : Ext.TrimStart('.').ToUpperInvariant();

    public string SizeText => IsDirectory ? "" : ByteFormat.Format(Node.OriginalSize);

    public string PackedText => IsDirectory ? "" : ByteFormat.Format(Node.CompressedSize);

    public string RatioText
    {
        get
        {
            if (IsDirectory || Node.OriginalSize <= 0 || Node.CompressedSize <= 0)
                return "";
            double ratio = (double)Node.CompressedSize / Node.OriginalSize * 100;
            return $"{ratio.ToString("0", CultureInfo.InvariantCulture)}%";
        }
    }

    public string ModifiedText =>
        Node.LastModified.Year >= 1980
            ? Node.LastModified.ToString("yyyy-MM-dd HH:mm")
            : "";

    public long SizeValue => Node.OriginalSize;
    public long PackedValue => Node.CompressedSize;
    public double RatioValue =>
        Node.OriginalSize > 0 && Node.CompressedSize > 0
            ? (double)Node.CompressedSize / Node.OriginalSize
            : 0;
    public string TypeValue => TypeText;

    public IconKey Icon
    {
        get
        {
            if (IsDirectory)
                return IconKey.Folder;

            var ext = Ext.ToLowerInvariant();
            if (ext is ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".xz" or ".zst" or ".cab" or ".arj")
                return IconKey.FileArchive;
            if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" or ".ico")
                return IconKey.FileImage;
            if (ext is ".mp3" or ".wav" or ".flac" or ".mp4" or ".avi" or ".mkv" or ".mov" or ".ogg")
                return IconKey.FileMedia;
            if (ext is ".txt" or ".md" or ".xml" or ".json" or ".csv" or ".cs" or ".js" or ".ts"
                or ".html" or ".css" or ".py" or ".sh" or ".sql" or ".yaml" or ".yml")
                return IconKey.FileCode;
            if (ext is ".doc" or ".docx" or ".pdf" or ".rtf" or ".odt")
                return IconKey.FileDoc;
            return IconKey.FileGeneric;
        }
    }

    public void Refresh()
    {
        OnPropertyChanged((string?)null);
    }
}
