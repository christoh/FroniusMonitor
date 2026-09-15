namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>
///     The market price of one slot of the day-ahead auction, in cents per kWh, net of every tax and levy. An hour
///     until the market went to quarter hours; the slot length is whatever <see cref="EndTime" /> minus
///     <see cref="StartTime" /> says, so nothing here assumes an hour.
/// </summary>
public sealed class EnergyPricePoint
{
    /// <summary>Start of the slot, UTC.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>End of the slot, UTC.</summary>
    public DateTime EndTime { get; set; }

    /// <summary>The market price, cents per kWh, without VAT and without any component of the end user tariff.</summary>
    public decimal CentsPerKiloWattHour { get; set; }

    public EnergyPriceSource Source { get; set; }

    public override string ToString() => FormattableString.Invariant($"{StartTime:yyyy-MM-dd HH:mm}Z {CentsPerKiloWattHour:N3} ct/kWh ({Source})");
}
