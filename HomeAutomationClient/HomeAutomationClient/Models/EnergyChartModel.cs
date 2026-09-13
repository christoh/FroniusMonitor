using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>One bar of the chart: a slot of time and a value that stands on <see cref="Base"/>, so bars can stack.</summary>
/// <param name="Label">What is written at the end of the bar, or <see langword="null"/> for nothing.</param>
public sealed record ChartBar(DateTime Start, DateTime End, double Base, double Value, string? Label);

/// <summary>One point of a line of the chart.</summary>
public sealed record ChartPoint(DateTime Time, double Value);

/// <summary>
/// The price chart as numbers and captions, worked out by the view model and drawn by the view. Nothing in here
/// knows the charting library: every time is local, every value is in the unit of its axis, and the colors are the
/// view's business. That is what lets the same model be tested without a control and drawn by another library.
/// </summary>
public sealed class EnergyChartModel
{
    public required string Title { get; init; }

    /// <summary>The span of the time axis, local time.</summary>
    public required DateTime AxisStart { get; init; }

    public required DateTime AxisEnd { get; init; }

    /// <summary>The prices at or above zero, in ct/kWh as displayed.</summary>
    public IReadOnlyList<ChartBar> PositivePrices { get; init; } = [];

    /// <summary>The prices below zero, drawn in another color like the WPF chart does.</summary>
    public IReadOnlyList<ChartBar> NegativePrices { get; init; } = [];

    /// <summary>The span of the price axis. Leaves room at the top for the production bars that hang from there.</summary>
    public double PriceAxisMinimum { get; init; }

    public double PriceAxisMaximum { get; init; } = 1;

    /// <summary>
    /// Sun and wind production in GW, as negative values hanging from the top edge: the production axis runs from
    /// <see cref="ProductionAxisMinimum"/> up to zero, and zero is at the top. The wind bars stand on the solar bars.
    /// </summary>
    public IReadOnlyList<ChartBar> Solar { get; init; } = [];

    public IReadOnlyList<ChartBar> Wind { get; init; } = [];

    public double ProductionAxisMinimum { get; init; } = -1;

    public bool HasProductions => Solar.Count > 0;

    /// <summary>Global radiation in W/m², measured and forecast as two lines so the forecast can be dashed.</summary>
    public IReadOnlyList<ChartPoint> RadiationMeasured { get; init; } = [];

    public IReadOnlyList<ChartPoint> RadiationForecast { get; init; } = [];

    public double RadiationAxisMaximum { get; init; } = 100;

    /// <summary>Wind speed in m/s, measured and forecast.</summary>
    public IReadOnlyList<ChartPoint> WindSpeedMeasured { get; init; } = [];

    public IReadOnlyList<ChartPoint> WindSpeedForecast { get; init; } = [];

    public double WindSpeedAxisMaximum { get; init; } = 10;

    public bool HasWeather => RadiationMeasured.Count + RadiationForecast.Count + WindSpeedMeasured.Count + WindSpeedForecast.Count > 0;

    public bool HasPrices => PositivePrices.Count + NegativePrices.Count > 0;

    public string PriceLegend { get; init; } = string.Empty;

    public string NegativePriceLegend { get; init; } = string.Empty;

    public string SolarLegend { get; init; } = string.Empty;

    public string WindLegend { get; init; } = string.Empty;

    public string RadiationLegend { get; init; } = string.Empty;

    public string WindSpeedLegend { get; init; } = string.Empty;

    /// <summary>What a forecast line is called in the legend, appended to the caption of the quantity.</summary>
    public string ForecastLegend { get; init; } = string.Empty;

    /// <summary>
    /// Works the chart out of <paramref name="data"/> for one local day. The arithmetic is the WPF chart's: the
    /// price axis starts at zero or below the lowest price, and is stretched at the top by 60 % where the
    /// production bars need the room, by 10 % otherwise; the production axis is three times the largest total,
    /// so the bars take the top third.
    /// </summary>
    /// <param name="dayStart">Start of the day shown, local time.</param>
    /// <param name="dayEnd">End of the day shown, local time.</param>
    public static EnergyChartModel Build(EnergyChartData data, DateTime dayStart, DateTime dayEnd, EnergyPriceDisplay display, bool gross, bool showProductions, bool showWeather)
    {
        const double margin = 1.1;

        var prices = data.Prices
            .Where(p => InDay(p.StartTime))
            .Select(p => (Start: Local(p.StartTime), End: Local(p.EndTime), Value: (double)EnergyPriceCalculator.Display(p.CentsPerKiloWattHour, data.PriceComponents, data.MarketVatRate, display, gross)))
            .OrderBy(p => p.Start)
            .ToList();

        var productions = showProductions
            ? data.Productions.Where(p => InDay(p.StartTime)).OrderBy(p => p.StartTime).ToList()
            : [];

        // The whole span, not only the day: the hours around the day's edges give the lines their end points.
        var weather = showWeather
            ? data.Weather.OrderBy(w => w.Time).ToList()
            : [];

        var priceMin = prices.Count == 0 ? 0 : Math.Min(prices.Min(p => p.Value), 0);
        var priceMax = prices.Count == 0 ? 0 : prices.Max(p => p.Value);
        var axisMinimum = priceMin > 0 ? priceMin / margin : priceMin * margin;
        var axisMaximum = priceMax + (priceMax - axisMinimum) * (productions.Count == 0 ? 0.1 : 0.6) * margin;

        var totals = productions.Select(p => (p.SolarMegaWatt + p.WindMegaWatt) / 1000).ToList();
        var productionMax = totals.Count == 0 ? 1 : Math.Max(totals.Max(), 0.001);

        // Radiation is the mean of an hour and drawn at its middle; wind is the state at the start of an hour.
        var (radiationMeasured, radiationForecast) = WeatherLines(weather, w => w.GlobalRadiationWattsPerSquareMeter, TimeSpan.FromMinutes(30), dayStart, dayEnd);
        var (windMeasured, windForecast) = WeatherLines(weather, w => w.WindSpeedMetersPerSecond, TimeSpan.Zero, dayStart, dayEnd);
        var radiationMax = radiationMeasured.Concat(radiationForecast).Select(p => p.Value).DefaultIfEmpty(0).Max();
        var windMax = windMeasured.Concat(windForecast).Select(p => p.Value).DefaultIfEmpty(0).Max();

        var title = $"{Loc.ElectricityPrice} ({data.PriceRegion.ToDisplayName()})";

        if (data.Prices.Any(p => InDay(p.StartTime) && p.Source == EnergyPriceSource.WattPilot))
        {
            title += $" - {Loc.PricesFromWattPilot}";
        }

        return new EnergyChartModel
        {
            Title = title,
            AxisStart = prices.Count == 0 ? dayStart : prices.Min(p => p.Start),
            AxisEnd = prices.Count == 0 ? dayEnd : prices.Max(p => p.End),
            PositivePrices = [.. prices.Where(p => p.Value >= 0).Select(p => new ChartBar(p.Start, p.End, 0, p.Value, p.Value.ToString("N2", CultureInfo.CurrentCulture)))],
            NegativePrices = [.. prices.Where(p => p.Value < 0).Select(p => new ChartBar(p.Start, p.End, 0, p.Value, p.Value.ToString("N2", CultureInfo.CurrentCulture)))],
            PriceAxisMinimum = axisMinimum,
            PriceAxisMaximum = Math.Max(axisMaximum, axisMinimum + 1),
            Solar = [.. productions.Select(p => new ChartBar(Local(p.StartTime), Local(p.EndTime), 0, -p.SolarMegaWatt / 1000, null))],
            Wind = [.. productions.Select(p => new ChartBar(Local(p.StartTime), Local(p.EndTime), -p.SolarMegaWatt / 1000, -(p.SolarMegaWatt + p.WindMegaWatt) / 1000, ((p.SolarMegaWatt + p.WindMegaWatt) / 1000).ToString("0.0", CultureInfo.CurrentCulture)))],
            ProductionAxisMinimum = -productionMax * 3,
            RadiationMeasured = radiationMeasured,
            RadiationForecast = radiationForecast,
            RadiationAxisMaximum = Math.Max(100, radiationMax * 1.15),
            WindSpeedMeasured = windMeasured,
            WindSpeedForecast = windForecast,
            WindSpeedAxisMaximum = Math.Max(5, windMax * 1.15),
            PriceLegend = display == EnergyPriceDisplay.Buy ? Loc.BuyingPrice : Loc.MarketPrice,
            NegativePriceLegend = Loc.ElectricityPriceNegative,
            SolarLegend = Loc.SolarProduction,
            WindLegend = Loc.WindProduction,
            RadiationLegend = $"{Loc.GlobalRadiation} ({data.WeatherStationName})",
            WindSpeedLegend = $"{Loc.WindSpeed} ({data.WeatherStationName})",
            ForecastLegend = Loc.Forecast,
        };

        bool InDay(DateTime utc)
        {
            var local = Local(utc);
            return local >= dayStart && local < dayEnd;
        }
    }

    /// <summary>
    /// One weather quantity as the two lines the chart draws, measured and forecast, reaching from one edge of the
    /// day to the other without a break between them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two gaps were visible before this: the solid line ended at the last measured hour and the dashed one began
    /// an hour later, and both stopped an hour short of midnight because the last hour of the day has no point at
    /// its end. So the forecast line starts at the last measured point, and where the first or last point does not
    /// sit on the edge of the day, a point on the edge is added: interpolated between the neighbouring hours where
    /// the hour beyond the edge is known - the live data has tomorrow's first hour - and the edge value repeated
    /// where it is not.
    /// </para>
    /// <para>
    /// <paramref name="offset"/> is where in its hour a value is drawn: the middle for a mean over the hour, the
    /// start for a state at a moment.
    /// </para>
    /// </remarks>
    public static (IReadOnlyList<ChartPoint> Measured, IReadOnlyList<ChartPoint> Forecast) WeatherLines(IReadOnlyList<WeatherPoint> weather, Func<WeatherPoint, double?> value, TimeSpan offset, DateTime dayStart, DateTime dayEnd)
    {
        var points = weather
            .Where(w => value(w) is not null)
            .Select(w => (Time: Local(w.Time) + offset, Value: value(w)!.Value, w.IsForecast))
            .OrderBy(p => p.Time)
            .ToList();

        var inside = points.Where(p => p.Time >= dayStart && p.Time <= dayEnd).ToList();

        if (inside.Count == 0)
        {
            return ([], []);
        }

        if (inside[0].Time > dayStart)
        {
            inside.Insert(0, Edge(dayStart, inside[0], points.LastOrDefault(p => p.Time < dayStart)));
        }

        if (inside[^1].Time < dayEnd)
        {
            inside.Add(Edge(dayEnd, inside[^1], points.FirstOrDefault(p => p.Time > dayEnd)));
        }

        var measured = inside.Where(p => !p.IsForecast).Select(p => new ChartPoint(p.Time, p.Value)).ToList();
        var forecast = inside.Where(p => p.IsForecast).Select(p => new ChartPoint(p.Time, p.Value)).ToList();

        if (measured.Count > 0 && forecast.Count > 0 && forecast[0].Time > measured[^1].Time)
        {
            forecast.Insert(0, measured[^1]);
        }

        return (measured, forecast);

        // The point on the edge of the day: between the nearest point inside and the nearest outside where the
        // outside one is the neighbouring hour, otherwise the inside value carried to the edge.
        static (DateTime Time, double Value, bool IsForecast) Edge(DateTime edge, (DateTime Time, double Value, bool IsForecast) inside, (DateTime Time, double Value, bool IsForecast) outside)
        {
            if (outside.Time == default || (outside.Time - inside.Time).Duration() > TimeSpan.FromMinutes(90))
            {
                return (edge, inside.Value, inside.IsForecast);
            }

            var fraction = (edge - inside.Time).TotalMinutes / (outside.Time - inside.Time).TotalMinutes;
            return (edge, inside.Value + (outside.Value - inside.Value) * fraction, inside.IsForecast);
        }
    }

    private static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
