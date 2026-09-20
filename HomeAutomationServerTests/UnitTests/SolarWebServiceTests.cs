using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// When <see cref="SolarWebService"/> asks Solar.web and when it serves the cache: a period that is over is read
/// once, a running one again after the refresh rate, a 429 stops every request for the time it names and the stale
/// chart is served meanwhile, a rejected login is never tried again, and old day charts are removed. The clock is
/// the test's, Solar.web and the store are fakes.
/// </summary>
public sealed class SolarWebServiceTests : IDisposable
{
    private const string PvSystemId = "318e321a-883b-4a82-868a-0fdc784476ad";

    /// <summary>Sunday 2026-09-20, 12:00 in Berlin (10:00 UTC).</summary>
    private static readonly DateTimeOffset noon = new(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly today = new(2026, 9, 20);

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
        Assert.Equal((SolarWebInterval.Month, SolarWebView.Consumption, new DateOnly(2026, 8, 15)), client.Asked[1]);
    }

    [Fact]
    public async Task A_running_period_is_read_again_after_the_refresh_rate_and_only_then()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);

        var first = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        clock.Now = noon.AddMinutes(14);
        var cached = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        clock.Now = noon.AddMinutes(15);
        var fresh = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);

        Assert.Equal(2, client.Requests);
        Assert.Equal(first.FetchedUtc, cached.FetchedUtc);
        Assert.Equal(noon.AddMinutes(15).UtcDateTime, fresh.FetchedUtc);

        // This month, this year and the whole history are running as well; yesterday is not final until a day after its end.
        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Year, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.All, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(-1), TestContext.Current.CancellationToken);
        Assert.Equal(6, client.Requests);

        clock.Now = noon.AddMinutes(31);
        await service.GetChartAsync(SolarWebInterval.Month, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Year, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.All, SolarWebView.Production, today, TestContext.Current.CancellationToken);
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(-1), TestContext.Current.CancellationToken);
        Assert.Equal(10, client.Requests);

        // Two days on, yesterday is final and stays what it was.
        clock.Now = noon.AddDays(2);
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today.AddDays(-1), TestContext.Current.CancellationToken);
        Assert.Equal(10, client.Requests);
    }

    [Fact]
    public async Task A_429_stops_every_request_for_the_time_it_names_and_the_stale_chart_is_served_meanwhile()
    {
        await service.StartAsync(TestContext.Current.CancellationToken);
        var first = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new SolarWebRateLimitException(TimeSpan.FromMinutes(10));

        // The stale chart, and the block is on.
        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
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
        var first = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new SolarWebUnavailableException(null, "Maintenance Work");

        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
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
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new SolarWebLoginException("rejected");

        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
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
        await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);

        clock.Now = noon.AddMinutes(20);
        client.Refusal = new HttpRequestException("Solar.web answered 503", null, System.Net.HttpStatusCode.ServiceUnavailable);

        var stale = await service.GetChartAsync(SolarWebInterval.Day, SolarWebView.Production, today, TestContext.Current.CancellationToken);
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
            p.MinimumRequestInterval = parameters.MinimumRequestInterval;
            p.DefaultRateLimitBackoff = parameters.DefaultRateLimitBackoff;
            p.UnavailableBackoff = parameters.UnavailableBackoff;
        })
        .BuildServiceProvider()
        .GetRequiredService<IOptionsMonitor<SolarWebParameters>>();
}
