using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;
using De.Hochstaetter.HomeAutomationServer.Services;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The SQLite cache of Solar.web's charts against a file of its own: what goes in comes out, a chart written again
/// replaces the old one as a whole, the views and the periods keep apart, and only the day charts before the cutoff
/// are removed.
/// </summary>
public sealed class SolarWebHistoryStoreTests : IAsyncLifetime
{
    private const string PvSystemId = "318e321a-883b-4a82-868a-0fdc784476ad";

    private readonly string directory = Path.Combine(Path.GetTempPath(), "HomeAutomationServerTests", Guid.NewGuid().ToString("N"));
    private SolarWebHistoryStore store = null!;

    public async ValueTask InitializeAsync()
    {
        store = new SolarWebHistoryStore(NullLogger<SolarWebHistoryStore>.Instance, Path.Combine(directory, SolarWebHistoryStore.FileName));
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
        Assert.Null(await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 9, 19), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_chart_comes_back_as_it_went_in_with_its_series_in_order_and_its_points_in_time()
    {
        var token = TestContext.Current.CancellationToken;
        var day = new DateOnly(2026, 9, 19);
        var midnight = new DateTime(2026, 9, 18, 22, 0, 0, DateTimeKind.Utc);

        var chart = new SolarWebChart
        {
            PvSystemId = PvSystemId,
            Interval = SolarWebInterval.Day,
            View = SolarWebView.Consumption,
            Period = day,
            FetchedUtc = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
            IsPremiumFeature = false,
            Title = "19.09.2026",
            SumValue = "6,53 kWh",
            Series =
            [
                new SolarWebSeries
                {
                    Id = "StateOfCharge", Name = "Ladezustand", Unit = "%", ChartType = "spline",
                    Points = [new SolarWebPoint { TimeUtc = midnight.AddMinutes(5), Value = 78.9 }, new SolarWebPoint { TimeUtc = midnight, Value = 79 }],
                },
                new SolarWebSeries
                {
                    Id = "FromGen", Name = "Produktion", Unit = "W", ChartType = "spline",
                    Points = [new SolarWebPoint { TimeUtc = midnight, Value = 0 }, new SolarWebPoint { TimeUtc = midnight.AddMinutes(5), Value = null }],
                },
                new SolarWebSeries
                {
                    Id = "BattOperatingState", Name = "Batteriestatus", Unit = "%", ChartType = "bubble",
                    Points = [new SolarWebPoint { TimeUtc = new DateTime(2026, 9, 19, 21, 28, 47, DateTimeKind.Utc), Value = 73.1, Text = "Normalbetrieb" }],
                },
                new SolarWebSeries { Id = "Empty", Name = "Nichts", Unit = "W", ChartType = "areaspline" },
            ],
        };

        await store.UpsertChartAsync(chart, token);
        var back = await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Consumption, day, token);

        Assert.NotNull(back);
        Assert.Equal(PvSystemId, back.PvSystemId);
        Assert.Equal(SolarWebInterval.Day, back.Interval);
        Assert.Equal(SolarWebView.Consumption, back.View);
        Assert.Equal(day, back.Period);
        Assert.Equal(chart.FetchedUtc, back.FetchedUtc);
        Assert.Equal(DateTimeKind.Utc, back.FetchedUtc.Kind);
        Assert.Equal("19.09.2026", back.Title);
        Assert.Equal("6,53 kWh", back.SumValue);
        Assert.Equal(["StateOfCharge", "FromGen", "BattOperatingState", "Empty"], back.Series.Select(s => s.Id));
        Assert.Equal(["Ladezustand", "Produktion", "Batteriestatus", "Nichts"], back.Series.Select(s => s.Name));
        Assert.Equal(["%", "W", "%", "W"], back.Series.Select(s => s.Unit));
        Assert.Equal("bubble", back.Series[2].ChartType);

        // Points come back in order of time, whatever order they were written in.
        Assert.Equal([midnight, midnight.AddMinutes(5)], back.Series[0].Points.Select(p => p.TimeUtc));
        Assert.Equal([79.0, 78.9], back.Series[0].Points.Select(p => p.Value));
        Assert.Equal(DateTimeKind.Utc, back.Series[0].Points[0].TimeUtc.Kind);
        Assert.Null(back.Series[1].Points[1].Value);
        Assert.Null(back.Series[1].Points[0].Text);

        var state = Assert.Single(back.Series[2].Points);
        Assert.Equal(new DateTime(2026, 9, 19, 21, 28, 47, DateTimeKind.Utc), state.TimeUtc);
        Assert.Equal(73.1, state.Value);
        Assert.Equal("Normalbetrieb", state.Text);
        Assert.Empty(back.Series[3].Points);

        // Another view and another day of the same system are not this chart.
        Assert.Null(await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Production, day, token));
        Assert.Null(await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Consumption, day.AddDays(1), token));
        Assert.Null(await store.GetChartAsync("other", SolarWebInterval.Day, SolarWebView.Consumption, day, token));
    }

    [Fact]
    public async Task A_chart_written_again_replaces_the_old_one_as_a_whole()
    {
        var token = TestContext.Current.CancellationToken;
        var month = new DateOnly(2026, 9, 1);
        var first = Chart(SolarWebInterval.Month, SolarWebView.Production, month, new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc), "FromGenToGrid", "PvForecastTruncated");
        var second = Chart(SolarWebInterval.Month, SolarWebView.Production, month, new DateTime(2026, 9, 20, 11, 0, 0, DateTimeKind.Utc), "FromGenToGrid");
        second.Series[0].Points[0].Value = 99;
        second.SumValue = "2 kWh";

        await store.UpsertChartAsync(first, token);
        await store.UpsertChartAsync(second, token);
        var back = await store.GetChartAsync(PvSystemId, SolarWebInterval.Month, SolarWebView.Production, month, token);

        Assert.NotNull(back);
        Assert.Equal(second.FetchedUtc, back.FetchedUtc);
        Assert.Equal("2 kWh", back.SumValue);
        var series = Assert.Single(back.Series);
        Assert.Equal("FromGenToGrid", series.Id);
        Assert.Equal(99, Assert.Single(series.Points).Value);
    }

    [Fact]
    public async Task Only_the_day_charts_before_the_cutoff_are_removed_and_their_number_is_told()
    {
        var token = TestContext.Current.CancellationToken;
        var fetched = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        var cutoff = new DateOnly(2026, 8, 21);

        await store.UpsertChartAsync(Chart(SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 8, 19), fetched, "FromGen"), token);
        await store.UpsertChartAsync(Chart(SolarWebInterval.Day, SolarWebView.Consumption, new DateOnly(2026, 8, 20), fetched, "FromGen"), token);
        await store.UpsertChartAsync(Chart(SolarWebInterval.Day, SolarWebView.Production, cutoff, fetched, "FromGen"), token);
        await store.UpsertChartAsync(Chart(SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 9, 19), fetched, "FromGen"), token);
        await store.UpsertChartAsync(Chart(SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 8, 1), fetched, "FromGenToGrid"), token);
        await store.UpsertChartAsync(Chart(SolarWebInterval.Year, SolarWebView.Expense, new DateOnly(2025, 1, 1), fetched, "Saving"), token);
        await store.UpsertChartAsync(Chart(SolarWebInterval.All, SolarWebView.Production, DateOnly.MinValue, fetched, "FromGenToGrid"), token);
        await store.UpsertChartAsync(Chart(SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 8, 19), fetched, "FromGen").ForSystem("other"), token);

        Assert.Equal(2, await store.DeleteDayChartsBeforeAsync(PvSystemId, cutoff, token));
        Assert.Equal(0, await store.DeleteDayChartsBeforeAsync(PvSystemId, cutoff, token));

        Assert.Null(await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 8, 19), token));
        Assert.Null(await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Consumption, new DateOnly(2026, 8, 20), token));
        Assert.NotNull(await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Production, cutoff, token));
        Assert.NotNull(await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 9, 19), token));
        Assert.NotNull(await store.GetChartAsync(PvSystemId, SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 8, 1), token));
        Assert.NotNull(await store.GetChartAsync(PvSystemId, SolarWebInterval.Year, SolarWebView.Expense, new DateOnly(2025, 1, 1), token));
        Assert.NotNull(await store.GetChartAsync(PvSystemId, SolarWebInterval.All, SolarWebView.Production, DateOnly.MinValue, token));
        Assert.NotNull(await store.GetChartAsync("other", SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 8, 19), token));

        // The points of the removed charts went with them, so a chart written again for that day starts clean.
        var again = Chart(SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 8, 19), fetched, "ToConsumer");
        await store.UpsertChartAsync(again, token);
        Assert.Equal("ToConsumer", Assert.Single((await store.GetChartAsync(PvSystemId, SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 8, 19), token))!.Series).Id);
    }

    /// <summary>A chart with one point per series, at the period's first instant.</summary>
    internal static SolarWebChart Chart(SolarWebInterval interval, SolarWebView view, DateOnly period, DateTime fetchedUtc, params string[] seriesIds)
    {
        var time = period == DateOnly.MinValue ? DateTime.UnixEpoch : period.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return new SolarWebChart
        {
            PvSystemId = PvSystemId,
            Interval = interval,
            View = view,
            Period = period,
            FetchedUtc = fetchedUtc,
            Title = period.ToString("yyyy-MM-dd"),
            SumValue = "1 kWh",
            Series = [.. seriesIds.Select(id => new SolarWebSeries { Id = id, Name = id, Unit = "kWh", ChartType = "column", Points = [new SolarWebPoint { TimeUtc = time, Value = 1 }] })],
        };
    }
}

file static class SolarWebChartExtensions
{
    /// <summary>The same chart for another system.</summary>
    public static SolarWebChart ForSystem(this SolarWebChart chart, string pvSystemId) => new()
    {
        PvSystemId = pvSystemId,
        Interval = chart.Interval,
        View = chart.View,
        Period = chart.Period,
        FetchedUtc = chart.FetchedUtc,
        IsPremiumFeature = chart.IsPremiumFeature,
        Title = chart.Title,
        SumValue = chart.SumValue,
        Series = chart.Series,
    };
}
