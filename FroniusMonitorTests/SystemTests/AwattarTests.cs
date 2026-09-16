using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.FroniusMonitor.Services;

namespace De.Hochstaetter.FroniusMonitorTests.SystemTests;

/// <summary>
/// Awattar's live price feed, asked the way the WPF app asks it.
/// </summary>
public sealed class AwattarTests
{
    private readonly IElectricityPriceService service = new AwattarService();

    [SystemFact]
    public async Task A_day_of_prices_is_twenty_four_whole_hours_in_UTC()
    {
        service.PriceRegion = AwattarCountry.GermanyLuxembourg;

        var result = (await service.GetElectricityPricesAsync(17.879m, 1.2257m)).ToList();

        Assert.Equal(24, result.Count);
        Assert.All(result, r => Assert.Equal(DateTimeKind.Utc, r.StartTime.Kind));
        Assert.All(result, r => Assert.Equal(DateTimeKind.Utc, r.EndTime.Kind));
        Assert.All(result, r => Assert.Equal(r.StartTime, r.EndTime.AddHours(-1)));
    }
}
