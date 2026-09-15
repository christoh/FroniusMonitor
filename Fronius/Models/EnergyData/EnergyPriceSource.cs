namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>
///     Where a market price came from. Awattar (tado° Energy since 2025) is the preferred source because it also has
///     the history and the price components; the Wattpilot carries the same day-ahead prices in its
///     <c>awpl</c> forecast and fills in when Awattar has not answered.
/// </summary>
public enum EnergyPriceSource : byte
{
    Awattar = 1,
    WattPilot = 2,
}
