using System.Globalization;

namespace FroniusPhone.Converters;

public abstract class ConverterBase : IMarkupExtension, IValueConverter
{
    public object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public abstract class MultiConverterBase : IMarkupExtension, IMultiValueConverter
{
    public object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture);

    public virtual object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class LoadPowerConverter : MultiConverterBase
{
    public override object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if
        (
            values[0] is not double loadPower ||
            values[1] is not double powerLoss ||
            values[2] is not bool includeInverterPower
        )
        {
            return 0;
        }

        //LoadArrow.Power = PowerFlow.LoadPower - (ViewModel.IncludeInverterPower ? ViewModel.SolarSystemService.PowerLossAvg : 0);
        return loadPower - (includeInverterPower ? powerLoss : 0);
    }
}
