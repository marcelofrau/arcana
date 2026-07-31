using System.IO;
using Arcana.Core.Filesystem;

namespace Arcana.App.Icons;

/// <summary>
/// Maps VFS nodes to icon keys. Archive root gets a file-archive icon, folders a
/// folder icon, files a type-based icon. Shared by the tree and the file list.
/// </summary>
public static class IconResolver
{
    public static IconKey ForNode(ArchiveNode node)
    {
        if (node.Parent == null)
            return IconKey.FileArchive;
        if (node.Type == NodeType.Directory)
            return IconKey.Folder;
        return ForExtension(Path.GetExtension(node.Name));
    }

    public static IconKey ForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".xz" or ".zst" or ".cab" or ".arj"
                => IconKey.FileArchive,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" or ".ico"
                => IconKey.FileImage,
            ".mp3" or ".wav" or ".flac" or ".mp4" or ".avi" or ".mkv" or ".mov" or ".ogg"
                => IconKey.FileMedia,
            ".txt" or ".md" or ".xml" or ".json" or ".csv" or ".cs" or ".js" or ".ts"
                or ".html" or ".css" or ".py" or ".sh" or ".sql" or ".yaml" or ".yml"
                => IconKey.FileCode,
            ".doc" or ".docx" or ".pdf" or ".rtf" or ".odt"
                => IconKey.FileDoc,
            _ => IconKey.FileGeneric,
        };
    }
}
