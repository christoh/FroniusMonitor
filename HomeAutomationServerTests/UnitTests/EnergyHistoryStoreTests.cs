using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.EnergyData;
using De.Hochstaetter.HomeAutomationServer.Services;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The SQLite history against a file of its own: what goes in comes out, writing the same slot again replaces it,
/// a forecast and a measurement of the same hour live side by side, and the tariff of a day is replaced as a whole.
/// </summary>
public sealed class EnergyHistoryStoreTests : IAsyncLifetime
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "HomeAutomationServerTests", Guid.NewGuid().ToString("N"));
    private EnergyHistoryStore store = null!;

    public async ValueTask InitializeAsync()
    {
        store = new EnergyHistoryStore(NullLogger<EnergyHistoryStore>.Instance, Path.Combine(directory, EnergyHistoryStore.FileName));
        await store.InitializeAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        // The pool holds the file open for a moment; the temp folder is not worth a retry loop.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Initializing_creates_the_folder_and_the_file_and_may_run_again()
    {
        Assert.True(File.Exists(store.FilePath));
        await store.InitializeAsync(TestContext.Current.CancellationToken);
        Assert.True(File.Exists(store.FilePath));
    }

    [Fact]
    public async Task Prices_come_back_exactly_and_a_slot_written_again_is_replaced()
    {
        var start = new DateTime(2026, 9, 13, 22, 0, 0, DateTimeKind.Utc);
        var token = TestContext.Current.CancellationToken;

        await store.UpsertPricesAsync(
        [
            new() { StartTime = start, EndTime = start.AddHours(1), CentsPerKiloWattHour = 12.345m, Source = EnergyPriceSource.Awattar },
            new() { StartTime = start.AddHours(1), EndTime = start.AddHours(2), CentsPerKiloWattHour = -0.001m, Source = EnergyPriceSource.Awattar },
            new() { StartTime = start, EndTime = start.AddHours(1), CentsPerKiloWattHour = 99m, Source = EnergyPriceSource.WattPilot },
        ], token);

        await store.UpsertPricesAsync([new() { StartTime = start, EndTime = start.AddHours(1), CentsPerKiloWattHour = 12.5m, Source = EnergyPriceSource.Awattar }], token);

        var awattar = await store.GetPricesAsync(EnergyPriceSource.Awattar, start, start.AddDays(1), token);
        Assert.Equal(2, awattar.Count);
        Assert.Equal(12.5m, awattar[0].CentsPerKiloWattHour);
        Assert.Equal(-0.001m, awattar[1].CentsPerKiloWattHour);
        Assert.Equal(DateTimeKind.Utc, awattar[0].StartTime.Kind);
        Assert.Equal(start.AddHours(1), awattar[0].EndTime);
        Assert.All(awattar, p => Assert.Equal(EnergyPriceSource.Awattar, p.Source));

        var wattPilot = await store.GetPricesAsync(EnergyPriceSource.WattPilot, start, start.AddDays(1), token);
        Assert.Equal(99m, Assert.Single(wattPilot).CentsPerKiloWattHour);

        // The span is start inclusive, end exclusive.
        Assert.Empty(await store.GetPricesAsync(EnergyPriceSource.Awattar, start.AddHours(2), start.AddDays(1), token));
        Assert.Single(await store.GetPricesAsync(EnergyPriceSource.Awattar, start, start.AddHours(1), token));
    }

    [Fact]
    public async Task The_tariff_of_a_day_keeps_its_order_and_is_replaced_as_a_whole()
    {
        var day = new DateOnly(2026, 9, 13);
        var token = TestContext.Current.CancellationToken;

        await store.UpsertPriceComponentsAsync(day,
        [
            new() { Name = "Netznutzung", Description = "Netznutzung", NetPrice = 4.72m, TaxRate = 0.19m },
            new() { Name = "Stromsteuer", Description = "Stromsteuer", NetPrice = 2.05m, TaxRate = 0.19m },
            new() { Name = "Grundpreis", Description = "Grundpreis Netz", NetPrice = 98.55m, TaxRate = 0.19m, Unit = "Euro/Jahr" },
        ], token);

        var components = await store.GetPriceComponentsAsync(day, token);
        Assert.Equal(["Netznutzung", "Stromsteuer", "Grundpreis"], components.Select(c => c.Name));
        Assert.Equal(4.72m, components[0].NetPrice);
        Assert.Equal(0.19m, components[0].TaxRate);
        Assert.Equal("Euro/Jahr", components[2].Unit);
        Assert.Empty(await store.GetPriceComponentsAsync(day.AddDays(1), token));

        await store.UpsertPriceComponentsAsync(day, [new() { Name = "Stromsteuer", Description = "Stromsteuer", NetPrice = 2.05m, TaxRate = 0.19m }], token);
        Assert.Equal("Stromsteuer", Assert.Single(await store.GetPriceComponentsAsync(day, token)).Name);
    }

    [Fact]
    public async Task Productions_are_kept_per_region()
    {
        var start = new DateTime(2026, 9, 13, 22, 0, 0, DateTimeKind.Utc);
        var token = TestContext.Current.CancellationToken;
        await store.UpsertProductionsAsync(AwattarCountry.GermanyLuxembourg, [new() { StartTime = start, EndTime = start.AddHours(1), SolarMegaWatt = 1.5, WindMegaWatt = 14619 }], token);
        await store.UpsertProductionsAsync(AwattarCountry.Austria, [new() { StartTime = start, EndTime = start.AddHours(1), SolarMegaWatt = 0, WindMegaWatt = 1 }], token);

        var germany = Assert.Single(await store.GetProductionsAsync(AwattarCountry.GermanyLuxembourg, start, start.AddDays(1), token));
        Assert.Equal(14619, germany.WindMegaWatt);
        Assert.Equal(1.5, germany.SolarMegaWatt);
        Assert.Equal(1, Assert.Single(await store.GetProductionsAsync(AwattarCountry.Austria, start, start.AddDays(1), token)).WindMegaWatt);
    }

    [Fact]
    public async Task A_forecast_and_a_measurement_of_the_same_hour_both_stay_and_a_value_is_never_overwritten_with_nothing()
    {
        var hour = new DateTime(2026, 9, 13, 9, 0, 0, DateTimeKind.Utc);
        var token = TestContext.Current.CancellationToken;

        await store.UpsertWeatherAsync("10870", [new() { Time = hour, IsForecast = true, GlobalRadiationWattsPerSquareMeter = 400, TemperatureCelsius = 15 }], token);
        await store.UpsertWeatherAsync("10870", [new() { Time = hour, IsForecast = false, GlobalRadiationWattsPerSquareMeter = 350 }], token);
        // The measurement of the wind arrives in a second file that says nothing about the radiation.
        await store.UpsertWeatherAsync("10870", [new() { Time = hour, IsForecast = false, WindSpeedMetersPerSecond = 3 }], token);
        // A newer forecast for the same hour replaces the old one.
        await store.UpsertWeatherAsync("10870", [new() { Time = hour, IsForecast = true, GlobalRadiationWattsPerSquareMeter = 420 }], token);
        await store.UpsertWeatherAsync("other", [new() { Time = hour, IsForecast = true, GlobalRadiationWattsPerSquareMeter = 1 }], token);

        var points = await store.GetWeatherAsync("10870", hour, hour.AddHours(1), token);
        Assert.Equal(2, points.Count);

        var measured = Assert.Single(points, p => !p.IsForecast);
        Assert.Equal(350, measured.GlobalRadiationWattsPerSquareMeter);
        Assert.Equal(3, measured.WindSpeedMetersPerSecond);
        Assert.Null(measured.TemperatureCelsius);

        var forecast = Assert.Single(points, p => p.IsForecast);
        Assert.Equal(420, forecast.GlobalRadiationWattsPerSquareMeter);
        Assert.Equal(15, forecast.TemperatureCelsius);
    }
}
