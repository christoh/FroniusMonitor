using System.Globalization;
using Avalonia.Data.Converters;

namespace De.Hochstaetter.HomeAutomationClient.Converters;

public abstract class ConverterBase : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(ConvertBack)} is not supported for {GetType().Name}");
    }
}

public abstract class MultiConverterBase : MarkupExtension, IMultiValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture);
}
