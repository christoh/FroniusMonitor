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

/// <summary>
/// True while a reading lies between <see cref="Minimum"/> and <see cref="Maximum"/>, both ends included and
/// either of them optional. For hiding what a reading outside that range would turn into nonsense - the delta
/// frequency of an inverter that is not synchronized to the grid, for instance.
/// </summary>
public class IsInRange : ConverterBase
{
    public double Minimum { get; set; } = double.NegativeInfinity;

    public double Maximum { get; set; } = double.PositiveInfinity;

    /// <summary>
    /// What a value that is no number at all is worth, a null among them. True, because "nothing was reported"
    /// is not the same as "reported outside the range", and a gauge with no value shows its own dashes for it.
    /// </summary>
    public bool Unknown { get; set; } = true;

    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IConvertible convertible || convertible.GetTypeCode() is < TypeCode.SByte or > TypeCode.Decimal)
        {
            return Unknown;
        }

        var number = convertible.ToDouble(culture);
        return number >= Minimum && number <= Maximum;
    }
}
