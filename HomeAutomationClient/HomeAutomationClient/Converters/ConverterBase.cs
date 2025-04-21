using Avalonia.Data.Converters;

namespace De.Hochstaetter.HomeAutomationClient.Converters;

public abstract class ConverterBase : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

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

public abstract class EqualityConverterBase<TSource, TDestination>(TSource? value) : ConverterBase
{
    public TSource? Value { get; set; } = value;
    public TDestination? Equal { get; set; }
    public TDestination? NotEqual { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TSource v && v.Equals(Value) ? Equal : NotEqual;
    }
}
