using System.Globalization;
using Avalonia.Data.Converters;
using Arcana.App.Localization;
using Arcana.Core.Compression;

namespace Arcana.App.Converters;

/// <summary>Maps <see cref="ExtractStatus"/> to a localized short label (OK / Skipped / Overwritten).</summary>
public sealed class ExtractStatusTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            ExtractStatus.Extracted => LocalizationManager.T("common.ok"),
            ExtractStatus.Skipped => LocalizationManager.T("msg.extractStatusSkipped"),
            ExtractStatus.Overwritten => LocalizationManager.T("msg.extractStatusOverwritten"),
            _ => "",
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
