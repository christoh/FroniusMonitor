using Avalonia.Media.Immutable;

namespace De.Hochstaetter.HomeAutomationClient.Converters;

/// <summary>The colours of the Type 2 plug's contacts, one per <see cref="PhaseState"/>. The rule itself is <see cref="WattPilotPhases"/>.</summary>
internal static class PhaseBrushes
{
    private static readonly ImmutableSolidColorBrush cableOnly = new(Color.FromRgb(248, 232, 19));

    public static IBrush For(PhaseState state) => state switch
    {
        PhaseState.Charging => Brushes.LightGreen,
        PhaseState.Ready => Brushes.LightSalmon,
        PhaseState.ChargerOnly => Brushes.White,
        PhaseState.CableOnly => cableOnly,
        _ => Brushes.Transparent,
    };
}

/// <summary>One phase of the plug: cable enabled, charger enabled, current - in that order.</summary>
public class WattPilotPhaseBrush : MultiConverterBase
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        PhaseBrushes.For(values.Count < 3 ? PhaseState.Unknown : WattPilotPhases.Of(values[0] as bool?, values[1] as bool?, values[2] as double?));
}

/// <summary>N and PE of the plug: the three phases' triples in a row, L1 to L3, nine values.</summary>
public class WattPilotNeutralBrush : MultiConverterBase
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 9)
        {
            return Brushes.Transparent;
        }

        return PhaseBrushes.For(WattPilotPhases.Neutral(Phase(0), Phase(3), Phase(6)));

        PhaseState Phase(int offset) => WattPilotPhases.Of(values[offset] as bool?, values[offset + 1] as bool?, values[offset + 2] as double?);
    }
}
