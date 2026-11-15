using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Arcana.App.Converters;

public sealed class IsPositiveConverter : IValueConverter
{
    public static readonly IsPositiveConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return d >= 0;
        if (value is int i)
            return i >= 0;
        if (value is float f)
            return f >= 0;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}