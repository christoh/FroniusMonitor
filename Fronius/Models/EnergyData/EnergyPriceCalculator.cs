namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>Which price the chart shows.</summary>
public enum EnergyPriceDisplay : byte
{
    /// <summary>The exchange price alone.</summary>
    Market = 0,

    /// <summary>What the end user pays: the exchange price plus every component of the tariff.</summary>
    Buy = 1,
}

/// <summary>
///     Turns a market price into what the chart shows. Shared by the server and the client so that a price the
///     server logs and a price the client draws are the same number.
/// </summary>
public static class EnergyPriceCalculator
{
    /// <summary>
    ///     The sum of the tariff components that are a price per kWh, net or gross. A component with any other unit
    ///     - a monthly base fee - is not part of a price per kWh and is left out.
    /// </summary>
    public static decimal ComponentsPerKiloWattHour(IEnumerable<EnergyPriceComponent> components, bool gross)
    {
        return components.Where(c => c.IsPerKiloWattHour).Sum(c => gross ? c.GrossPrice : c.NetPrice);
    }

    /// <summary>
    ///     The price of one slot as displayed, in cents per kWh.
    /// </summary>
    /// <param name="marketCentsPerKiloWattHour">The net market price of the slot.</param>
    /// <param name="components">The tariff components; only read for <see cref="EnergyPriceDisplay.Buy" />.</param>
    /// <param name="marketVatRate">The VAT rate of the market price itself, 0.19 for 19 %.</param>
    /// <param name="display">Market price alone or the whole buying price.</param>
    /// <param name="gross">With VAT or without.</param>
    /// <remarks>
    ///     Every component carries its own tax rate and the market price carries <paramref name="marketVatRate" />,
    ///     so gross is not one factor over the sum: a levy that is exempt from VAT stays as it is while the rest is
    ///     taxed. A negative market price is taxed like a positive one - the VAT on a negative amount is negative -
    ///     which is what the invoice does too.
    /// </remarks>
    public static decimal Display(decimal marketCentsPerKiloWattHour, IEnumerable<EnergyPriceComponent> components, decimal marketVatRate, EnergyPriceDisplay display, bool gross)
    {
        var market = gross ? marketCentsPerKiloWattHour * (1 + marketVatRate) : marketCentsPerKiloWattHour;

        return display switch
        {
            EnergyPriceDisplay.Market => market,
            EnergyPriceDisplay.Buy => market + ComponentsPerKiloWattHour(components, gross),
            _ => throw new ArgumentOutOfRangeException(nameof(display), display, null),
        };
    }

    /// <summary>
    ///     Whether <paramref name="prices" /> cover <paramref name="fromUtc" /> to <paramref name="toUtc" /> without a
    ///     gap: the slots add up to the span. Used to decide whether a day has to be fetched again.
    /// </summary>
    public static bool CoversSpan(IEnumerable<EnergyPricePoint> prices, DateTime fromUtc, DateTime toUtc)
    {
        var covered = TimeSpan.Zero;

        foreach (var price in prices)
        {
            var start = price.StartTime < fromUtc ? fromUtc : price.StartTime;
            var end = price.EndTime > toUtc ? toUtc : price.EndTime;

            if (end > start)
            {
                covered += end - start;
            }
        }

        return covered >= toUtc - fromUtc;
    }
}
