using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.HomeAutomationClient.Models.Gen24;

namespace De.Hochstaetter.HomeAutomationClient.Converters;

public class StrictestLimitConverter : ConverterBase
{
    public bool IsMinimum { get; set; }
    public bool IsOnePhase { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double result = 24150;

        if (value is Gen24PowerLimits { ActivePower: { } activePower } && activePower.IsEnabled)
        {
            if (activePower.HardLimit.IsEnabled)
            {
                result = activePower.HardLimit.PowerLimit * (activePower.PhaseMode == PhaseMode.WeakestPhase ? 3d : 1d) / (IsOnePhase ? 3d : 1d);
            }

            if (activePower.SoftLimit.IsEnabled)
            {
                result = activePower.SoftLimit.PowerLimit * (activePower.PhaseMode == PhaseMode.WeakestPhase ? 3d : 1d) / (IsOnePhase ? 3d : 1d);
            }
        }

        return Math.Min(result, IsOnePhase ? 8050 : 24150) * (IsMinimum ? -1 : 1);
    }
}

public class AcPowerMinimum : MultiConverterBase
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not IConvertible maxAcPower || values[1] is not Gen24Storage)
        {
            return 0d;
        }

        return -maxAcPower.ToDouble(culture);
    }
}

public class LifeTimeEnergyToGaugeColors : MultiConverterBase
{
    public override object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = new List<ColorThreshold>([new ColorThreshold(0, Colors.Red), new ColorThreshold(1, Colors.Red)]);

        if (values.Count > 1 && values[0] is IConvertible wattPeakCurrentTracker && values[1] is IConvertible wattPeakTotal)
        {
            var optimumRelativeEnergy = wattPeakCurrentTracker.ToDouble(culture) / wattPeakTotal.ToDouble(culture);

            if (optimumRelativeEnergy is > 0 and < 1)
            {
                if (optimumRelativeEnergy - .2 > 0)
                {
                    result.Insert(result.Count - 1, new ColorThreshold(optimumRelativeEnergy - .2, Colors.OrangeRed));
                }

                if (optimumRelativeEnergy - .1 > 0)
                {
                    result.Insert(result.Count - 1, new ColorThreshold(optimumRelativeEnergy - .1, Colors.YellowGreen));
                }

                result.Insert(result.Count - 1, new ColorThreshold(optimumRelativeEnergy, Colors.Green));

                if (optimumRelativeEnergy + .1 < 1)
                {
                    result.Insert(result.Count - 1, new ColorThreshold(optimumRelativeEnergy + .1, Colors.YellowGreen));
                }

                if (optimumRelativeEnergy + .2 < 1)
                {
                    result.Insert(result.Count - 1, new ColorThreshold(optimumRelativeEnergy + .2, Colors.OrangeRed));
                }
            }
        }

        return result;
    }
}

public class MpptComparison : MultiConverterBase
{
    public string StringFormat { get; set; } = "N1";
    public string UnitName { get; set; } = "%";

    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not IConvertible powerMppt1 || values[1] is not IConvertible powerMppt2)
        {
            return targetType.IsAssignableFrom(typeof(double)) ? 0 : null;
        }

        var wattPeakMppt1 = values.Count >= 3 ? (values[2] as IConvertible ?? 1d).ToDouble(CultureInfo.CurrentCulture) : 1d;
        var wattPeakMppt2 = values.Count >= 4 ? (values[3] as IConvertible ?? 1d).ToDouble(CultureInfo.CurrentCulture) : 1d;

        var percentage = powerMppt2.ToDouble(culture) / wattPeakMppt2 / (powerMppt1.ToDouble(culture) / wattPeakMppt1) * 100;

        object result =
            targetType.IsAssignableFrom(typeof(double))
                ? percentage
                : $"{percentage.ToString(StringFormat, CultureInfo.CurrentCulture)} {UnitName}";

        return result;
    }
}

public class Gauge2Text : MultiConverterBase
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 1 || values[0] is not Gauge gauge)
        {
            return null;
        }

        return gauge.Value.ToString(gauge.StringFormat, CultureInfo.CurrentCulture) + " " + gauge.UnitName;
    }
}

public class DeltaConverter : MultiConverterBase
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 1)
        {
            return 0d;
        }

        if (values.Count < 2)
        {
            return values[0];
        }

        if (values[0] is not IConvertible minuend || values[1] is not IConvertible subtrahend)
        {
            return 0d;
        }

        return minuend.ToDouble(culture) - subtrahend.ToDouble(culture);
    }
}

public class LinuxVersion : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Version version ? version.ToLinuxString() : value;
    }
}

public class PowerStatus2Brush : MultiConverterBase
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.OfType<bool>().ToList() is not { Count: 3 } list)
        {
            return null;
        }

        return !list[0] ? Brushes.OrangeRed : list[2] ? Brushes.Azure : list[1] ? Brushes.AntiqueWhite : Brushes.LightGray;
    }
}

public class Gen24Status2PanelBrush : ConverterBase
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Gen24Status status ? status.ToPanelBrush() : Brushes.Gainsboro;
    }
}

public class Gen24Status2Brush : ConverterBase
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Gen24Status status ? status.ToBrush() : Brushes.Gainsboro;
    }
}

public class InverterDisplayMode2Bool : EqualityConverterBase<InverterDisplayMode, bool>
{
    public InverterDisplayMode2Bool(InverterDisplayMode value) : base(value)
    {
        Equal = true;
        NotEqual = false;
    }
}

public class MeterDisplayMode2Bool : EqualityConverterBase<MeterDisplayMode, bool>
{
    public MeterDisplayMode2Bool(MeterDisplayMode value) : base(value)
    {
        Equal = true;
        NotEqual = false;
    }
}

public class WattPilotDisplayMode2Bool : EqualityConverterBase<WattPilotDisplayMode, bool>
{
    public WattPilotDisplayMode2Bool(WattPilotDisplayMode value) : base(value)
    {
        Equal = true;
        NotEqual = false;
    }
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class InverterStatusLocalizeExtension : ConverterBase
{
    private static readonly IGen24LocalizationService gen24Localization = IoC.GetRegistered<IGen24LocalizationService>();

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not string statusStatusString
            ? value
            : gen24Localization.GetLocalizedString(Gen24LocalizationSection.Ui, $"INVERTER.DEVICESTATE.{statusStatusString}");
    }
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class PowerMeterStatusLocalizeExtension : ConverterBase
{
    private static readonly IGen24LocalizationService gen24Localization = IoC.GetRegistered<IGen24LocalizationService>();

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not string statusStatusString
            ? value
            : gen24Localization.GetLocalizedString(Gen24LocalizationSection.Ui, $"POWERMETER.DEVICESTATE.{statusStatusString}");
    }
}

public class StorageValueConverterBase:ConverterBase
{
    public string? PropertyName { get; set; }
    public double FallBackValue { get; set; }
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case Gen24Storage storage when PropertyName!=null:
            {
                var pi = typeof(Gen24Storage).GetProperty(PropertyName);
                return pi?.GetValue(storage)??FallBackValue;
            }

            default:
                return FallBackValue;
        }
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
