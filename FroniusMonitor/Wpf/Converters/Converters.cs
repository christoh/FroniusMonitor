﻿using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Gen24;
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

public class DateConverter : ConverterBase
{
    public string StringFormat { get; set; } = "G";
    public bool UseCurrentCulture { get; set; } = true;
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime date)
        {
            return null;
        }

        return date.ToString(StringFormat, UseCurrentCulture ? CultureInfo.CurrentCulture : culture);
    }
}

public class NullToAnything<T> : ConverterBase
{
    public virtual T? Null { get; set; }
    public virtual T? NotNull { get; set; }
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)=>value is null? Null: NotNull;
}

public class NullToVisibility : NullToAnything<Visibility>
{
    public override Visibility Null { get; set; }= Visibility.Collapsed;
    public override Visibility NotNull { get; set; }=Visibility.Visible;
}


public class NullToString : ConverterBase
{
    public string NullText { get; init; } = "---";

    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value ?? NullText;
    }
}

public abstract class PowerCorrector : ConverterBase
{
    protected abstract double OffsetWatts { get; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not IConvertible convertible ? null : convertible.ToDouble(culture) + OffsetWatts;
    }
}

public class GridMeterConsumptionCorrector : PowerCorrector
{
    protected override double OffsetWatts => App.Settings.ConsumedEnergyOffsetWattHours;
}

public class GridMeterProductionCorrector : PowerCorrector
{
    protected override double OffsetWatts => App.Settings.ProducedEnergyOffsetWattHours;
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

public class Bool2Visibility : BoolToAnything<Visibility>
{
    public Bool2Visibility()
    {
        True = Visibility.Visible;
        False = Null = Visibility.Collapsed;
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

        var list = new DoubleCollection {slider.Minimum};

        for (var current = Round(slider.Minimum); current < slider.Maximum; current += tickDistance)
        {
            list.Add(current);
        }

        list.Add(slider.Maximum);
        return list;


        double Round(double i) => Math.Ceiling(i / tickDistance) * tickDistance;
    }
}

public class CountToVisibility:ConverterBase
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not IEnumerable<Storage> storages ? Visibility.Collapsed : storages.Any() ? Visibility.Visible : Visibility.Collapsed;
    }
}
