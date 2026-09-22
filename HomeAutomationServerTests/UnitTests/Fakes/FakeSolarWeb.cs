using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>Solar.web as a test wants it: answers a chart for whatever is asked, counts what it was asked, and can refuse in every way the real one does.</summary>
internal sealed class FakeSolarWebClient(TimeProvider clock) : ISolarWebClient
{
    /// <summary>How many charts were asked for.</summary>
    public int Requests { get; private set; }

    public int FirmwareRequests { get; private set; }

    public List<(SolarWebInterval Interval, SolarWebView View, DateOnly Date)> Asked { get; } = [];

    /// <summary>What the firmware answer lists.</summary>
    public List<SolarWebFirmwareComponent> Firmware { get; } = [];

    /// <summary>What the next requests throw, or <see langword="null" /> to answer.</summary>
    public Exception? Refusal { get; set; }

    /// <summary>What one particular chart throws instead of an answer, or <see langword="null" />; every other chart is answered.</summary>
    public Func<SolarWebInterval, SolarWebView, DateOnly, Exception?>? FailWhen { get; set; }

    /// <summary>True where the account has no Premium: the two Premium views are answered with Solar.web's placeholder.</summary>
    public bool IsPremiumLocked { get; set; }

    /// <summary>
    /// How far behind the inverter this Solar.web is: a day chart gets its last slot, 23:55 local, only once the
    /// day has been over for this long. Zero by default, so a day is complete the moment it ends.
    /// </summary>
    public TimeSpan Lag { get; set; }

    public Task<SolarWebFirmwareStatus> GetFirmwareStatusAsync(SolarWebSettings settings, CancellationToken token = default)
    {
        FirmwareRequests++;

        if (Refusal != null)
        {
            throw Refusal;
        }

        return Task.FromResult(new SolarWebFirmwareStatus { PvSystemId = settings.PvSystemId, Timestamp = clock.GetUtcNow().UtcDateTime, Components = [.. Firmware] });
    }

    public Task<SolarWebChart> GetChartAsync(SolarWebSettings settings, SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default)
    {
        Requests++;
        Asked.Add((interval, view, date));

        if (Refusal != null)
        {
            throw Refusal;
        }

        if (FailWhen?.Invoke(interval, view, date) is { } failure)
        {
            throw failure;
        }

        var period = SolarWebPeriod.Normalize(interval, date);
        var now = clock.GetUtcNow().UtcDateTime;

        // A day chart's last point: the day's last slot once the day is over and the lag has passed; otherwise as
        // far as this Solar.web has got, which is the lag behind now or behind the day's end - the way Solar.web's
        // chart of today ends at the current five minutes, and the chart of a day that has just ended stops hours
        // short of midnight while Solar.web catches up.
        var lastPoint = now;

        if (interval == SolarWebInterval.Day && SolarWebPeriod.EndUtc(interval, period, settings.ResolveTimeZone()) is { } end)
        {
            var lastSlot = end.AddMinutes(-5);
            lastPoint = now >= end + Lag ? lastSlot : (now < end ? now : lastSlot) - Lag;
        }

        return Task.FromResult(new SolarWebChart
        {
            PvSystemId = settings.PvSystemId,
            Interval = interval,
            View = view,
            Period = period,
            FetchedUtc = now,
            IsPremiumFeature = IsPremiumLocked && view.IsPremium(),
            Title = period.ToString("yyyy-MM-dd"),
            SumValue = $"{Requests} kWh",
            Series = [new SolarWebSeries { Id = "FromGenToGrid", Name = "Energie ins Netz eingespeist", Unit = "kWh", ChartType = "column", Points = [new SolarWebPoint { TimeUtc = lastPoint, Value = Requests }] }],
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
