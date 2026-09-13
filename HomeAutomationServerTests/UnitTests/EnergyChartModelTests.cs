using System.Text.Json;
using System.Text.Json.Serialization;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.EnergyData;
using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the client makes of the server's data: the bars, their labels and the axes of <see cref="EnergyChartModel"/>,
/// and that the data survives the trip over the wire with the options both ends use.
/// </summary>
public sealed class EnergyChartModelTests
{
    private static readonly DateTime dayStart = new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Local);

    [Fact]
    public void Prices_become_bars_with_their_value_written_on_them_and_negative_ones_go_to_their_own_series()
    {
        var data = Data(prices: [10m, -1m, 30m]);

        var model = EnergyChartModel.Build(data, dayStart, dayStart.AddDays(1), EnergyPriceDisplay.Market, gross: false, showProductions: false, showWeather: false);

        Assert.Equal(2, model.PositivePrices.Count);
        var negative = Assert.Single(model.NegativePrices);
        Assert.Equal(-1, negative.Value);
        Assert.Equal(dayStart.AddHours(1), negative.Start);
        Assert.Equal(dayStart.AddHours(2), negative.End);
        Assert.Equal((-1.0).ToString("N2"), negative.Label);
        Assert.Equal(30.ToString("N2"), model.PositivePrices[1].Label);

        // The axis starts below the lowest price and ends 10 % above the highest where nothing hangs from the top.
        Assert.Equal(-1.1, model.PriceAxisMinimum, 6);
        Assert.Equal(30 + (30 + 1.1) * 0.1 * 1.1, model.PriceAxisMaximum, 6);
        Assert.Equal(dayStart, model.AxisStart);
        Assert.Equal(dayStart.AddHours(3), model.AxisEnd);
        Assert.False(model.HasProductions);
        Assert.False(model.HasWeather);
        Assert.True(model.HasPrices);
    }

    [Fact]
    public void The_buying_price_adds_the_components_and_the_title_names_the_wattpilot_where_it_filled_in()
    {
        var data = Data(prices: [10m]);
        data.Prices[0].Source = EnergyPriceSource.WattPilot;
        data.PriceComponents.Add(new EnergyPriceComponent { Name = "Netz", NetPrice = 5m, TaxRate = 0.19m });

        var gross = EnergyChartModel.Build(data, dayStart, dayStart.AddDays(1), EnergyPriceDisplay.Buy, gross: true, showProductions: false, showWeather: false);
        var net = EnergyChartModel.Build(data, dayStart, dayStart.AddDays(1), EnergyPriceDisplay.Buy, gross: false, showProductions: false, showWeather: false);

        Assert.Equal((double)(10m * 1.19m + 5m * 1.19m), Assert.Single(gross.PositivePrices).Value, 6);
        Assert.Equal(15, Assert.Single(net.PositivePrices).Value, 6);
        Assert.Contains(Fronius.Localization.Resources.PricesFromWattPilot, gross.Title);
        Assert.Contains(AwattarCountry.GermanyLuxembourg.ToDisplayName(), gross.Title);
    }

    [Fact]
    public void Productions_hang_from_the_top_as_negative_gigawatts_with_the_wind_stacked_on_the_sun()
    {
        var data = Data(prices: [10m, 10m]);
        data.Productions.Add(new GridProductionPoint { StartTime = data.Prices[0].StartTime, EndTime = data.Prices[0].EndTime, SolarMegaWatt = 1000, WindMegaWatt = 2000 });
        data.Productions.Add(new GridProductionPoint { StartTime = data.Prices[1].StartTime, EndTime = data.Prices[1].EndTime, SolarMegaWatt = 500, WindMegaWatt = 500 });

        var model = EnergyChartModel.Build(data, dayStart, dayStart.AddDays(1), EnergyPriceDisplay.Market, gross: false, showProductions: true, showWeather: false);

        Assert.True(model.HasProductions);
        Assert.Equal(-1, model.Solar[0].Value);
        Assert.Equal(-1, model.Wind[0].Base);
        Assert.Equal(-3, model.Wind[0].Value);
        Assert.Equal(3.0.ToString("0.0"), model.Wind[0].Label);
        Assert.Null(model.Solar[0].Label);
        Assert.Equal(-9, model.ProductionAxisMinimum);
        // With productions the price axis leaves 60 % of room at the top.
        Assert.Equal(10 + 10 * 0.6 * 1.1, model.PriceAxisMaximum, 6);

        var without = EnergyChartModel.Build(data, dayStart, dayStart.AddDays(1), EnergyPriceDisplay.Market, gross: false, showProductions: false, showWeather: false);
        Assert.False(without.HasProductions);
    }

    [Fact]
    public void Weather_splits_into_measured_and_forecast_lines_and_only_the_hours_of_the_day_count()
    {
        var data = Data(prices: [10m]);
        data.Weather.Add(new WeatherPoint { Time = data.Prices[0].StartTime, IsForecast = false, GlobalRadiationWattsPerSquareMeter = 300, WindSpeedMetersPerSecond = 2 });
        data.Weather.Add(new WeatherPoint { Time = data.Prices[0].StartTime.AddHours(1), IsForecast = true, GlobalRadiationWattsPerSquareMeter = 600 });
        data.Weather.Add(new WeatherPoint { Time = data.Prices[0].StartTime.AddDays(2), IsForecast = true, GlobalRadiationWattsPerSquareMeter = 999 });

        var model = EnergyChartModel.Build(data, dayStart, dayStart.AddDays(1), EnergyPriceDisplay.Market, gross: false, showProductions: false, showWeather: true);

        Assert.True(model.HasWeather);

        // The measured hour sits at its middle, and the line is carried to the start of the day; the forecast
        // starts where the measured line ends and is carried to the end of the day - the point two days later is
        // too far away to interpolate towards.
        Assert.Equal([(dayStart, 300.0), (dayStart.AddMinutes(30), 300.0)], model.RadiationMeasured.Select(p => (p.Time, p.Value)));
        Assert.Equal([(dayStart.AddMinutes(30), 300.0), (dayStart.AddMinutes(90), 600.0), (dayStart.AddDays(1), 600.0)], model.RadiationForecast.Select(p => (p.Time, p.Value)));

        // Wind is the state at the start of the hour, so the first point is on the edge already.
        Assert.Equal([(dayStart, 2.0), (dayStart.AddDays(1), 2.0)], model.WindSpeedMeasured.Select(p => (p.Time, p.Value)));
        Assert.Empty(model.WindSpeedForecast);
        Assert.Equal(600 * 1.15, model.RadiationAxisMaximum, 6);
        Assert.Equal(5, model.WindSpeedAxisMaximum);
        Assert.Contains("TESTSTATION", model.RadiationLegend);
    }

    [Fact]
    public void Weather_lines_end_on_the_edge_of_the_day_interpolated_from_the_neighbouring_hour()
    {
        var start = dayStart.ToUniversalTime();
        List<WeatherPoint> weather =
        [
            new() { Time = start.AddHours(-1), IsForecast = false, GlobalRadiationWattsPerSquareMeter = 100, WindSpeedMetersPerSecond = 1 },
            new() { Time = start, IsForecast = false, GlobalRadiationWattsPerSquareMeter = 200, WindSpeedMetersPerSecond = 2 },
            new() { Time = start.AddHours(23), IsForecast = true, GlobalRadiationWattsPerSquareMeter = 400, WindSpeedMetersPerSecond = 4 },
            new() { Time = start.AddHours(24), IsForecast = true, GlobalRadiationWattsPerSquareMeter = 600, WindSpeedMetersPerSecond = 6 },
        ];

        var (radiationMeasured, radiationForecast) = EnergyChartModel.WeatherLines(weather, w => w.GlobalRadiationWattsPerSquareMeter, TimeSpan.FromMinutes(30), dayStart, dayStart.AddDays(1));
        var (windMeasured, windForecast) = EnergyChartModel.WeatherLines(weather, w => w.WindSpeedMetersPerSecond, TimeSpan.Zero, dayStart, dayStart.AddDays(1));

        // Midnight lies half way between the means of 23:00 and of 00:00 the next day, and between yesterday's
        // 23:00 and today's 00:00.
        Assert.Equal([(dayStart, 150.0), (dayStart.AddMinutes(30), 200.0)], radiationMeasured.Select(p => (p.Time, p.Value)));
        Assert.Equal([(dayStart.AddMinutes(30), 200.0), (dayStart.AddHours(23.5), 400.0), (dayStart.AddDays(1), 500.0)], radiationForecast.Select(p => (p.Time, p.Value)));

        // The wind at midnight is a point of the day itself, on both edges, so nothing is interpolated.
        Assert.Equal([(dayStart, 2.0)], windMeasured.Select(p => (p.Time, p.Value)));
        Assert.Equal([(dayStart, 2.0), (dayStart.AddHours(23), 4.0), (dayStart.AddDays(1), 6.0)], windForecast.Select(p => (p.Time, p.Value)));

        Assert.Empty(EnergyChartModel.WeatherLines(weather, w => w.TemperatureCelsius, TimeSpan.Zero, dayStart, dayStart.AddDays(1)).Measured);
    }

    [Fact]
    public void Without_prices_the_axis_is_the_day_and_nothing_is_drawn()
    {
        var model = EnergyChartModel.Build(Data(prices: []), dayStart, dayStart.AddDays(1), EnergyPriceDisplay.Buy, gross: true, showProductions: true, showWeather: true);

        Assert.False(model.HasPrices);
        Assert.Equal(dayStart, model.AxisStart);
        Assert.Equal(dayStart.AddDays(1), model.AxisEnd);
        Assert.Equal(1, model.PriceAxisMaximum);
    }

    [Fact]
    public void The_chart_data_survives_the_trip_with_the_options_of_the_server_and_the_client()
    {
        var serverOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = true,
            IgnoreReadOnlyFields = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        var data = Data(prices: [10m, -1m]);
        data.PriceComponents.Add(new EnergyPriceComponent { Name = "Netz", Description = "Netznutzung", NetPrice = 4.72m, TaxRate = 0.19m });
        data.Productions.Add(new GridProductionPoint { StartTime = data.Prices[0].StartTime, EndTime = data.Prices[0].EndTime, SolarMegaWatt = 1, WindMegaWatt = 2 });
        data.Weather.Add(new WeatherPoint { Time = data.Prices[0].StartTime, IsForecast = true, WindSpeedMetersPerSecond = 3 });

        var json = JsonSerializer.Serialize(data, serverOptions);
        var back = JsonSerializer.Deserialize<EnergyChartData>(json, serverOptions)!;

        Assert.Contains("\"source\":\"awattar\"", json);
        Assert.DoesNotContain("manufacturer", json);
        Assert.Equal(data.From, back.From);
        Assert.Equal(2, back.Prices.Count);
        Assert.Equal(-1m, back.Prices[1].CentsPerKiloWattHour);
        Assert.Equal(DateTimeKind.Utc, back.Prices[1].StartTime.Kind);
        Assert.Equal(4.72m, Assert.Single(back.PriceComponents).NetPrice);
        Assert.Equal(0.19m, back.PriceComponents[0].TaxRate);
        Assert.Equal(EnergyPriceComponent.CentsPerKiloWattHourUnit, back.PriceComponents[0].Unit);
        Assert.Equal(2, Assert.Single(back.Productions).WindMegaWatt);
        var weather = Assert.Single(back.Weather);
        Assert.True(weather.IsForecast);
        Assert.Null(weather.GlobalRadiationWattsPerSquareMeter);
        Assert.Equal(3, weather.WindSpeedMetersPerSecond);
        Assert.Equal("TESTSTATION", back.WeatherStationName);
        Assert.Equal(0.19m, back.MarketVatRate);
    }

    private static EnergyChartData Data(IReadOnlyList<decimal> prices)
    {
        var start = dayStart.ToUniversalTime();

        return new EnergyChartData
        {
            From = start,
            To = start.AddDays(1),
            MarketVatRate = 0.19m,
            WeatherStationName = "TESTSTATION",
            Prices = [.. prices.Select((cents, i) => new EnergyPricePoint { StartTime = start.AddHours(i), EndTime = start.AddHours(i + 1), CentsPerKiloWattHour = cents, Source = EnergyPriceSource.Awattar })],
        };
    }
}
