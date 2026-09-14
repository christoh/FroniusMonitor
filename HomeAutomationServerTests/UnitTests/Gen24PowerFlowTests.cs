using De.Hochstaetter.Fronius.Models.Gen24;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The loss and the efficiency of an inverter as <see cref="Gen24PowerFlow"/> works them out. Signs are the
/// Gen24's: solar only ever comes in, the battery gives (positive) or takes (negative), and the AC side delivers
/// (positive) or draws (negative) - the latter while the battery is charged from the grid or from another
/// inverter's AC.
/// </summary>
public sealed class Gen24PowerFlowTests
{
    private static Gen24PowerFlow Flow(double solar, double storage, double ac) => new() { SolarPower = solar, StoragePower = storage, InverterAcPower = ac };

    [Theory]
    [InlineData(5000, -1000, 3800, 200, 0.96)]   // sun, part of it into the battery, the rest out as AC
    [InlineData(0, 2000, 1900, 100, 0.95)]       // night, the battery feeds the house
    [InlineData(0, -1900, -2000, 100, 0.95)]     // night, the grid charges the battery: AC in, DC out
    [InlineData(500, -1900, -1500, 100, 0.95)]   // dusk, the grid tops the panels up to charge the battery
    [InlineData(4000, 0, 3900, 100, 0.975)]      // plain sun to AC
    public void What_leaves_over_what_comes_in_whichever_way_the_battery_and_the_ac_side_flow(double solar, double storage, double ac, double loss, double efficiency)
    {
        var flow = Flow(solar, storage, ac);

        Assert.Equal(loss, flow.PowerLoss, 6);
        Assert.NotNull(flow.Efficiency);
        Assert.Equal(efficiency, flow.Efficiency.Value, 6);
    }

    [Fact]
    public void Without_input_worth_measuring_there_is_no_efficiency()
    {
        Assert.Null(Flow(0, 0, 0).Efficiency);
        // A few watts at dawn would make the ratio noise.
        Assert.Null(Flow(solar: 5, storage: 0, ac: 4).Efficiency);
        Assert.NotNull(Flow(solar: 50, storage: 0, ac: 45).Efficiency);
    }
}
