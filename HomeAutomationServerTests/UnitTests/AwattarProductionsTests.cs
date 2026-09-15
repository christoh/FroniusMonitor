using System.Text.Json;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Services.EnergyData;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// Awattar's production feed as it really answers: every hour of the span asked for, and <c>null</c> for both
/// values in the hours beyond its forecast. Cut down from a live answer of 2026-09-15, where the 24 hours of the
/// second day were all null and the server rejected the whole thing.
/// </summary>
public sealed class AwattarProductionsTests
{
    private const string Answer = """
        {
          "object": "list",
          "data": [
            { "start_timestamp": 1789509600000, "end_timestamp": 1789513200000, "solar": 0, "wind": 348 },
            { "start_timestamp": 1789545600000, "end_timestamp": 1789549200000, "solar": 3376, "wind": 712 },
            { "start_timestamp": 1789596000000, "end_timestamp": 1789599600000, "solar": null, "wind": null }
          ],
          "url": "/de/v1/power/productions"
        }
        """;

    [Fact]
    public void The_hours_beyond_the_forecast_are_read_and_left_out()
    {
        var list = JsonSerializer.Deserialize<AwattarEnergyList>(Answer, AwattarClient.JsonOptions);

        Assert.NotNull(list);
        Assert.Equal(3, list.Energies.Count);
        Assert.False(list.Energies[2].HasValues);

        var productions = AwattarClient.ToProductions(list);

        Assert.Equal(2, productions.Count);
        Assert.Equal(new DateTime(2026, 9, 15, 22, 0, 0, DateTimeKind.Utc), productions[0].StartTime);
        Assert.Equal(0, productions[0].SolarMegaWatt);
        Assert.Equal(348, productions[0].WindMegaWatt);
        Assert.Equal(3376, productions[1].SolarMegaWatt);
        Assert.Equal(712, productions[1].WindMegaWatt);
    }

    [Fact]
    public void An_hour_with_one_value_missing_is_not_a_forecast_either()
    {
        Assert.False(new AwattarEnergy { SolarProductionMegaWatt = 1, WindProductionMegaWatt = null }.HasValues);
        Assert.False(new AwattarEnergy { SolarProductionMegaWatt = null, WindProductionMegaWatt = 1 }.HasValues);
        Assert.True(new AwattarEnergy { SolarProductionMegaWatt = 0, WindProductionMegaWatt = 0 }.HasValues);
    }
}
