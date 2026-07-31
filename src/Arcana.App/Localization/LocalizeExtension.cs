using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Arcana.App.Localization;

/// <summary>
/// Live localization markup extension: <c>{l:Loc Key=menu.file.openArchive}</c>.
/// Re-resolves when <see cref="LocalizationManager.Current"/> changes.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    public string? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
        => Key == null
            ? string.Empty
            : new Binding
            {
                Source = LocalizationManager.Instance,
                Path = nameof(LocalizationManager.Current),
                Mode = BindingMode.OneWay,
                Converter = LocConverter.Instance,
                ConverterParameter = Key,
            };
}
