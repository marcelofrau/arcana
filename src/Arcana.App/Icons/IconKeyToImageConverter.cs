using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Arcana.App.Icons;

public sealed class IconKeyToImageConverter : IValueConverter
{
    public static readonly IconKeyToImageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IconKey key)
            return IconRuntime.Current.GetIcon(key);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
