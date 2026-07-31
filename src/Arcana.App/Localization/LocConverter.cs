using System.Globalization;
using Avalonia.Data.Converters;

namespace Arcana.App.Localization;

public sealed class LocConverter : IValueConverter
{
    public static LocConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => parameter is string key ? LocalizationManager.Instance.Get(key) : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
