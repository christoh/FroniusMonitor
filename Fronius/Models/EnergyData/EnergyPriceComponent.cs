namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>
///     One line of the end user tariff on top of the market price: a grid fee, a levy, a tax, the provider's own
///     service fee. Awattar answers them for a postal code and a grid operator; the service fee is not in that
///     answer and is added from the settings.
/// </summary>
/// <remarks>
///     Not every component is per kWh - a base fee comes as Euro per month - so <see cref="Unit" /> travels with the
///     value and only the ones in <see cref="CentsPerKiloWattHourUnit" /> go into a price per kWh.
/// </remarks>
public sealed class EnergyPriceComponent
{
    /// <summary>The unit Awattar uses for a component that is a price per kWh.</summary>
    public const string CentsPerKiloWattHourUnit = "Cent/kWh";

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>The net amount in <see cref="Unit" />.</summary>
    public decimal NetPrice { get; set; }

    /// <summary>The VAT rate of this component, 0.19 for 19 %.</summary>
    public decimal TaxRate { get; set; }

    public string Unit { get; set; } = CentsPerKiloWattHourUnit;

    /// <summary>Whether this component is part of a price per kWh.</summary>
    public bool IsPerKiloWattHour => string.Equals(Unit, CentsPerKiloWattHourUnit, StringComparison.OrdinalIgnoreCase);

    public decimal GrossPrice => NetPrice * (1 + TaxRate);

    public override string ToString() => FormattableString.Invariant($"{Name}: {NetPrice:N4} {Unit} + {TaxRate:P1}");
}
