using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Arcana.Core.Compression;

namespace Arcana.App.Converters;

/// <summary>Maps <see cref="ExtractStatus"/> to a semantic brush (success / warning / accent).</summary>
public sealed class ExtractStatusBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            ExtractStatus.Extracted => Brush("AppSuccessBrush"),
            ExtractStatus.Skipped => Brush("AppWarningBrush"),
            ExtractStatus.Overwritten => Brush("AppAccentBrush"),
            _ => null,
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static IBrush? Brush(string key)
        => Application.Current?.Resources.TryGetResource(key, null, out var value) == true ? value as IBrush : null;
}
