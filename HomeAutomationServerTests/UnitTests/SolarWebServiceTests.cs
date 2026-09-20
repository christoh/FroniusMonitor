using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// When <see cref="SolarWebService"/> asks Solar.web and when it serves the cache: what is cached is served as it
/// is, the tick reads the running periods and a period that has just ended once more, a 429 stops every request for
/// the time it names and the stale chart is served meanwhile, a rejected login is never tried again, and old day
/// charts are removed. The clock is the test's, Solar.web and the store are fakes.
/// </summary>
public sealed class SolarWebServiceTests : IDisposable
{
    private const string PvSystemId = "318e321a-883b-4a82-868a-0fdc784476ad";

    /// <summary>Sunday 2026-09-20, 12:00 in Berlin (10:00 UTC).</summary>
    private static readonly DateTimeOffset noon = new(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly today = new(2026, 9, 20);

    /// <summary>
    /// A forecast day: the one chart a client's request still reads again once it is a refresh rate old, which is
    /// how the failure tests provoke a request while today's chart would simply come from the cache.
    /// </summary>
    private static readonly DateOnly tomorrow = today.AddDays(1);

    private readonly SettableTimeProvider clock = new(noon);
    private readonly InMemorySolarWebHistoryStore store = new();
    private readonly FakeSolarWebClient client;
    private readonly SolarWebParameters parameters;
    private readonly IDataControlService controlService = new DataControlService(NullLogger<DataControlService>.Instance);
    private readonly List<DeviceUpdateEventArgs> updates = [];
    private readonly SolarWebService service;

    public SolarWebServiceTests()
    {
        client = new FakeSolarWebClient(clock);
        controlService.DeviceUpdate += (_, e) => updates.Add(e);

        parameters = new SolarWebParameters
        {
            Settings = new SolarWebSettings { UserName = "you@example.com", Password = "secret", PvSystemId = PvSystemId, TimeZoneId = "Europe/Berlin" },
            RefreshRate = TimeSpan.FromMinutes(15),
            MinimumRequestInterval = TimeSpan.Zero,
        };

        service = Create(parameters, store);
    }

    private SolarWebService Create(SolarWebParameters p, InMemorySolarWebHistoryStore s) => new(NullLogger<SolarWebService>.Instance, Options(p), client, s, controlService, clock);

    public void Dispose() => service.Dispose();

    [Fact]
    public async Task Starting_initializes_the_store_and_a_missing_or_empty_section_disables_the_service()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);
        Assert.True(service.IsEnabled);
        Assert.Equal(1, store.Initializations);
        Assert.Equal(today, service.Today);

        using var disabled = Create(new SolarWebParameters(), new InMemorySolarWebHistoryStore());
        await disabled.StartAsync(TestContext.Current.CancellationToken);
        Assert.False(disabled.IsEnabled);
        await Assert.ThrowsAsync<InvalidOperationException>(() => disabled.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => disabled.GetFirmwareStatusAsync(TestContext.Current.CancellationToken));
        await disabled.TickAsync(TestContext.Current.CancellationToken);

        using var noSystem = Create(new SolarWebParameters { Settings = new SolarWebSettings { UserName = "x" } }, new InMemorySolarWebHistoryStore());
        Assert.False(noSystem.IsEnabled);
        Assert.Equal(0, client.Requests);
        Assert.Equal(0, client.FirmwareRequests);
    }

    [Fact]
    public async Task The_firmware_status_is_read_on_request_kept_for_the_refresh_rate_and_pushed_only_when_it_changes()
    {
        client.Firmware.Add(Component("Battery", null, null, false));
        client.Firmware.Add(Component("Gen24Main", "1.41.11-1", "1.41.11-1", false));
        await service.StartAsync(TestContext.Current.CancellationToken);
        Assert.Null(service.FirmwareStatus);
        Assert.Empty(updates);

        // The first read is published: a client that connects later gets it replayed.
        var first = await service.GetFirmwareStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, client.FirmwareRequests);
        Assert.Same(first, service.FirmwareStatus);
        Assert.False(first.HasOutdatedFirmware);
        Assert.Same(first, Assert.IsType<SolarWebFirmwareStatus>(controlService.Entities[SolarWebFirmwareStatus.DeviceId].Device));
        var update = Assert.Single(updates);
        Assert.Equal(SolarWebFirmwareStatus.DeviceId, update.Id);

        // Within the refresh rate the same status is served; after it Solar.web is asked again, and nothing changed, so nothing is pushed.
        clock.Now = noon.AddMinutes(14);
        Assert.Same(first, await service.GetFirmwareStatusAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, client.FirmwareRequests);
        clock.Now = noon.AddMinutes(15);
        var second = await service.GetFirmwareStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, client.FirmwareRequests);
        Assert.NotSame(first, second);
        Assert.Equal(clock.Now.UtcDateTime, second.Timestamp);
        Assert.Single(updates);

        // A newer firmware turns up: the tick reads it regardless of age and the clients are told.
        client.Firmware[1] = Component("Gen24Main", "1.41.11-1", "1.42.3-2", true);
        clock.Now = noon.AddMinutes(20);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, client.FirmwareRequests);
        Assert.Equal(2, updates.Count);
        var pushed = Assert.IsType<SolarWebFirmwareStatus>(updates[1].Device.Device);
        Assert.True(pushed.HasOutdatedFirmware);
        Assert.Equal(new Version(1, 42, 3, 2), pushed.Components[1].UpdateVersion);
        Assert.Same(pushed, service.FirmwareStatus);

        // The update was installed: told once more, so the client can take its notice down.
        client.Firmware[1] = Component("Gen24Main", "1.42.3-2", "1.42.3-2", false);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, updates.Count);
        Assert.False(Assert.IsType<SolarWebFirmwareStatus>(updates[2].Device.Device).HasOutdatedFirmware);

        // Stopping takes the status off the hub.
        await service.StopAsync(TestContext.Current.CancellationToken);
        Assert.False(controlService.Entities.ContainsKey(SolarWebFirmwareStatus.DeviceId));
        Assert.Null(service.FirmwareStatus);
    }

    [Fact]
    public async Task A_firmware_read_that_fails_keeps_the_last_status_and_the_tick_swallows_the_failure()
    {
        client.Firmware.Add(Component("Gen24Main", "1.41.11-1", "1.41.11-1", false));
        await service.StartAsync(TestContext.Current.CancellationToken);
        var first = await service.GetFirmwareStatusAsync(TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new SolarWebUnavailableException(null, "Maintenance Work");
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Same(first, service.FirmwareStatus);
        Assert.Same(first, await service.GetFirmwareStatusAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, client.FirmwareRequests);
        Assert.NotNull(service.UnavailableUntil);

        // The charts share the back-off: a chart not in the cache is refused without a request.
        await Assert.ThrowsAsync<SolarWebUnavailableException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal(0, client.Requests);
        Assert.Single(updates);
    }

    private static SolarWebFirmwareComponent Component(string family, string? installed, string? available, bool newer) => new()
    {
        DataSourceId = "pilot-0.6e-796954429721009271_1663595310",
        ComponentId = family == "Battery" ? 16580609 : 262144,
        UpdateFamily = family,
        IsOnline = true,
        InstalledVersion = SolarWebVersion.Parse(installed),
        UpdateVersion = SolarWebVersion.Parse(available),
        ChangelogUrl = available == null ? null : $"https://firmware-download.fronius.com/releaseGroup/Gen24-imxs6/common/{available}/changelog.pdf",
        NewerVersionAvailable = newer,
        IsUpdateAllowed = true,
        UpdateRecommendationInfo = installed == null ? "Empty" : newer ? "NewerVersionAvailable" : "LatestVersionInstalled",
        UpdateStatus = installed == null ? "None" : "Success",
    };

    [Fact]
    public async Task A_period_that_is_over_is_read_once_and_then_served_from_the_cache_for_good()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);
        var yesterday = today.AddDays(-2);

        var first = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, yesterday, TestContext.Current.CancellationToken);
        var second = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, yesterday, TestContext.Current.CancellationToken);
        clock.Now = noon.AddDays(400);
        var third = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, yesterday, TestContext.Current.CancellationToken);

        Assert.Equal(1, client.Requests);
        Assert.Equal(first.FetchedUtc, second.FetchedUtc);
        Assert.Equal(first.FetchedUtc, third.FetchedUtc);
        Assert.Equal(yesterday, first.Period);

        // Last month and last year as well, asked by any day in them.
        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Consumption, new DateOnly(2026, 8, 15), TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Consumption, new DateOnly(2026, 8, 31), TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Year, SolarWebView.Expense, new DateOnly(2025, 6, 1), TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Year, SolarWebView.Expense, new DateOnly(2025, 12, 31), TestContext.Current.CancellationToken);
        Assert.Equal(3, client.Requests);

        // Solar.web is asked by the period's first day, whichever day the client named.
        Assert.Equal((SolarWebInterval.Month, SolarWebView.Consumption, new DateOnly(2026, 8, 1)), client.Asked[1]);
    }

    [Fact]
    public async Task A_client_is_served_what_is_cached_however_old_and_only_a_forecast_day_is_read_again()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Today, read for the first client; the next ones get the same chart hours later, because keeping it fresh is the tick's job.
        var first = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        clock.Now = noon.AddHours(3);
        var again = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        Assert.Equal(1, client.Requests);
        Assert.Equal(first.FetchedUtc, again.FetchedUtc);

        // This month, this year and the whole history the same: one request each, then the cache.
        foreach (var interval in new[] { SolarWebInterval.Month, SolarWebInterval.Year, SolarWebInterval.All })
        {
            await service.GetChartAsync(interval, SolarWebView.Production, today, TestContext.Current.CancellationToken);
            clock.Now = clock.Now.AddHours(1);
            await service.GetChartAsync(interval, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        }

        Assert.Equal(4, client.Requests);

        // Tomorrow is a forecast, which no tick refreshes: it is read again once the cached one is a refresh rate old.
        var start = clock.Now;
        var forecast = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(1), TestContext.Current.CancellationToken);
        clock.Now = start.AddMinutes(14);
        Assert.Equal(forecast.FetchedUtc, (await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(1), TestContext.Current.CancellationToken)).FetchedUtc);
        Assert.Equal(5, client.Requests);
        clock.Now = start.AddMinutes(15);
        Assert.Equal(clock.Now.UtcDateTime, (await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(1), TestContext.Current.CancellationToken)).FetchedUtc);
        Assert.Equal(6, client.Requests);
    }

    [Fact]
    public async Task The_tick_reads_the_running_periods_in_every_view_and_a_day_that_has_ended_until_it_is_complete()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);

        // The first tick fills the cache: today in its two views, this month, this year and the whole history in all
        // four, and the two days before today, which ended less than two days ago and which nothing had read yet.
        // Last month and last year ended long ago: what the cache has not got of them, a client fetches when it asks.
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, client.FirmwareRequests);
        Assert.Equal(18, client.Requests);
        Assert.DoesNotContain(client.Asked, a => a.Interval == SolarWebInterval.Day && a.View.IsPremium());
        Assert.Equal(2, client.Asked.Count(a => a == (SolarWebInterval.Day, a.View, today)));
        Assert.Contains((SolarWebInterval.Day, SolarWebView.Consumption, today.AddDays(-1)), client.Asked);
        Assert.Contains((SolarWebInterval.Day, SolarWebView.Production, today.AddDays(-2)), client.Asked);
        Assert.DoesNotContain(client.Asked, a => a.Interval == SolarWebInterval.Day && a.Date == today.AddDays(-3));
        Assert.DoesNotContain(client.Asked, a => a.Interval == SolarWebInterval.Month && a.Date == new DateOnly(2026, 8, 1));
        Assert.DoesNotContain(client.Asked, a => a.Interval == SolarWebInterval.Year && a.Date == new DateOnly(2025, 1, 1));
        Assert.Equal(4, client.Asked.Count(a => a.Interval == SolarWebInterval.All));

        // The next tick reads the running periods only: the two days before today came back complete, with the 23:55 slot.
        clock.Now = noon.AddMinutes(15);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(32, client.Requests);

        // A client meanwhile is served what the last tick read, without a request of its own.
        var chart = await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        Assert.Equal(noon.AddMinutes(15).UtcDateTime, chart.FetchedUtc);
        Assert.Equal(32, client.Requests);

        // Midnight in Berlin passes (22:00 UTC). The tick reads the new day, and the day that has just ended once
        // more, because the chart in the cache ends where the day stood at the last tick; this Solar.web has no lag,
        // so that read is complete and the day is left alone from then on.
        clock.Now = new DateTimeOffset(2026, 9, 20, 22, 5, 0, TimeSpan.Zero);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(48, client.Requests);
        Assert.Equal(2, client.Asked.Skip(32).Count(a => a.Interval == SolarWebInterval.Day && a.Date == today));
        Assert.Equal(2, client.Asked.Skip(32).Count(a => a.Interval == SolarWebInterval.Day && a.Date == today.AddDays(1)));

        clock.Now = new DateTimeOffset(2026, 9, 20, 22, 20, 0, TimeSpan.Zero);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(62, client.Requests);
        Assert.DoesNotContain(client.Asked.Skip(48), a => a.Interval == SolarWebInterval.Day && a.Date == today);
    }

    [Fact]
    public async Task A_day_solar_web_is_behind_on_is_read_again_every_tick_until_its_last_slot_has_arrived()
    {
        // Solar.web is three hours behind the inverter; the server starts five minutes after midnight in Berlin.
        client.Lag = TimeSpan.FromHours(3);
        var midnight = new DateTimeOffset(2026, 9, 20, 22, 0, 0, TimeSpan.Zero);
        clock.Now = midnight.AddMinutes(5);
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Yesterday comes back without its last slot, the day before with it.
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(18, client.Requests);

        // A client asking for yesterday is served the incomplete chart - the cache is what is served - without a request.
        clock.Now = midnight.AddMinutes(10);
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        Assert.Equal(18, client.Requests);

        // Every tick reads yesterday again while its last slot is missing...
        clock.Now = midnight.AddMinutes(20);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(34, client.Requests);
        clock.Now = midnight.AddHours(2).AddMinutes(55);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(50, client.Requests);
        Assert.Equal(6, client.Asked.Count(a => a.Interval == SolarWebInterval.Day && a.Date == today));

        // ...and once more when it has arrived, which is the read that completes it; then it is left alone.
        clock.Now = midnight.AddHours(3).AddMinutes(5);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(66, client.Requests);
        clock.Now = midnight.AddHours(3).AddMinutes(20);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(80, client.Requests);
        Assert.DoesNotContain(client.Asked.Skip(66), a => a.Interval == SolarWebInterval.Day && a.Date == today);
    }

    [Fact]
    public async Task A_month_is_complete_once_the_day_chart_of_its_last_day_is_and_the_month_was_read_after_that()
    {
        // 30 September, noon in Berlin; Solar.web three hours behind.
        client.Lag = TimeSpan.FromHours(3);
        var lastDay = new DateOnly(2026, 9, 30);
        var midnight = new DateTimeOffset(2026, 9, 30, 22, 0, 0, TimeSpan.Zero);
        clock.Now = new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.Zero);
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(18, client.Requests);

        // October. The tick reads the last day of September again, and September in all four views, because its
        // chart was read at noon while the month was still running - and again while the last day's slot is missing.
        clock.Now = midnight.AddMinutes(5);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(38, client.Requests);
        Assert.Equal(2, client.Asked.Skip(18).Count(a => a.Interval == SolarWebInterval.Day && a.Date == lastDay));
        Assert.Equal(4, client.Asked.Skip(18).Count(a => a.Interval == SolarWebInterval.Month && a.Date == new DateOnly(2026, 9, 1)));
        Assert.DoesNotContain(client.Asked.Skip(18), a => a.Interval == SolarWebInterval.Year && a.Date == new DateOnly(2025, 1, 1));

        clock.Now = midnight.AddMinutes(20);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(58, client.Requests);

        // The last slot has arrived: the last day is complete, September is read once more after it, and both are done.
        clock.Now = midnight.AddHours(3).AddMinutes(5);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(78, client.Requests);
        clock.Now = midnight.AddHours(3).AddMinutes(20);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(92, client.Requests);
        Assert.DoesNotContain(client.Asked.Skip(78), a => a.Date == lastDay || a.Interval == SolarWebInterval.Month && a.Date == new DateOnly(2026, 9, 1));

        // The September chart in the cache is the one read after the last day's chart was complete.
        var september = await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2026, 9, 15), TestContext.Current.CancellationToken);
        Assert.Equal(midnight.AddHours(3).AddMinutes(5).UtcDateTime, september.FetchedUtc);
        Assert.Equal(92, client.Requests);
    }

    [Fact]
    public async Task A_year_is_complete_the_same_way_by_the_day_chart_of_31_december()
    {
        // 31 December 2026, noon in Berlin, which is winter time: midnight is 23:00 UTC.
        var midnight = new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.Zero);
        clock.Now = new DateTimeOffset(2026, 12, 31, 11, 0, 0, TimeSpan.Zero);
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(18, client.Requests);

        // 2027. The tick reads 31 December once more - complete, this Solar.web has no lag - and December and 2026
        // after it, in all four views each.
        clock.Now = midnight.AddMinutes(5);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(42, client.Requests);
        Assert.Equal(4, client.Asked.Skip(18).Count(a => a.Interval == SolarWebInterval.Month && a.Date == new DateOnly(2026, 12, 1)));
        Assert.Equal(4, client.Asked.Skip(18).Count(a => a.Interval == SolarWebInterval.Year && a.Date == new DateOnly(2026, 1, 1)));
        Assert.Equal(4, client.Asked.Skip(18).Count(a => a.Interval == SolarWebInterval.Year && a.Date == new DateOnly(2027, 1, 1)));

        clock.Now = midnight.AddMinutes(20);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(56, client.Requests);
        Assert.DoesNotContain(client.Asked.Skip(42), a => a.Date.Year == 2026);
    }

    [Fact]
    public async Task A_premium_view_the_account_is_locked_out_of_is_tried_again_once_a_day_only()
    {
        client.IsPremiumLocked = true;
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(18, client.Requests);

        // The next tick leaves the six locked views alone: two each for the month, the year and the whole history.
        clock.Now = noon.AddMinutes(15);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(26, client.Requests);
        Assert.DoesNotContain(client.Asked.Skip(18), a => a.View.IsPremium());

        // A client gets Solar.web's placeholder from the cache, without a request.
        var locked = await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Expense, today, TestContext.Current.CancellationToken);
        Assert.True(locked.IsPremiumFeature);
        Assert.Equal(26, client.Requests);

        // A day later they are tried again - the account may have Premium by now, and here it has.
        client.IsPremiumLocked = false;
        clock.Now = noon.AddDays(1).AddMinutes(15);
        await service.TickAsync(TestContext.Current.CancellationToken);
        Assert.Equal(6, client.Asked.Skip(26).Count(a => a.View.IsPremium()));
        Assert.False((await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Expense, today, TestContext.Current.CancellationToken)).IsPremiumFeature);
    }

    [Fact]
    public void A_day_chart_is_complete_once_a_measured_series_has_a_value_in_its_last_five_minutes()
    {
        var end = new DateTime(2026, 9, 19, 22, 0, 0, DateTimeKind.Utc);
        var chart = SolarWebHistoryStoreTests.Chart(SolarWebInterval.Day, SolarWebView.Production, new DateOnly(2026, 9, 19), end, "ToConsumer", "PvForecastTruncated", "BattOperatingState");
        var measured = chart.Series[0];
        var forecast = chart.Series[1];
        var bubble = chart.Series[2];
        bubble.ChartType = "bubble";

        // Points up to 23:50 are not enough, nor a null at 23:55, nor the forecast's or the bubbles' points there.
        measured.Points = [new SolarWebPoint { TimeUtc = end.AddMinutes(-10), Value = 178 }, new SolarWebPoint { TimeUtc = end.AddMinutes(-5) }];
        forecast.Points = [new SolarWebPoint { TimeUtc = end.AddMinutes(-5), Value = 0 }, new SolarWebPoint { TimeUtc = end.AddHours(12), Value = 3000 }];
        bubble.Points = [new SolarWebPoint { TimeUtc = end.AddMinutes(-3), Value = 73.1, Text = "Normalbetrieb" }];
        Assert.False(SolarWebPeriod.IsDayComplete(chart, end));

        // A value at 23:55 is; one at midnight belongs to the next day and is not.
        measured.Points[1].Value = 0;
        Assert.True(SolarWebPeriod.IsDayComplete(chart, end));
        measured.Points[1].TimeUtc = end;
        Assert.False(SolarWebPeriod.IsDayComplete(chart, end));
    }

    [Fact]
    public async Task A_429_stops_every_request_for_the_time_it_names_and_the_stale_chart_is_served_meanwhile()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);
        var first = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new SolarWebRateLimitException(TimeSpan.FromMinutes(10));

        // The stale chart, and the block is on.
        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);
        Assert.Equal(2, client.Requests);
        Assert.Equal(first.FetchedUtc, stale.FetchedUtc);
        Assert.Equal(noon.AddMinutes(30), service.UnavailableUntil);

        // Nothing cached for the month: the caller is told how long to wait, and Solar.web is not asked.
        var refused = await Assert.ThrowsAsync<SolarWebUnavailableException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal(TimeSpan.FromMinutes(10), refused.RetryAfter);
        Assert.Equal(2, client.Requests);

        clock.Now = noon.AddMinutes(29);
        await Assert.ThrowsAsync<SolarWebUnavailableException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal(2, client.Requests);

        // The time is up and Solar.web answers again.
        clock.Now = noon.AddMinutes(30);
        client.Refusal = null;
        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        Assert.Equal(3, client.Requests);
        Assert.Null(service.UnavailableUntil);

        // A 429 without a Retry-After takes the configured back-off, and stays a 429 for the caller.
        clock.Now = noon.AddMinutes(60);
        client.Refusal = new SolarWebRateLimitException(null);
        await Assert.ThrowsAsync<SolarWebRateLimitException>(() => service.GetChartAsync(SolarWebInterval.Year, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal(noon.AddMinutes(60) + parameters.DefaultRateLimitBackoff, service.UnavailableUntil);
    }

    [Fact]
    public async Task A_503_or_the_maintenance_page_stops_the_requests_for_the_shorter_back_off_and_serves_the_stale_chart()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);
        var first = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new SolarWebUnavailableException(null, "Maintenance Work");

        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);
        Assert.Equal(first.FetchedUtc, stale.FetchedUtc);
        Assert.Equal(noon.AddMinutes(20) + parameters.UnavailableBackoff, service.UnavailableUntil);

        var refused = await Assert.ThrowsAsync<SolarWebUnavailableException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.IsNotType<SolarWebRateLimitException>(refused);
        Assert.Equal(parameters.UnavailableBackoff, refused.RetryAfter);
        Assert.Equal(2, client.Requests);

        // A Retry-After of its own is respected.
        clock.Now = noon.AddMinutes(20) + parameters.UnavailableBackoff;
        client.Refusal = new SolarWebUnavailableException(TimeSpan.FromMinutes(42));
        await Assert.ThrowsAsync<SolarWebUnavailableException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal(3, client.Requests);
        Assert.Equal(clock.Now.AddMinutes(42), service.UnavailableUntil);

        // Over, and the maintenance page is history.
        clock.Now = clock.Now.AddMinutes(42);
        client.Refusal = null;
        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        Assert.Equal(4, client.Requests);
        Assert.Null(service.UnavailableUntil);
    }

    [Fact]
    public async Task A_rejected_login_is_thrown_once_and_never_tried_again_and_the_cache_still_serves()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new SolarWebLoginException("rejected");

        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);
        Assert.Equal(noon.UtcDateTime, stale.FetchedUtc);
        Assert.Equal(2, client.Requests);

        var refused = await Assert.ThrowsAsync<SolarWebLoginException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal("rejected", refused.InnerException?.Message);
        Assert.Equal(2, client.Requests);

        // Not even with a right password later: the server has to be restarted.
        client.Refusal = null;
        clock.Now = noon.AddHours(5);
        await Assert.ThrowsAsync<SolarWebLoginException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal(2, client.Requests);
    }

    [Fact]
    public async Task Any_other_failure_serves_the_stale_chart_or_reaches_the_caller_and_is_tried_again_next_time()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new HttpRequestException("Solar.web answered 503", null, System.Net.HttpStatusCode.ServiceUnavailable);

        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, tomorrow, TestContext.Current.CancellationToken);
        Assert.Equal(noon.UtcDateTime, stale.FetchedUtc);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken));
        Assert.Equal(3, client.Requests);

        client.Refusal = null;
        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        Assert.Equal(4, client.Requests);
    }

    [Fact]
    public async Task A_premium_view_has_no_day_chart_and_solar_web_is_not_asked_for_one()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetChartAsync(SolarWebInterval.Day, SolarWebView.ReturnOfInvestment, today, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Expense, today, TestContext.Current.CancellationToken));
        Assert.Equal(0, client.Requests);

        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.ReturnOfInvestment, today, TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.All, SolarWebView.Expense, today, TestContext.Current.CancellationToken);
        Assert.Equal(2, client.Requests);
    }

    [Fact]
    public async Task Day_charts_older_than_the_retention_are_removed_at_the_start_and_when_a_day_chart_is_written()
    {
        var fetched = noon.AddDays(-40).UtcDateTime;
        await store.UpsertChartAsync(SolarWebHistoryStoreTests.Chart(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(-31), fetched, "FromGen"), TestContext.Current.CancellationToken);
        await store.UpsertChartAsync(SolarWebHistoryStoreTests.Chart(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(-30), fetched, "FromGen"), TestContext.Current.CancellationToken);
        await store.UpsertChartAsync(SolarWebHistoryStoreTests.Chart(SolarWebInterval.Month, SolarWebView.Production, new DateOnly(2025, 1, 1), fetched, "FromGenToGrid"), TestContext.Current.CancellationToken);

        await service.StartAsync(TestContext.Current.CancellationToken);
        Assert.Equal([new DateOnly(2025, 1, 1), today.AddDays(-30)], store.Charts.Select(c => c.Period).Order());

        // A day passes; writing today's chart removes the day that fell out of the window, the month stays.
        clock.Now = noon.AddDays(1);
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(1), TestContext.Current.CancellationToken);
        Assert.Equal([new DateOnly(2025, 1, 1), today.AddDays(1)], store.Charts.Select(c => c.Period).Order());
    }

    private static IOptionsMonitor<SolarWebParameters> Options(SolarWebParameters parameters) => new ServiceCollection()
        .AddOptions()
        .Configure<SolarWebParameters>(p =>
        {
            p.Settings = parameters.Settings;
            p.RefreshRate = parameters.RefreshRate;
            p.DayRetention = parameters.DayRetention;
            p.FinalAfter = parameters.FinalAfter;
            p.LockedViewRetry = parameters.LockedViewRetry;
            p.MinimumRequestInterval = parameters.MinimumRequestInterval;
            p.DefaultRateLimitBackoff = parameters.DefaultRateLimitBackoff;
            p.UnavailableBackoff = parameters.UnavailableBackoff;
        })
        .BuildServiceProvider()
        .GetRequiredService<IOptionsMonitor<SolarWebParameters>>();
}
