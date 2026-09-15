using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.EnergyData;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>Awattar as a test wants it: answers what it is given, counts what it was asked, and can refuse.</summary>
internal sealed class FakeAwattarClient : IAwattarClient
{
    public List<EnergyPricePoint> Prices { get; } = [];

    public List<GridProductionPoint> Productions { get; } = [];

    public List<EnergyPriceComponent> Components { get; } = [];

    public int PriceRequests { get; private set; }

    public int ProductionRequests { get; private set; }

    public int ComponentRequests { get; private set; }

    public List<(DateTime From, DateTime To)> PriceSpans { get; } = [];

    public bool Fails { get; set; }

    public Task<IReadOnlyList<EnergyPricePoint>> GetMarketPricesAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        PriceRequests++;
        PriceSpans.Add((fromUtc, toUtc));
        Refuse();
        return Task.FromResult<IReadOnlyList<EnergyPricePoint>>([.. Prices.Where(p => p.StartTime >= fromUtc && p.StartTime < toUtc)]);
    }

    public Task<IReadOnlyList<GridProductionPoint>> GetProductionsAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        ProductionRequests++;
        Refuse();
        return Task.FromResult<IReadOnlyList<GridProductionPoint>>([.. Productions.Where(p => p.StartTime >= fromUtc && p.StartTime < toUtc)]);
    }

    public Task<IReadOnlyList<EnergyPriceComponent>> GetPriceComponentsAsync(EnergyDataSettings settings, DateOnly day, CancellationToken token = default)
    {
        ComponentRequests++;
        Refuse();
        return Task.FromResult<IReadOnlyList<EnergyPriceComponent>>([.. Components]);
    }

    private void Refuse()
    {
        if (Fails)
        {
            throw new HttpRequestException("Too many requests", null, System.Net.HttpStatusCode.TooManyRequests);
        }
    }
}

internal sealed class FakeDwdWeatherClient : IDwdWeatherClient
{
    public List<WeatherPoint> Forecast { get; } = [];

    public List<WeatherPoint> Observations { get; } = [];

    public int ForecastRequests { get; private set; }

    public Task<DwdForecast> GetForecastAsync(string stationId, CancellationToken token = default)
    {
        ForecastRequests++;
        return Task.FromResult(new DwdForecast { StationId = stationId, StationName = "TESTSTATION", IssueTime = DateTime.UtcNow, Points = [.. Forecast] });
    }

    public Task<IReadOnlyList<WeatherPoint>> GetObservationsAsync(string stationId, CancellationToken token = default)
    {
        return Task.FromResult<IReadOnlyList<WeatherPoint>>([.. Observations]);
    }
}

/// <summary>The history store in memory, with the same upsert semantics the SQLite one has.</summary>
internal sealed class InMemoryEnergyHistoryStore : IEnergyHistoryStore
{
    private readonly Dictionary<(EnergyPriceSource Source, DateTime Start), EnergyPricePoint> prices = [];
    private readonly Dictionary<DateOnly, List<EnergyPriceComponent>> components = [];
    private readonly Dictionary<(AwattarCountry Region, DateTime Start), GridProductionPoint> productions = [];
    private readonly Dictionary<(string Station, DateTime Time, bool IsForecast), WeatherPoint> weather = [];

    public int Initializations { get; private set; }

    public Task InitializeAsync(CancellationToken token = default)
    {
        Initializations++;
        return Task.CompletedTask;
    }

    public Task UpsertPricesAsync(IEnumerable<EnergyPricePoint> list, CancellationToken token = default)
    {
        foreach (var price in list)
        {
            prices[(price.Source, price.StartTime)] = price;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EnergyPricePoint>> GetPricesAsync(EnergyPriceSource source, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        return Task.FromResult<IReadOnlyList<EnergyPricePoint>>([.. prices.Values.Where(p => p.Source == source && p.StartTime >= fromUtc && p.StartTime < toUtc).OrderBy(p => p.StartTime)]);
    }

    public Task UpsertPriceComponentsAsync(DateOnly day, IEnumerable<EnergyPriceComponent> list, CancellationToken token = default)
    {
        components[day] = [.. list];
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EnergyPriceComponent>> GetPriceComponentsAsync(DateOnly day, CancellationToken token = default)
    {
        return Task.FromResult<IReadOnlyList<EnergyPriceComponent>>(components.TryGetValue(day, out var list) ? [.. list] : []);
    }

    public Task UpsertProductionsAsync(AwattarCountry region, IEnumerable<GridProductionPoint> list, CancellationToken token = default)
    {
        foreach (var production in list)
        {
            productions[(region, production.StartTime)] = production;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GridProductionPoint>> GetProductionsAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        return Task.FromResult<IReadOnlyList<GridProductionPoint>>([.. productions.Where(p => p.Key.Region == region && p.Key.Start >= fromUtc && p.Key.Start < toUtc).Select(p => p.Value).OrderBy(p => p.StartTime)]);
    }

    public Task UpsertWeatherAsync(string stationId, IEnumerable<WeatherPoint> points, CancellationToken token = default)
    {
        foreach (var point in points)
        {
            weather[(stationId, point.Time, point.IsForecast)] = point;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WeatherPoint>> GetWeatherAsync(string stationId, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        return Task.FromResult<IReadOnlyList<WeatherPoint>>([.. weather.Where(w => w.Key.Station == stationId && w.Key.Time >= fromUtc && w.Key.Time < toUtc).Select(w => w.Value).OrderBy(w => w.Time)]);
    }
}
