namespace De.Hochstaetter.HomeAutomationClient.Converters;

/// <summary>
/// A power in watts as the power flow page prints it: whole watts below a kilowatt, two decimals above, and
/// "---" for nothing. Absolute unless <see cref="Signed"/> asks otherwise, because the card's state line says
/// which way the power goes. Nothing sets <see cref="Signed"/> today - the wire labels of the battery and the
/// grid did, and they are gone.
/// </summary>
public class PowerText : ConverterBase
{
    public bool Signed { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Format(value as double?, Signed, culture);

    public static string Format(double? watts, bool signed, CultureInfo culture)
    {
        if (watts is not { } power || !double.IsFinite(power))
        {
            return "---";
        }

        var shown = signed ? power : Math.Abs(power);
        var sign = shown < 0 ? "−" : string.Empty;
        shown = Math.Abs(shown);

        return shown >= 1000
            ? $"{sign}{(shown / 1000).ToString("N2", culture)} kW"
            : $"{sign}{shown.ToString("N0", culture)} W";
    }
}

/// <summary>
/// The caption of a card: the kind of a source, the name of a consumer, and "rest of house" for the one node
/// that stands for everything without a plug of its own.
/// </summary>
public class PowerFlowTitle : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not PowerFlowNode node ? null : node.Kind switch
    {
        PowerFlowNodeKind.Grid => Loc.Grid,
        PowerFlowNodeKind.Solar => Loc.Solar,
        PowerFlowNodeKind.Inverter => Loc.Inverter,
        PowerFlowNodeKind.Battery => Loc.Battery,
        PowerFlowNodeKind.House => Loc.House,
        PowerFlowNodeKind.RestOfHouse => Loc.RestOfHouse,
        _ => node.Name,
    };
}

/// <summary>
/// The state line of a source: what a battery or the grid is doing, and for a battery how full it is. Empty for
/// a node that has nothing to say, so the caller hides the line.
/// </summary>
public class PowerFlowStateText : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PowerFlowNode node)
        {
            return string.Empty;
        }

        var state = node.State switch
        {
            PowerFlowState.Idle => Loc.Idle,
            PowerFlowState.Charging => Loc.Charging,
            PowerFlowState.Discharging => Loc.Discharging,
            PowerFlowState.Standby => Loc.Standby,
            PowerFlowState.FeedIn => Loc.FeedIn,
            PowerFlowState.GridImport => Loc.GridImport,
            _ => string.Empty,
        };

        return node.StateOfCharge is { } soc ? $"{state} · {soc.ToString("P0", culture)}" : state;
    }
}

/// <summary>
/// Whether a card is on the AC side of the sources - the grid or an inverter - which are the cards that carry a
/// device name and are drawn a little wider for it.
/// </summary>
public class PowerFlowIsAcSource : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is PowerFlowNodeKind.Grid or PowerFlowNodeKind.Inverter;
}

/// <summary>Whether a node has a state line at all.</summary>
public class PowerFlowHasState : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is PowerFlowNode { State: not PowerFlowState.None };
}

/// <summary>A string that says something, for the visibility of an optional line.</summary>
public class NotEmpty : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !string.IsNullOrWhiteSpace(value as string);
}

/// <summary>
/// A length times a share of it, for a bar that fills part of a track: the track's width and a value from 0 to 1.
/// Nought where either is missing, so a battery that has not reported shows an empty bar and not a stripe.
/// </summary>
public class Fraction : MultiConverterBase
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not double length || values[1] is not double share || !double.IsFinite(length) || !double.IsFinite(share))
        {
            return 0d;
        }

        return Math.Clamp(share, 0, 1) * length;
    }
}

/// <summary>Upper case, for the small letter-spaced captions of the cards. A converter, because a text has no CSS.</summary>
public class UpperCase : ConverterBase
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value as string)?.ToUpper(culture);
}

/// <summary>
/// The icon of a card, from the kind of its node. The outlines are drawn on a 24 by 24 grid, stroked and not
/// filled, so one <c>Path</c> style sizes and colours all of them.
/// </summary>
public class PowerFlowIcon : ConverterBase
{
    private static readonly Dictionary<PowerFlowNodeKind, Geometry> icons = new()
    {
        [PowerFlowNodeKind.Grid] = Geometry.Parse("M12,2 L7,22 M12,2 L17,22 M8.5,9 H15.5 M6.5,15 H17.5 M4,22 H20"),
        [PowerFlowNodeKind.Solar] = Geometry.Parse("M16,12 A4,4 0 1 1 8,12 A4,4 0 1 1 16,12 M12,2 V4 M12,20 V22 M2,12 H4 M20,12 H22 M4.9,4.9 L6.3,6.3 M17.7,17.7 L19.1,19.1 M4.9,19.1 L6.3,17.7 M17.7,6.3 L19.1,4.9"),
        [PowerFlowNodeKind.Inverter] = Geometry.Parse("M20,12 A8,8 0 0 1 5.7,16.9 M4,12 A8,8 0 0 1 18.3,7.1 M18,3 V7 H14 M6,21 V17 H10"),
        [PowerFlowNodeKind.Battery] = Geometry.Parse("M9,4 H15 A2,2 0 0 1 17,6 V19 A2,2 0 0 1 15,21 H9 A2,2 0 0 1 7,19 V6 A2,2 0 0 1 9,4 Z M10,2 H14 M10,13 H14 M12,11 V15"),
        [PowerFlowNodeKind.House] = Geometry.Parse("M3,11 L12,3 L21,11 M5,10 V21 H19 V10 M10,21 V15 H14 V21"),
        [PowerFlowNodeKind.Car] = Geometry.Parse("M3,13 L5,8 H19 L21,13 V18 H3 Z M5,13 H19 M9,17 A1.5,1.5 0 1 1 6,17 A1.5,1.5 0 1 1 9,17 M18,17 A1.5,1.5 0 1 1 15,17 A1.5,1.5 0 1 1 18,17"),
        [PowerFlowNodeKind.Consumer] = Geometry.Parse("M9,3 V8 M15,3 V8 M6,8 H18 L17,13 A5,5 0 0 1 7,13 Z M12,18 V21"),
        [PowerFlowNodeKind.RestOfHouse] = Geometry.Parse("M7.5,12 A1.5,1.5 0 1 1 4.5,12 A1.5,1.5 0 1 1 7.5,12 M13.5,12 A1.5,1.5 0 1 1 10.5,12 A1.5,1.5 0 1 1 13.5,12 M19.5,12 A1.5,1.5 0 1 1 16.5,12 A1.5,1.5 0 1 1 19.5,12"),
    };

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is PowerFlowNodeKind kind && icons.TryGetValue(kind, out var geometry) ? geometry : null;
}
