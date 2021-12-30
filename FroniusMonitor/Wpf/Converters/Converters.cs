using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.FroniusMonitor.Unity;
using De.Hochstaetter.FroniusMonitor.Models;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Converters;

public abstract class ConverterBase : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    public virtual object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class NullToString : ConverterBase
{
    public string NullText { get; init; } = "---";

    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value ?? NullText;
    }
}

public class GridMeterConsumptionCorrector : ConverterBase
{
    private readonly Settings settings = IoC.Get<Settings>();

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IConvertible convertible)
        {
            return null;
        }

        return convertible.ToDouble(culture) + settings.ConsumedEnergyOffSetWatts;
    }
}

public class GridMeterProductionCorrector : ConverterBase
{
    private readonly Settings settings = IoC.Get<Settings>();

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IConvertible convertible)
        {
            return null;
        }

        return convertible.ToDouble(culture) + settings.ProducedEnergyOffsetWatts;
    }
}

public class SocToColor : ConverterBase
{
    private static readonly IReadOnlyList<BatteryColor> batteryColors = new BatteryColor[]
    {
        new (0, Colors.DarkRed),
        new (0.05, Colors.Red),
        new (0.3, Colors.Yellow),
        new (0.5, Colors.YellowGreen),
        new (0.7, Colors.LightGreen),
        new (1, Colors.LawnGreen),
    };

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Color color;

        switch (value)
        {
            case IConvertible convertible:
                var soc = convertible.ToDouble(culture);

                switch (soc)
                {
                    case <= 0:
                        color = batteryColors.First().Color;
                        break;

                    case >= 1:
                        color = batteryColors[^1].Color;
                        break;

                    default:
                        var lower = batteryColors.Last(bc => soc >= bc.Soc);
                        var upper = batteryColors.First(bc => bc.Soc > lower.Soc);
                        var percentage = (soc - lower.Soc) / (upper.Soc - lower.Soc);
                        color.R = Round(lower.Color.R, upper.Color.R);
                        color.G = Round(lower.Color.G, upper.Color.G);
                        color.B = Round(lower.Color.B, upper.Color.B);
                        color.A = 0xff;

                        byte Round(byte lowerColor, byte upperColor)
                        {
                            return (byte)Math.Round((1 - percentage) * lowerColor + percentage * upperColor);
                        }

                        break;
                }

                break;

            default:
                color = Colors.White;
                break;
        }

        return targetType.IsAssignableFrom(typeof(Color)) ? color : targetType.IsAssignableFrom(typeof(Brush)) ? new SolidColorBrush(color) : null;
    }
}
