using System;
using System.Collections.Generic;
using Avalonia.Media;
using Serilog;

namespace Arcana.App.Icons;

/// <summary>
/// Splits icon resolution by context: filesystem keys (folders and file
/// mimetypes) always come from the Papirus provider, while action keys
/// (toolbar, sort arrows, save/settings/help) come from the selected theme.
/// Lets WinRAR-style themes and the built-in action themes supply toolbar
/// icons while the file list keeps the colorful Papirus mimetypes.
/// </summary>
public sealed class CompositeIconProvider : IIconProvider
{
    private static readonly ILogger Log = Serilog.Log.ForContext<CompositeIconProvider>();

    private static readonly IReadOnlySet<IconKey> FileSystemKeys =
        new HashSet<IconKey>
        {
            IconKey.Folder,
            IconKey.FileGeneric,
            IconKey.FileArchive,
            IconKey.FileImage,
            IconKey.FileCode,
            IconKey.FileMedia,
            IconKey.FileDoc,
            IconKey.Rar,
        };

    public IIconProvider FileSystemProvider { get; }
    public IIconProvider ActionProvider { get; }

    public string Name => ActionProvider.Name;
    public double ToolbarSize => ActionProvider.ToolbarSize;

    public CompositeIconProvider(IIconProvider filesystemProvider, IIconProvider actionProvider)
    {
        FileSystemProvider = filesystemProvider;
        ActionProvider = actionProvider;
    }

    public IImage? GetIcon(IconKey key)
    {
        if (FileSystemKeys.Contains(key))
            return FileSystemProvider.GetIcon(key);
        return ActionProvider.GetIcon(key);
    }
}
