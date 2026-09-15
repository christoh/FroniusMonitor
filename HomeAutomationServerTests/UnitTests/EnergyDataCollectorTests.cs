using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.EnergyData;
using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.Fronius.Services.DataCollectors;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// When <see cref="EnergyDataCollector"/> asks Awattar, what it publishes, and how the Wattpilot fills in. The
/// clock is the test's, the sources are fakes, the store is in memory; nothing here waits for a timer - the test
/// calls <see cref="EnergyDataCollector.TickAsync"/> itself.
/// </summary>
public sealed class EnergyDataCollectorTests : IAsyncDisposable
{
    /// <summary>A Sunday in September, 10:00 in Berlin (08:00 UTC): before the price window.</summary>
    private static readonly DateTimeOffset morning = new(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);

    private static readonly TimeZoneInfo berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly FakeAwattarClient awattar = new();
    private readonly FakeDwdWeatherClient dwd = new();
    private readonly InMemoryEnergyHistoryStore store = new();
    private readonly SettableTimeProvider clock = new(morning);
    private readonly IDataControlService controlService = new DataControlService(NullLogger<DataControlService>.Instance);
    private readonly List<DeviceUpdateEventArgs> updates = [];
    private EnergyDataCollector? collector;

    public EnergyDataCollectorTests()
    {
        controlService.DeviceUpdate += (_, e) => updates.Add(e);
    }

    public async ValueTask DisposeAsync()
    {
        if (collector != null)
        {
            await collector.DisposeAsync();
        }
    }

    [Fact]
    public async Task Starting_reads_today_and_tomorrow_once_and_publishes_them()
    {
        awattar.Prices.AddRange(Hourly(Day(13), 24, 10m));
        awattar.Productions.AddRange(Hourly(Day(13), 24, 10m).Select(p => new GridProductionPoint { StartTime = p.StartTime, EndTime = p.EndTime, SolarMegaWatt = 1000, WindMegaWatt = 2000 }));
        awattar.Components.Add(new EnergyPriceComponent { Name = "Netznutzung", Description = "Netznutzung", NetPrice = 4.72m, TaxRate = 0.19m });
        dwd.Forecast.Add(new WeatherPoint { Time = Day(13).AddHours(12), IsForecast = true, GlobalRadiationWattsPerSquareMeter = 500 });
        collector = Create(Settings());

        await collector.StartAsync(TestContext.Current.CancellationToken);
        await collector.TickAsync(TestContext.Current.CancellationToken);

        var span = Assert.Single(awattar.PriceSpans);
        Assert.Equal(Day(13), span.From);
        Assert.Equal(Day(15), span.To);

        var published = Assert.IsType<EnergyChartData>(controlService.Entities[EnergyChartData.DeviceId].Device);
        Assert.Same(collector.Current, published);
        Assert.Equal(24, published.Prices.Count);
        Assert.Equal(24, published.Productions.Count);
        Assert.Equal(0.19m, published.MarketVatRate);
        Assert.Equal("TESTSTATION", published.WeatherStationName);
        Assert.Single(published.Weather);

        // The tariff plus Awattar's own fee, which is not in Awattar's answer.
        Assert.Equal(2, published.PriceComponents.Count);
        Assert.Equal(1.5m, published.PriceComponents[1].NetPrice);
        Assert.Equal(0.19m, published.PriceComponents[1].TaxRate);
        Assert.Equal(1, awattar.ComponentRequests);
    }

    [Fact]
    public async Task Tomorrow_is_asked_for_only_in_the_window_and_only_every_quarter_of_an_hour()
    {
        awattar.Prices.AddRange(Hourly(Day(13), 24, 10m));
        collector = Create(Settings());
        await collector.StartAsync(TestContext.Current.CancellationToken);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, awattar.PriceRequests);

        // 11:59 Berlin: tomorrow is incomplete, but the window has not opened.
        clock.Now = morning.AddHours(1).AddMinutes(59);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, awattar.PriceRequests);

        // 12:00: asked, tomorrow only.
        clock.Now = morning.AddHours(2);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, awattar.PriceRequests);
        Assert.Equal((Day(14), Day(15)), awattar.PriceSpans[^1]);

        // 12:10: not yet again.
        clock.Now = morning.AddHours(2).AddMinutes(10);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, awattar.PriceRequests);

        // 12:15: again, and now Awattar has tomorrow.
        awattar.Prices.AddRange(Hourly(Day(14), 24, 20m));
        clock.Now = morning.AddHours(2).AddMinutes(15);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, awattar.PriceRequests);
        Assert.Equal(48, collector.Current!.Prices.Count);

        // 12:30, 14:45: tomorrow is complete, so Awattar is left alone.
        clock.Now = morning.AddHours(2).AddMinutes(30);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        clock.Now = morning.AddHours(4).AddMinutes(45);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, awattar.PriceRequests);
    }

    [Fact]
    public async Task The_wattpilot_fills_in_what_awattar_has_not_answered_and_gives_way_where_it_has()
    {
        awattar.Prices.AddRange(Hourly(Day(13), 24, 10m));
        collector = Create(Settings());
        await collector.StartAsync(TestContext.Current.CancellationToken);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(collector.Current!.Prices, p => p.Source == EnergyPriceSource.WattPilot);

        // The charger knows today and tomorrow, hourly.
        var wattPilot = new WattPilot
        {
            SerialNumber = "4711",
            ElectricityPrices = new WattPilotElectricityPrice
            {
                StartSeconds = new DateTimeOffset(Day(13)).ToUnixTimeSeconds(),
                IntervalSeconds = 3600,
                CentsPerKiloWattHour = [.. Enumerable.Range(0, 48).Select(i => 99m)],
            },
        };

        controlService.AddOrUpdate("wp", new ManagedDevice(wattPilot, null, typeof(IWattPilotService), true));
        await WaitFor(() => collector.Current!.Prices.Count == 48);

        var current = collector.Current!;
        Assert.Equal(24, current.Prices.Count(p => p.Source == EnergyPriceSource.Awattar));
        Assert.Equal(24, current.Prices.Count(p => p.Source == EnergyPriceSource.WattPilot));
        Assert.All(current.Prices.Where(p => p.StartTime < Day(14)), p => Assert.Equal(EnergyPriceSource.Awattar, p.Source));
        Assert.All(current.Prices.Where(p => p.StartTime >= Day(14)), p => Assert.Equal(99m, p.CentsPerKiloWattHour));
        Assert.True(current.HasWattPilotPrices);

        // Once Awattar has tomorrow, its numbers replace the charger's.
        awattar.Prices.AddRange(Hourly(Day(14), 24, 20m));
        clock.Now = morning.AddHours(2);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.All(collector.Current!.Prices, p => Assert.Equal(EnergyPriceSource.Awattar, p.Source));
    }

    [Fact]
    public async Task A_day_that_is_over_is_fetched_once_and_then_served_from_the_store()
    {
        awattar.Prices.AddRange(Hourly(Day(1), 24, 5m));
        collector = Create(Settings());
        await collector.StartAsync(TestContext.Current.CancellationToken);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        var requestsAfterStart = awattar.PriceRequests;

        var first = await collector.GetDayAsync(new DateOnly(2026, 9, 1), TestContext.Current.CancellationToken);
        Assert.Equal(24, first.Prices.Count);
        Assert.Equal(Day(1), first.From);
        Assert.Equal(Day(2), first.To);
        Assert.Equal(requestsAfterStart + 1, awattar.PriceRequests);

        awattar.Fails = true;
        var second = await collector.GetDayAsync(new DateOnly(2026, 9, 1), TestContext.Current.CancellationToken);
        Assert.Equal(24, second.Prices.Count);
        Assert.Equal(requestsAfterStart + 1, awattar.PriceRequests);
    }

    [Fact]
    public async Task A_refusal_by_awattar_keeps_what_the_store_has_and_the_weather_still_arrives()
    {
        awattar.Fails = true;
        dwd.Observations.Add(new WeatherPoint { Time = Day(13).AddHours(6), IsForecast = false, WindSpeedMetersPerSecond = 3 });
        collector = Create(Settings());
        await collector.StartAsync(TestContext.Current.CancellationToken);

        await collector.TickAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(collector.Current);
        Assert.Empty(collector.Current.Prices);
        Assert.Single(collector.Current.Weather);
    }

    [Fact]
    public async Task Without_the_settings_section_nothing_is_collected()
    {
        collector = Create(null);

        await collector.StartAsync(TestContext.Current.CancellationToken);
        await collector.TickAsync(TestContext.Current.CancellationToken);

        Assert.False(collector.IsEnabled);
        Assert.Null(collector.Current);
        Assert.Empty(controlService.Entities);
        Assert.Equal(0, store.Initializations);
    }

    [Fact]
    public async Task Stopping_withdraws_the_published_data()
    {
        awattar.Prices.AddRange(Hourly(Day(13), 24, 10m));
        collector = Create(Settings());
        await collector.StartAsync(TestContext.Current.CancellationToken);
        await collector.TickAsync(TestContext.Current.CancellationToken);
        Assert.Contains(EnergyChartData.DeviceId, controlService.Entities.Keys);

        await collector.StopAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(EnergyChartData.DeviceId, controlService.Entities.Keys);
        Assert.Contains(updates, u => u.DeviceAction == DeviceAction.Delete && u.Id == EnergyChartData.DeviceId);
    }

    [Fact]
    public void Merging_prefers_awattar_and_keeps_the_wattpilot_where_awattar_is_silent()
    {
        var awattarPrices = Hourly(Day(13), 24, 10m);
        var wattPilotPrices = Hourly(Day(13).AddHours(12), 36, 99m).Select(p => { p.Source = EnergyPriceSource.WattPilot; return p; }).ToList();

        var merged = EnergyDataCollector.MergePrices(awattarPrices, wattPilotPrices);

        Assert.Equal(48, merged.Count);
        Assert.Equal(Day(13), merged[0].StartTime);
        Assert.Equal(Day(14).AddHours(23), merged[^1].StartTime);
        Assert.Equal(24, merged.Count(p => p.Source == EnergyPriceSource.WattPilot));
        Assert.True(merged.Zip(merged.Skip(1)).All(pair => pair.First.StartTime < pair.Second.StartTime));
    }

    [Fact]
    public void Merging_weather_takes_the_measurement_over_the_forecast_of_the_same_hour()
    {
        var hour = Day(13).AddHours(9);
        var forecast = new WeatherPoint { Time = hour, IsForecast = true, GlobalRadiationWattsPerSquareMeter = 400 };
        var measured = new WeatherPoint { Time = hour, IsForecast = false, GlobalRadiationWattsPerSquareMeter = 350 };
        var later = new WeatherPoint { Time = hour.AddHours(1), IsForecast = true, GlobalRadiationWattsPerSquareMeter = 500 };

        var merged = EnergyDataCollector.MergeWeather([forecast, measured, later]);

        Assert.Equal(2, merged.Count);
        Assert.Same(measured, merged[0]);
        Assert.Same(later, merged[1]);
    }

    /// <summary>Midnight of a September day in Berlin, as UTC: 22:00 of the day before.</summary>
    private static DateTime Day(int day) => TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 9, day, 0, 0, 0, DateTimeKind.Unspecified), berlin);

    private static List<EnergyPricePoint> Hourly(DateTime from, int hours, decimal cents)
    {
        return [.. Enumerable.Range(0, hours).Select(i => new EnergyPricePoint { StartTime = from.AddHours(i), EndTime = from.AddHours(i + 1), CentsPerKiloWattHour = cents, Source = EnergyPriceSource.Awattar })];
    }

    private static EnergyDataSettings Settings() => new()
    {
        Bearer = "token",
        PostalCode = "85375",
        GridOperatorId = "9901068000001",
        DwdStationId = "10870",
        TimeZoneId = "Europe/Berlin",
    };

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.True(condition());
    }

    private EnergyDataCollector Create(EnergyDataSettings? settings)
    {
        var options = new ServiceCollection()
            .AddOptions()
            .Configure<EnergyDataCollectorParameters>(p =>
            {
                p.Settings = settings;
                // Long, so that the timer never fires during a test; the tests tick by hand.
                p.TickInterval = TimeSpan.FromHours(1);
            })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<EnergyDataCollectorParameters>>();

        return new EnergyDataCollector(NullLogger<EnergyDataCollector>.Instance, options, controlService, awattar, dwd, store, clock);
    }
}
