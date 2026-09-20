using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>Solar.web as a test wants it: answers a chart for whatever is asked, counts what it was asked, and can refuse in every way the real one does.</summary>
internal sealed class FakeSolarWebClient(TimeProvider clock) : ISolarWebClient
{
    public int Requests { get; private set; }

    public List<(SolarWebInterval Interval, SolarWebView View, DateOnly Date)> Asked { get; } = [];

    /// <summary>What the next requests throw, or <see langword="null" /> to answer.</summary>
    public Exception? Refusal { get; set; }

    public Task<SolarWebChart> GetChartAsync(SolarWebSettings settings, SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default)
    {
        Requests++;
        Asked.Add((interval, view, date));

        if (Refusal != null)
        {
            throw Refusal;
        }

        var period = SolarWebPeriod.Normalize(interval, date);

        return Task.FromResult(new SolarWebChart
        {
            PvSystemId = settings.PvSystemId,
            Interval = interval,
            View = view,
            Period = period,
            FetchedUtc = clock.GetUtcNow().UtcDateTime,
            Title = period.ToString("yyyy-MM-dd"),
            SumValue = $"{Requests} kWh",
            Series = [new SolarWebSeries { Id = "FromGenToGrid", Name = "Energie ins Netz eingespeist", Unit = "kWh", ChartType = "column", Points = [new SolarWebPoint { TimeUtc = clock.GetUtcNow().UtcDateTime, Value = Requests }] }],
        });
    }
}

/// <summary>The chart cache in memory, with the same replace-as-a-whole semantics the SQLite one has.</summary>
internal sealed class InMemorySolarWebHistoryStore : ISolarWebHistoryStore
{
    private readonly Dictionary<(string System, SolarWebInterval Interval, SolarWebView View, DateOnly Period), SolarWebChart> charts = [];

    public int Initializations { get; private set; }

    public IReadOnlyCollection<SolarWebChart> Charts => charts.Values;

    public Task InitializeAsync(CancellationToken token = default)
    {
        Initializations++;
        return Task.CompletedTask;
    }

    public Task<SolarWebChart?> GetChartAsync(string pvSystemId, SolarWebInterval interval, SolarWebView view, DateOnly period, CancellationToken token = default)
    {
        return Task.FromResult(charts.GetValueOrDefault((pvSystemId, interval, view, period)));
    }

    public Task UpsertChartAsync(SolarWebChart chart, CancellationToken token = default)
    {
        charts[(chart.PvSystemId, chart.Interval, chart.View, chart.Period)] = chart;
        return Task.CompletedTask;
    }

    public Task<int> DeleteDayChartsBeforeAsync(string pvSystemId, DateOnly cutoff, CancellationToken token = default)
    {
        var gone = charts.Keys.Where(k => k.System == pvSystemId && k.Interval == SolarWebInterval.Day && k.Period < cutoff).ToList();
        gone.ForEach(k => charts.Remove(k));
        return Task.FromResult(gone.Count);
    }
}
