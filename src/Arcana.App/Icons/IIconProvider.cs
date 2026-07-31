using Avalonia.Media;

namespace Arcana.App.Icons;

public interface IIconProvider
{
    string Name { get; }
    double ToolbarSize { get; }
    IImage? GetIcon(IconKey key);
}
