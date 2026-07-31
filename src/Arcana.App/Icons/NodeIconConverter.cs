using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Arcana.Core.Filesystem;

namespace Arcana.App.Icons;

/// <summary>
/// Converts an ArchiveNode to its icon image, reading the current provider from
/// IconRuntime so rows re-render on theme switch.
/// </summary>
public sealed class NodeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ArchiveNode node)
            return IconRuntime.Current.GetIcon(IconResolver.ForNode(node));
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
