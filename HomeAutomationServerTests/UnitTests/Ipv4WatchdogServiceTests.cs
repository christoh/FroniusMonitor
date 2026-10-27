using System.Net;
using System.Net.NetworkInformation;
using De.Hochstaetter.FritzBox.Http;
using De.Hochstaetter.FritzBox.Internet;
using De.Hochstaetter.HomeAutomationServer.Models.Ipv4Watchdog;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServer.Services.Ipv4Watchdog;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// <see cref="Ipv4WatchdogService"/> with a clock the test moves, a pinger the test answers for, the real outage log in
/// a file of its own and <see cref="FakeIpv6Box"/> behind the real <see cref="FritzBoxHttpClient"/>. Every tick is
/// made by hand: the clock's timers never fire.
/// </summary>
public sealed class Ipv4WatchdogServiceTests : IAsyncLifetime
{
    private static readonly IPAddress publicAddress = IPAddress.Parse("203.0.113.10");
    private static readonly IPAddress reference = IPAddress.Parse("192.0.2.53");

    private readonly string directory = Path.Combine(Path.GetTempPath(), "HomeAutomationServerTests", Guid.NewGuid().ToString("N"));
    private readonly ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddTestOutput());
    private readonly ManualTimeProvider clock = new(new DateTimeOffset(2026, 10, 27, 23, 0, 0, TimeSpan.Zero));
    private readonly FakePinger pinger = new();
    private readonly FakeIpv6Box box = new();
    private readonly List<Ipv4WatchdogService> services = [];
    private Ipv4WatchdogStore store = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        store = new Ipv4WatchdogStore(loggers.CreateLogger<Ipv4WatchdogStore>(), Path.Combine(directory, Ipv4WatchdogStore.FileName));
        pinger.Answers[publicAddress] = true;
        pinger.Answers[reference] = true;
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var service in services)
        {
            await service.StopAsync(Token);
            service.Dispose();
        }

        SqliteConnection.ClearAllPools();
        loggers.Dispose();

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task Without_a_configured_box_nothing_is_watched()
    {
        var service = Create(new Ipv4WatchdogSettings { BaseUrl = FakeFritzBox.BaseUrl, UserName = "watchdog" });

        await service.StartAsync(Token);
        await service.TickAsync(Token);

        Assert.False(service.IsEnabled);
        Assert.Equal(Ipv4WatchdogPhase.Stopped, service.Status.Phase);
        Assert.Equal(Ipv4WatchdogTestRequest.NotConfigured, service.RequestTestCycle());
        Assert.Empty(clock.Timers);
        Assert.Empty(pinger.Pings);
        Assert.Empty(box.Calls);
    }

    [Fact]
    public async Task While_IPv4_answers_the_box_is_pinged_once_per_interval_and_left_alone()
    {
        var service = await StartAsync();

        await service.TickAsync(Token);
        Advance(29);
        await service.TickAsync(Token);
        Advance(1);
        await service.TickAsync(Token);

        Assert.Equal([publicAddress, publicAddress], pinger.Pings);
        Assert.Equal(Ipv4WatchdogPhase.Monitoring, service.Status.Phase);
        Assert.True(service.Status.LastCheckAnswered);
        Assert.Empty(box.Calls);
        Assert.Empty(await store.GetOutagesAsync(10, Token));
    }

    [Fact]
    public async Task An_outage_is_detected_after_five_unanswered_pings_and_ended_by_a_cycle()
    {
        var service = await StartAsync();
        await service.TickAsync(Token);
        pinger.Answers[publicAddress] = false;
        box.AfterPut = _ => box.IsReachable = false;
        Advance(30);

        await service.TickAsync(Token);

        Assert.Equal(1 + 5, pinger.Pings.Count(p => p.Equals(publicAddress)));
        Assert.Equal([Ipv6Client.Off], box.ModesPut);
        Assert.Equal(Ipv6Client.Off, box.Mode);
        Assert.Equal(Ipv4WatchdogPhase.SwitchingOn, service.Status.Phase);
        var outage = Assert.Single(await store.GetOutagesAsync(10, Token));
        Assert.Equal(((DateTime?)Now.AddSeconds(-30), Now), (outage.LastGoodUtc, outage.DetectedUtc));
        var cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Equal(((long?)outage.Id, false, Ipv6Client.Automatic, Now, (DateTime?)Now), (cycle.OutageId, cycle.IsTest, cycle.PreviousMode, cycle.StartedUtc, cycle.SwitchedOffUtc));

        // The box reconnects: not there at the first tick after the delay, there at the next.
        Advance(30);
        await service.TickAsync(Token);
        Assert.Equal([Ipv6Client.Off], box.ModesPut);
        box.AfterPut = null;
        box.IsReachable = true;
        Advance(5);
        await service.TickAsync(Token);

        Assert.Equal([Ipv6Client.Off, Ipv6Client.Automatic], box.ModesPut);
        Assert.Equal(Ipv4WatchdogPhase.Verifying, service.Status.Phase);
        Assert.Equal(Now, (await store.GetUnfinishedCycleAsync(Token))?.SwitchedOnUtc);

        pinger.Answers[publicAddress] = true;
        Advance(30);
        await service.TickAsync(Token);

        cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Equal(((Ipv4WatchdogCycleResult?)Ipv4WatchdogCycleResult.Ipv4Back, (DateTime?)Now), (cycle.Result, cycle.FinishedUtc));
        outage = Assert.Single(await store.GetOutagesAsync(10, Token));
        Assert.Equal(((Ipv4Recovery?)Ipv4Recovery.AfterCycle, (DateTime?)Now), (outage.Recovery, outage.RecoveredUtc));
        Assert.Equal(Ipv4WatchdogPhase.Monitoring, service.Status.Phase);
        Assert.Null(service.Status.Outage);
        Assert.Equal(Ipv6Client.Automatic, box.Mode);
    }

    [Fact]
    public async Task IPv6_stays_off_at_least_for_the_delay_although_the_box_answers_earlier()
    {
        var service = await StartWithOutageAsync();

        Advance(29);
        await service.TickAsync(Token);
        Assert.Equal([Ipv6Client.Off], box.ModesPut);

        Advance(1);
        await service.TickAsync(Token);
        Assert.Equal([Ipv6Client.Off, Ipv6Client.Automatic], box.ModesPut);
    }

    [Fact]
    public async Task When_this_sides_IPv4_is_down_too_the_outage_is_logged_but_the_box_is_not_switched()
    {
        pinger.Answers[publicAddress] = false;
        pinger.Answers[reference] = false;
        var service = await StartAsync();

        await service.TickAsync(Token);

        Assert.Single(await store.GetOutagesAsync(10, Token));
        Assert.Empty(await store.GetCyclesAsync(10, Token));
        Assert.Empty(box.Calls);
        Assert.Equal(Ipv4WatchdogPhase.Monitoring, service.Status.Phase);
    }

    [Fact]
    public async Task At_most_three_cycles_start_within_an_hour_and_the_next_one_after_it()
    {
        pinger.Answers[publicAddress] = false;
        var service = await StartAsync();

        for (var cycle = 0; cycle < 3; cycle++)
        {
            await service.TickAsync(Token);
            Advance(30);
            await service.TickAsync(Token);
            Advance(30);
            await service.TickAsync(Token);
            Advance(30);
        }

        await service.TickAsync(Token);

        var cycles = await store.GetCyclesAsync(10, Token);
        Assert.Equal(3, cycles.Count);
        Assert.All(cycles, c => Assert.Equal(Ipv4WatchdogCycleResult.Ipv4StillDown, c.Result));
        Assert.Equal(6, box.ModesPut.Count);
        Assert.Equal(Ipv4WatchdogPhase.Monitoring, service.Status.Phase);
        Assert.Single(await store.GetOutagesAsync(10, Token));

        // The oldest of the three leaves the hour a second after it started an hour ago.
        clock.Now = cycles[^1].StartedUtc.AddHours(1).AddSeconds(1);
        await service.TickAsync(Token);

        Assert.Equal(4, (await store.GetCyclesAsync(10, Token)).Count);
        Assert.Equal(Ipv4WatchdogPhase.SwitchingOn, service.Status.Phase);
    }

    [Fact]
    public async Task IPv6_switched_off_by_somebody_else_is_left_alone()
    {
        box.Mode = Ipv6Client.Off;
        pinger.Answers[publicAddress] = false;
        var service = await StartAsync();

        await service.TickAsync(Token);

        Assert.Empty(box.ModesPut);
        Assert.Empty(await store.GetCyclesAsync(10, Token));
        Assert.Equal(Ipv4WatchdogPhase.Monitoring, service.Status.Phase);
    }

    [Fact]
    public async Task A_box_that_does_not_answer_when_the_outage_is_found_is_not_switched()
    {
        box.IsReachable = false;
        pinger.Answers[publicAddress] = false;
        var service = await StartAsync();

        await service.TickAsync(Token);

        Assert.Empty(box.ModesPut);
        Assert.Empty(await store.GetCyclesAsync(10, Token));
        Assert.Single(await store.GetOutagesAsync(10, Token));
    }

    [Fact]
    public async Task A_server_that_stopped_in_the_middle_of_a_cycle_switches_IPv6_back_on_at_the_next_start()
    {
        var first = await StartWithOutageAsync();
        await first.StopAsync(Token);
        Assert.Equal(Ipv6Client.Off, box.Mode);

        Advance(600);
        var second = await StartAsync();
        Assert.Equal(Ipv4WatchdogPhase.SwitchingOn, second.Status.Phase);
        await second.TickAsync(Token);

        Assert.Equal([Ipv6Client.Off, Ipv6Client.Automatic], box.ModesPut);
        Assert.Equal(Ipv4WatchdogPhase.Verifying, second.Status.Phase);

        Advance(30);
        await second.TickAsync(Token);

        var cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Equal(Ipv4WatchdogCycleResult.Ipv4StillDown, cycle.Result);
    }

    [Fact]
    public async Task A_server_that_stopped_after_switching_back_only_checks_the_result_at_the_next_start()
    {
        var first = await StartWithOutageAsync();
        Advance(30);
        await first.TickAsync(Token);
        Assert.Equal(Ipv4WatchdogPhase.Verifying, first.Status.Phase);
        var switchedOn = Now;
        await first.StopAsync(Token);

        Advance(60);
        var second = await StartAsync();
        await second.TickAsync(Token);
        Advance(30);
        await second.TickAsync(Token);

        Assert.Equal([Ipv6Client.Off, Ipv6Client.Automatic], box.ModesPut);
        var cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Equal((switchedOn, (string?)null), (cycle.SwitchedOnUtc, cycle.Detail));
        Assert.NotNull(cycle.Result);
    }

    [Fact]
    public async Task A_test_cycle_runs_while_IPv4_works_and_is_logged_as_a_test()
    {
        var service = await StartAsync();
        await service.TickAsync(Token);

        Assert.Equal(Ipv4WatchdogTestRequest.Accepted, service.RequestTestCycle());
        Assert.True(service.Status.IsTestCycleRequested);
        await service.TickAsync(Token);

        Assert.Equal(Ipv4WatchdogTestRequest.CycleRunning, service.RequestTestCycle());
        Assert.Equal([Ipv6Client.Off], box.ModesPut);
        Advance(30);
        await service.TickAsync(Token);
        Advance(30);
        await service.TickAsync(Token);

        var cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Equal((true, (long?)null, (Ipv4WatchdogCycleResult?)Ipv4WatchdogCycleResult.Ipv4Back), (cycle.IsTest, cycle.OutageId, cycle.Result));
        Assert.Empty(await store.GetOutagesAsync(10, Token));
        Assert.False(service.Status.IsTestCycleRequested);
    }

    [Fact]
    public async Task A_test_cycle_counts_towards_the_hourly_limit()
    {
        var service = await StartAsync();

        for (var cycle = 0; cycle < 4; cycle++)
        {
            service.RequestTestCycle();
            await service.TickAsync(Token);
            Advance(30);
            await service.TickAsync(Token);
            Advance(30);
            await service.TickAsync(Token);
        }

        Assert.Equal(3, (await store.GetCyclesAsync(10, Token)).Count);
    }

    [Fact]
    public async Task A_box_that_refuses_to_switch_off_ends_the_cycle_as_failed_without_switching_again()
    {
        box.RefusePut = true;
        var service = await StartWithOutageAsync(expectSwitchedOff: false);

        Advance(30);
        await service.TickAsync(Token);

        var cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Equal(Ipv4WatchdogCycleResult.Failed, cycle.Result);
        Assert.Null(cycle.SwitchedOffUtc);
        Assert.Contains("not switched off", cycle.Detail);
        Assert.Contains("invalid value", cycle.Detail);
        Assert.Equal([Ipv6Client.Off], box.ModesPut);
        Assert.Equal(Ipv4WatchdogPhase.Monitoring, service.Status.Phase);
    }

    [Fact]
    public async Task IPv6_switched_back_on_by_somebody_else_is_not_switched_again()
    {
        var service = await StartWithOutageAsync();
        box.Mode = Ipv6Client.Automatic;

        Advance(30);
        await service.TickAsync(Token);

        Assert.Equal([Ipv6Client.Off], box.ModesPut);
        var cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Contains("back in ipv6_automatic already", cycle.Detail);
        Assert.Equal(Ipv4WatchdogPhase.Verifying, service.Status.Phase);
    }

    [Fact]
    public async Task A_box_in_another_mode_after_switching_back_ends_the_cycle_as_failed()
    {
        var service = await StartWithOutageAsync();
        Advance(30);
        await service.TickAsync(Token);
        box.Mode = Ipv6Client.Off;

        Advance(30);
        await service.TickAsync(Token);

        var cycle = Assert.Single(await store.GetCyclesAsync(10, Token));
        Assert.Equal(Ipv4WatchdogCycleResult.Failed, cycle.Result);
        Assert.Contains("reports ipv6_off instead of ipv6_automatic", cycle.Detail);
    }

    [Fact]
    public async Task An_outage_that_ends_by_itself_is_logged_as_such()
    {
        pinger.Answers[publicAddress] = false;
        pinger.Answers[reference] = false;
        var service = await StartAsync();
        await service.TickAsync(Token);

        pinger.Answers[publicAddress] = true;
        Advance(30);
        await service.TickAsync(Token);

        var outage = Assert.Single(await store.GetOutagesAsync(10, Token));
        Assert.Equal(((Ipv4Recovery?)Ipv4Recovery.ByItself, (DateTime?)Now), (outage.Recovery, outage.RecoveredUtc));
    }

    [Fact]
    public async Task A_host_that_cannot_ping_logs_no_outage_and_switches_nothing()
    {
        pinger.Failure = new PingException("An exception occurred during a Ping request.", new UnauthorizedAccessException());
        var service = await StartAsync();

        await service.TickAsync(Token);

        Assert.Empty(await store.GetOutagesAsync(10, Token));
        Assert.Empty(box.Calls);
        Assert.Equal(Ipv4WatchdogPhase.Monitoring, service.Status.Phase);
    }

    [Fact]
    public async Task An_unusable_public_address_leaves_the_watchdog_stopped_instead_of_failing_the_server()
    {
        var service = Create(Settings(publicAddress: "not an address"));

        await service.StartAsync(Token);

        Assert.Equal(Ipv4WatchdogPhase.Stopped, service.Status.Phase);
        Assert.Empty(clock.Timers);
    }

    private DateTime Now => clock.Now.UtcDateTime;

    private void Advance(int seconds) => clock.Now += TimeSpan.FromSeconds(seconds);

    private async Task<Ipv4WatchdogService> StartAsync()
    {
        var service = Create(Settings());
        await service.StartAsync(Token);
        return service;
    }

    /// <summary>Starts, sees IPv4 once, loses it 30 s later and switches IPv6 off; the box stays reachable.</summary>
    private async Task<Ipv4WatchdogService> StartWithOutageAsync(bool expectSwitchedOff = true)
    {
        var service = await StartAsync();
        await service.TickAsync(Token);
        pinger.Answers[publicAddress] = false;
        Advance(30);
        await service.TickAsync(Token);
        Assert.Equal(Ipv4WatchdogPhase.SwitchingOn, service.Status.Phase);
        Assert.Equal(expectSwitchedOff ? Ipv6Client.Off : Ipv6Client.Automatic, box.Mode);
        return service;
    }

    private static Ipv4WatchdogSettings Settings(string? publicAddress = null) => new()
    {
        BaseUrl = FakeFritzBox.BaseUrl,
        UserName = "watchdog",
        Password = "secret",
        PublicAddress = publicAddress ?? Ipv4WatchdogServiceTests.publicAddress.ToString(),
        ReferenceAddresses = reference.ToString(),
    };

    private Ipv4WatchdogService Create(Ipv4WatchdogSettings settings)
    {
        var options = new ServiceCollection()
            .Configure<Ipv4WatchdogParameters>(p => p.Settings = settings)
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<Ipv4WatchdogParameters>>();

        var service = new Ipv4WatchdogService
        (
            loggers.CreateLogger<Ipv4WatchdogService>(),
            options,
            store,
            pinger,
            clock,
            o => new FritzBoxHttpClient(o, loggers.CreateLogger<FritzBoxHttpClient>(), box)
        );

        services.Add(service);
        return service;
    }

    /// <summary>Answers for each address as the test says, and remembers whom it pinged.</summary>
    private sealed class FakePinger : IPinger
    {
        public Dictionary<IPAddress, bool> Answers { get; } = [];

        public List<IPAddress> Pings { get; } = [];

        /// <summary>Thrown instead of answering, as on a host that may not ping.</summary>
        public Exception? Failure { get; set; }

        public Task<bool> PingAsync(IPAddress address, TimeSpan timeout, CancellationToken token = default)
        {
            Pings.Add(address);
            return Failure is null ? Task.FromResult(Answers.GetValueOrDefault(address)) : Task.FromException<bool>(Failure);
        }
    }
}
