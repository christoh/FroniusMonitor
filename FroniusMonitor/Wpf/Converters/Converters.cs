using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Converters;

public abstract class ConverterBase : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public abstract class MultiConverterBase : MarkupExtension, IMultiValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public abstract object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture);

    public virtual object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}


#if DEBUG
public class DoNothing : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
#endif

public class Sum : MultiConverterBase
{
    public override object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.OfType<IConvertible>().Select(v => v.ToDouble(CultureInfo.CurrentCulture)).Sum();
    }
}

public abstract class PlacementModeToAnything<T> : ConverterBase
{
    public T? Top { get; set; }
    public T? Bottom { get; set; }
    public T? Right { get; set; }
    public T? Left { get; set; }
    public T? Center { get; set; }
    public T? Null { get; set; }
    public T? Relative { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            PlacementMode.Top => Top,
            PlacementMode.Bottom => Bottom,
            PlacementMode.Right => Right,
            PlacementMode.Left => Left,
            PlacementMode.Center => Center,
            PlacementMode.Relative => Relative,
            _ => Null,
        };
    }
}

public class PlacementMode2Thickness : PlacementModeToAnything<Thickness> { }

public class PlacementMode2Double : PlacementModeToAnything<double> { }

public class PlacementMode2VerticalAlignment : PlacementModeToAnything<VerticalAlignment> { }

public class PlacementMode2TextAlignment : PlacementModeToAnything<TextAlignment> { }

public class DateConverter : ConverterBase
{
    public string StringFormat { get; set; } = "G";
    public bool UseUtc { get; set; } = false;
    public bool UseCurrentCulture { get; set; } = true;

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime date)
        {
            return null;
        }

        return (UseUtc ? date.ToUniversalTime() : date.ToLocalTime()).ToString(StringFormat, UseCurrentCulture ? CultureInfo.CurrentCulture : culture);
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var effectiveCulture = UseCurrentCulture ? CultureInfo.CurrentCulture : culture;

        return DateTime.TryParse
        (
            value is IConvertible convertible ? convertible.ToString(effectiveCulture) : value?.ToString(),
            effectiveCulture,
            UseUtc ? DateTimeStyles.AssumeUniversal : DateTimeStyles.AssumeLocal,
            out var date
        )
            ? date
            : value;
    }
}

public class NullToAnything<T> : ConverterBase
{
    public virtual T? Null { get; set; }
    public virtual T? NotNull { get; set; }
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is null ? Null : NotNull;
}

public class NullToVisibility : NullToAnything<Visibility>
{
    public override Visibility Null { get; set; } = Visibility.Collapsed;
    public override Visibility NotNull { get; set; } = Visibility.Visible;
}

public class NullToString : ConverterBase
{
    public string NullText { get; init; } = "---";

    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value ?? NullText;
    }
}

public class NullToBrush : NullToAnything<Brush>
{
    public override Brush? NotNull { get; set; } = Brushes.AntiqueWhite;
    public override Brush? Null { get; set; } = Brushes.LightGray;
}

public class NullToBool : NullToAnything<bool>
{
    public override bool NotNull { get; set; } = true;
    public override bool Null { get; set; } = false;
}

public class ToUpper : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value?.ToString()?.ToUpper(CultureInfo.CurrentCulture);
}

public abstract class GridMeterCorrectorBase : ConverterBase
{
    private readonly ISolarSystemService solarSystemService = IoC.GetRegistered<ISolarSystemService>();
    protected SmartMeterCalibrationHistoryItem? First, Last;
    private double lastOffsetCorrectedValue, factor;
    private int count;
    protected abstract EnergyDirection EnergyDirection { get; }

    public double FirstEnergyReal => GetEnergy(First!, EnergyDirection);
    public double FirstOffset => GetOffset(First!, EnergyDirection);
    public double LastEnergyReal => GetEnergy(Last!, EnergyDirection);
    public double LastOffset => GetOffset(Last!, EnergyDirection);

    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double doubleValue || !double.IsFinite(doubleValue))
        {
            return 0;
        }

        if (count != solarSystemService.SmartMeterHistory.Count)
        {
            count = solarSystemService.SmartMeterHistory.Count;
            First = solarSystemService.SmartMeterHistory.FirstOrDefault(item => double.IsFinite(GetOffset(item, EnergyDirection)));
            Last = solarSystemService.SmartMeterHistory.LastOrDefault(item => double.IsFinite(GetOffset(item, EnergyDirection)));

            if (First is not null && Last is not null)
            {
                lastOffsetCorrectedValue = LastEnergyReal + LastOffset;
                factor = (lastOffsetCorrectedValue - FirstEnergyReal - FirstOffset) / (LastEnergyReal - FirstEnergyReal);
                factor = double.IsFinite(factor) ? factor : 1;
            }
        }

        return Last is null
            ? doubleValue
            : Math.Round(lastOffsetCorrectedValue + (doubleValue + LastOffset - lastOffsetCorrectedValue) * factor, MidpointRounding.AwayFromZero);
    }

    private double GetEnergy(SmartMeterCalibrationHistoryItem item, EnergyDirection energyDirection) => energyDirection switch
    {
        EnergyDirection.Consumption => item.EnergyRealConsumed,
        EnergyDirection.Production => item.EnergyRealProduced,
        _ => throw new NotSupportedException($"{nameof(EnergyDirection)} must be either {nameof(EnergyDirection.Consumption)} or {nameof(EnergyDirection.Production)}")
    };

    private double GetOffset(SmartMeterCalibrationHistoryItem item, EnergyDirection energyDirection) => energyDirection switch
    {
        EnergyDirection.Consumption => item.ConsumedOffset,
        EnergyDirection.Production => item.ProducedOffset,
        _ => throw new NotSupportedException($"{nameof(EnergyDirection)} must be either {nameof(EnergyDirection.Consumption)} or {nameof(EnergyDirection.Production)}")
    };
}

public class GridMeterProductionCorrector : GridMeterCorrectorBase
{
    protected override EnergyDirection EnergyDirection => EnergyDirection.Production;
}

public class GridMeterConsumptionCorrector : GridMeterCorrectorBase
{
    protected override EnergyDirection EnergyDirection => EnergyDirection.Consumption;
}

public class SocToColor : ConverterBase
{
    private static readonly IReadOnlyList<BatteryColor> batteryColors = new BatteryColor[]
    {
        new(0, Colors.DarkRed),
        new(0.05, Colors.Red),
        new(0.25, Colors.Yellow),
        new(0.40, Colors.YellowGreen),
        new(0.5, Colors.LightGreen),
        new(1, Colors.LawnGreen),
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
                        color = batteryColors[0].Color;
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

public abstract class BoolToAnything<T> : ConverterBase
{
    public T? True { get; set; }
    public T? False { get; set; }
    public T? Null { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not bool boolValue ? Null : boolValue ? True : False;
    }
}

public class Bool2Brush : BoolToAnything<Brush> { }

//public class Bool2DataTemplate : BoolToAnything<DataTemplate> { }

public class Bool2Visibility : BoolToAnything<Visibility>
{
    public Bool2Visibility()
    {
        True = Visibility.Visible;
        False = Null = Visibility.Collapsed;
    }
}

public class Bool2String : BoolToAnything<string> { }

public class MultiBool2Anything<T> : MultiConverterBase
{
    public virtual T? All { get; set; }
    public virtual T? Any { get; set; }
    public virtual T? None { get; set; }
    public virtual T? Invalid { get; set; }

    public override object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValues = values.OfType<bool>().ToArray();

        if (values.Length != boolValues.Length)
        {
            return Invalid;
        }

        if (boolValues.All(b => b))
        {
            return All;
        }

        if (boolValues.Any(b => b))
        {
            return Any;
        }

        return None;
    }
}

public class MultiBool2Visibility : MultiBool2Anything<Visibility>
{
    public override Visibility Any { get; set; } = Visibility.Collapsed;
    public override Visibility All { get; set; } = Visibility.Visible;
    public override Visibility None { get; set; } = Visibility.Collapsed;
    public override Visibility Invalid { get; set; } = Visibility.Collapsed;
}

public class MultiBool2Bool : MultiBool2Anything<bool> { }

public class ModbusInterfaceRole2Visibility : MultiConverterBase
{
    public override object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var roles = values.OfType<ModbusInterfaceRole>().ToArray();

        if (values.Length != roles.Length)
        {
            return Visibility.Collapsed;
        }

        return roles.Any(r => r == ModbusInterfaceRole.Slave) ? Visibility.Visible : Visibility.Collapsed;
    }
}

public class CommonModbusSettingsVisibility : MultiConverterBase
{
    public override object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var roles = values.OfType<ModbusInterfaceRole>();
        var flags = values.OfType<bool>();

        var result = roles.Any(r => r == ModbusInterfaceRole.Slave) || flags.Any(b => b) ? Visibility.Visible : Visibility.Collapsed;
        return result;
    }
}

public class SeverityToVisibility : ConverterBase
{
    public Visibility Error { get; set; } = Visibility.Collapsed;
    public Visibility Warning { get; set; } = Visibility.Collapsed;
    public Visibility Information { get; set; } = Visibility.Collapsed;
    public Visibility Other { get; set; } = Visibility.Collapsed;

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Severity severity)
        {
            return null;
        }

        return severity switch
        {
            Severity.Error => Error,
            Severity.Information => Information,
            Severity.Warning => Warning,
            _ => Other,
        };
    }
}

public class Bool2Double : BoolToAnything<double> { }

public class GetTemperatureTicks : ConverterBase
{
    private readonly double tickDistance;

    public GetTemperatureTicks(double tickDistance)
    {
        this.tickDistance = tickDistance;
    }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Slider slider)
        {
            return null;
        }

        var list = new DoubleCollection { slider.Minimum };

        for (var current = Round(slider.Minimum); current < slider.Maximum; current += tickDistance)
        {
            list.Add(current);
        }

        list.Add(slider.Maximum);
        return list;


        double Round(double i) => Math.Ceiling(i / tickDistance) * tickDistance;
    }
}

public class OptimizationMode2Anything<T> : ConverterBase
{
    public virtual T? Automatic { get; set; }
    public virtual T? Manual { get; set; }
    public virtual T? Null { get; set; }


    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return
            value is not OptimizationMode mode
                ? Null
                : mode == OptimizationMode.Automatic
                    ? Automatic
                    : Manual;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not T tValue)
        {
            return null;
        }

        return tValue.Equals(Automatic) ? OptimizationMode.Automatic : tValue.Equals(Manual) ? OptimizationMode.Manual : null;
    }
}

public class OptimizationMode2Bool : OptimizationMode2Anything<bool> { }

public class OptimizationMode2Visibility : OptimizationMode2Anything<Visibility>
{
    public override Visibility Manual { get; set; } = Visibility.Visible;
    public override Visibility Automatic { get; set; } = Visibility.Collapsed;
}

public class SocLimits2Anything<T> : ConverterBase
{
    public virtual T? Override { get; set; }
    public virtual T? UseDefault { get; set; }
    public virtual T? Null { get; set; }


    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return
            value is not SocLimits limits
                ? Null
                : limits == SocLimits.Override
                    ? Override
                    : UseDefault;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not T tValue)
        {
            return null;
        }

        return tValue.Equals(Override) ? SocLimits.Override : tValue.Equals(UseDefault) ? SocLimits.UseManufacturerDefault : null;
    }
}

public class SocLimits2Bool : SocLimits2Anything<bool> { }

public class SocLimits2Visibility : SocLimits2Anything<Visibility>
{
    public override Visibility Override { get; set; } = Visibility.Visible;
    public override Visibility UseDefault { get; set; } = Visibility.Collapsed;
}

public class ToAbsolute : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IConvertible convertible ? Math.Abs(convertible.ToDouble(CultureInfo.CurrentCulture)) : null;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IConvertible convertible ? (int)Math.Round(-convertible.ToDouble(CultureInfo.CurrentCulture), MidpointRounding.AwayFromZero) : null;
    }
}

public class BoolInverter : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : null;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : null;
    }
}

public class Enum2DisplayName : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            var result = enumValue.ToDisplayName();
            return result;
        }

        return value;
    }
}

public class ShowFritzBoxIcon : ConverterBase, IMultiValueConverter
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return
            value is not WebConnection connection ||
            string.IsNullOrWhiteSpace(connection.BaseUrl) ||
            "http://".StartsWith(connection.BaseUrl)
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 3)
        {
            return Visibility.Collapsed;
        }

        var visibility = (Visibility)Convert(values[0], targetType, parameter, culture);

        return values[1] is true && values[2] is true ? visibility : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class SensorData : ConverterBase
{
    public string StringFormat { get; set; } = "G";
    public string Unit { get; set; } = string.Empty;
    public bool ForceCurrentCulture { get; set; } = true;
    public double Factor { get; set; } = 1;

    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var unitString = $"{(Unit == string.Empty ? string.Empty : " ")}{Unit}";

        if (value is null)
        {
            return $"---{unitString}";
        }

        var effectiveCulture = ForceCurrentCulture ? CultureInfo.CurrentCulture : culture;

        if (value is IConvertible convertible)
        {
            var doubleValue = convertible.ToDouble(effectiveCulture) * Factor;
            return $"{doubleValue.ToString(StringFormat, effectiveCulture)}{unitString}";
        }

        return $"{value}{unitString}";
    }
}

public class AccessMode2Bool : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not AccessMode accessMode ? null : accessMode == AccessMode.RequireAuth;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not bool boolValue ? null : boolValue ? AccessMode.RequireAuth : AccessMode.Everyone;
    }
}

public class Milliseconds2Minutes : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int intValue)
        {
            return value;
        }

        return intValue / 60000;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string stringValue || !int.TryParse(stringValue, NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var intValue))
        {
            return value;
        }

        return intValue * 60000;
    }
}

public class CarStatus2Visibility : ConverterBase
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is CarStatus.Idle or null ? Visibility.Visible : Visibility.Collapsed;
    }
}

public class CarStatus2Opacity : ConverterBase
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is CarStatus.Idle or null ? .5 : 1;
    }
}

//public class WattPilotPhase2Brush : ConverterBase
//{
//    public Phases Phase { get; set; }

//    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
//    {
//        Color result;

//        switch (value)
//        {
//            case WattPilot wattPilot:

//                var resultL1 = GetPhaseColor(Phases.L1, wattPilot.L1CableEnabled, wattPilot.L1ChargerEnabled);
//                var resultL2 = GetPhaseColor(Phases.L2, wattPilot.L2CableEnabled, wattPilot.L2ChargerEnabled);
//                var resultL3 = GetPhaseColor(Phases.L3, wattPilot.L3CableEnabled, wattPilot.L3ChargerEnabled);

//                var colors = new[] { resultL1, resultL2, resultL3 };

//                if (colors.Any(c => c == Colors.LightGreen))
//                {
//                    result = Colors.LightGreen;
//                }
//                else if (colors.Any(c => c == Color.FromRgb(248, 232, 19)))
//                {
//                    result = Color.FromRgb(248, 232, 19);
//                }
//                else if (colors.Any(c => c == Colors.White))
//                {
//                    result = Colors.White;
//                }
//                else if (colors.Any(c => c == Colors.OrangeRed))
//                {
//                    result = Colors.OrangeRed;
//                }
//                else
//                {
//                    result = Colors.Transparent;
//                }

//                Color GetPhaseColor(Phases phases, bool? cableEnabled, bool? carEnabled)
//                {
//                    if ((Phase & phases) != 0)
//                    {
//                        if (carEnabled is true && cableEnabled is true)
//                        {
//                            return Colors.LightGreen;
//                        }

//                        if (cableEnabled is true)
//                        {
//                            return Color.FromRgb(248, 232, 19);
//                        }

//                        if (carEnabled is true)
//                        {
//                            return Colors.White;
//                        }

//                        return Colors.OrangeRed;
//                    }

//                    return Colors.Transparent;
//                }

//                break;

//            default:
//                result = Colors.Transparent;
//                break;
//        }

//        return targetType.IsAssignableFrom(typeof(Color)) ? result : new SolidColorBrush(result);
//    }
//}

public class LinuxVersion : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Version version ? version.ToLinuxString() : value;
    }
}

public class PowerStatus2Brush : MultiConverterBase
{
    public override object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.OfType<bool>().ToList() is not { Count: 3 } list)
        {
            return null;
        }

        return !list[0] ? Brushes.OrangeRed : list[2] ? Brushes.Azure : list[1] ? Brushes.AntiqueWhite : Brushes.LightGray;
    }
}

public class EqualityToAnything<TFrom, TTo> : ConverterBase
{
    public TFrom? Value { get; set; }
    public TTo? Equal { get; set; }
    public TTo? NotEqual { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? Value is null ? Equal : NotEqual : value.Equals(Value) ? Equal : NotEqual;
    }
}

public class ByteEqualityToString : EqualityToAnything<byte, string> { }

public class ToshibaAcOperatingMode2Visibility : EqualityToAnything<ToshibaHvacOperatingMode, Visibility> { }

public class ToshibaAcOperatingMode2Brush : EqualityToAnything<ToshibaHvacOperatingMode, Brush> { }

public class ToshibaHvacWifiLedStatus2Brush : EqualityToAnything<ToshibaHvacWifiLedStatus, Brush> { }

public class ToshibaHvacSwingMode2Visibility : EqualityToAnything<ToshibaHvacSwingMode, Visibility> { }

public class ToshibaHvacSwingMode2Brush : EqualityToAnything<ToshibaHvacSwingMode, Brush>
{
    private static readonly ToshibaHvacSwingMode[] fixedModes =
    {
        ToshibaHvacSwingMode.Fixed1,
        ToshibaHvacSwingMode.Fixed2,
        ToshibaHvacSwingMode.Fixed3,
        ToshibaHvacSwingMode.Fixed4,
        ToshibaHvacSwingMode.Fixed5,
    };

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ToshibaHvacSwingMode swingMode && !fixedModes.Contains(swingMode))
        {
            return Equal;
        }

        return base.Convert(value, targetType, parameter, culture);
    }
}

public class MeritFeatureA2Visibility : EqualityToAnything<ToshibaHvacMeritFeaturesA, Visibility>
{
    public MeritFeatureA2Visibility()
    {
        Equal = Visibility.Visible;
        NotEqual = Visibility.Collapsed;
    }
}

public class Range2Anything<TFrom, TTo> : ConverterBase where TFrom : IComparable
{
    public TFrom? Minimum { get; set; }
    public TFrom? Maximum { get; set; }
    public TTo? InRange { get; set; }
    public TTo? OutOfRange { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not TFrom fromValue ? OutOfRange : fromValue.CompareTo(Minimum) >= 0 && fromValue.CompareTo(Maximum) <= 0 ? InRange : OutOfRange;
    }
}

public class ToshibaHvacMeritFeaturesARange2Visibility : Range2Anything<ToshibaHvacMeritFeaturesA, Visibility>
{
    public ToshibaHvacMeritFeaturesARange2Visibility()
    {
        InRange = Visibility.Visible;
        OutOfRange = Visibility.Collapsed;
    }
}

public class ToshibaHvacSilent2Visibility : ConverterBase
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ToshibaHvacMeritFeaturesA.Silent1 or ToshibaHvacMeritFeaturesA.Silent2 ? Visibility.Visible : Visibility.Collapsed;
    }
}

public class ToshibaMeritFeatureTemperature : MultiConverterBase
{
    public override object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 1)
        {
            return null;
        }

        if (values.Length < 2 || values[0] is not sbyte temperature || values[1] is not ToshibaHvacMeritFeaturesA meritFeaturesA || meritFeaturesA != ToshibaHvacMeritFeaturesA.Heating8C)
        {
            return values[0];
        }

        return (sbyte)(temperature - 16);
    }
}
