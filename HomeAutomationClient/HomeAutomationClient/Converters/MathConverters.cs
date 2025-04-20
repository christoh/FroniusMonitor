namespace De.Hochstaetter.HomeAutomationClient.Converters;

public class Multiply : ConverterBase
{
    public bool UseConverterCulture { get; set; }
    public double Factor { get; set; } = 100;

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not IConvertible convertible ? value : convertible.ToDouble(UseConverterCulture ? culture : CultureInfo.CurrentCulture) * Factor;
    }
}
