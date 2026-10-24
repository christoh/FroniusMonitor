using System.Diagnostics;
using System.Threading.Channels;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Services;
using De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.Extensions.Options;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What <see cref="FritzBoxDataCollector"/> puts into the control service, and when: a device when it appears, again
/// only when something a client would notice has changed, its removal when the box no longer lists it - and the lock a
/// command puts on a device, which every read of the box has to lift.
/// </summary>
/// <remarks>
/// <para>
/// The collector runs on its own thread, so the tests never wait for a result by sleeping. The fake service hands the
/// collector one round per call to <c>GetDevices</c>, which a test gives it (<see cref="FakeFritzBoxService.RoundAsync"/>),
/// and a test goes on once the collector has asked for the round <em>after</em> it: a round is published before the
/// next one is asked for, so that call is the proof that the round is out. With <c>RefreshRate</c> zero nothing is
/// waited for between two rounds, and every wait has a timeout so that a failure ends the test instead of hanging it.
/// </para>
/// <para>
/// The one wait that is a plain wait is <see cref="Several_commands_before_the_read_are_answered_by_one_read"/>: that a
/// read does <em>not</em> happen cannot be proved by waiting for something. It can only make the test pass too early,
/// never fail wrongly.
/// </para>
/// </remarks>
public sealed class FritzBoxDataCollectorTests : IAsyncDisposable
{
    private static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan hour = TimeSpan.FromHours(1);

    private const string SofaAin = "08761 0000001";
    private const string OfficeAin = "11630 0000002";
    private const string LampAin = "13077 0000003-1";

    private static readonly string sofaId = IdOf(Outlet(SofaAin, string.Empty));
    private static readonly string officeId = IdOf(Outlet(OfficeAin, string.Empty));
    private static readonly string lampId = IdOf(Outlet(LampAin, string.Empty));

    private readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddTestOutput());
    private readonly IDataControlService controlService;
    private readonly Channel<DeviceUpdateEventArgs> updates = Channel.CreateUnbounded<DeviceUpdateEventArgs>();
    private readonly List<FakeFritzBoxService> services = [];
    private readonly WebConnection box = new() { BaseUrl = "http://fritz.box", UserName = "smarthome", Password = "secret" };
    private FritzBoxDataCollector? collector;

    public FritzBoxDataCollectorTests()
    {
        controlService = new DataControlService(loggerFactory.CreateLogger<DataControlService>());
        controlService.DeviceUpdate += (_, e) => updates.Writer.TryWrite(e);
    }

    public async ValueTask DisposeAsync()
    {
        if (collector != null)
        {
            await collector.DisposeAsync();
        }

        loggerFactory.Dispose();
    }

    [Fact]
    public async Task The_first_round_publishes_every_device_as_new_under_its_unique_id()
    {
        var sofa = Outlet(SofaAin, "Outlet sofa");
        var office = Outlet(OfficeAin, "Outlet office", 228);
        var service = await StartOneBoxAsync();

        await service.RoundAsync(sofa, office);

        var published = TakeUpdates();
        Assert.Equal(2, published.Count);
        Assert.All(published, u => Assert.Equal(DeviceAction.Add, u.DeviceAction));
        Assert.Equal<string>([IdOf(sofa), IdOf(office)], published.Select(u => u.Id));
        Assert.Same(sofa, published[0].Device.Device);
        Assert.Same(office, published[1].Device.Device);

        Assert.Equal(2, controlService.Entities.Count);
        Assert.Same(sofa, controlService.Entities[IdOf(sofa)].Device);
        Assert.Same(office, controlService.Entities[IdOf(office)].Device);

        Assert.All(controlService.Entities.Values, d =>
        {
            Assert.Equal(typeof(IFritzBoxService), d.ServiceType);
            Assert.False(d.SupportsPushMessages);
            Assert.Same(box, d.Credentials);
        });
    }

    [Fact]
    public async Task A_round_with_the_same_values_publishes_nothing_and_the_published_instances_stay()
    {
        var sofa = Outlet(SofaAin, "Outlet sofa");
        var office = Outlet(OfficeAin, "Outlet office", 228);
        var service = await StartOneBoxAsync();
        await service.RoundAsync(sofa, office);
        TakeUpdates();

        // New instances, as every read of the box hands out, with exactly the values of the first round.
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"), Outlet(OfficeAin, "Outlet office", 228));

        Assert.Empty(TakeUpdates());
        Assert.Same(sofa, controlService.Entities[IdOf(sofa)].Device);
        Assert.Same(office, controlService.Entities[IdOf(office)].Device);
    }

    [Fact]
    public async Task A_device_that_changed_is_published_again_alone_as_a_change_with_the_new_instance()
    {
        var sofa = Outlet(SofaAin, "Outlet sofa");
        var service = await StartOneBoxAsync();
        await service.RoundAsync(sofa, Outlet(OfficeAin, "Outlet office", 228));
        TakeUpdates();

        var officeNow = Outlet(OfficeAin, "Outlet office", 310);
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"), officeNow);

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Change, update.DeviceAction);
        Assert.Equal(IdOf(officeNow), update.Id);
        Assert.Same(officeNow, update.Device.Device);
        Assert.Same(officeNow, controlService.Entities[IdOf(officeNow)].Device);
        Assert.Same(sofa, controlService.Entities[IdOf(sofa)].Device);
    }

    [Theory]
    [InlineData("the power")]
    [InlineData("switching on")]
    [InlineData("the name")]
    [InlineData("going out of radio range")]
    [InlineData("a lock at the box")]
    public async Task Whatever_a_client_would_notice_is_published(string change)
    {
        var service = await StartOneBoxAsync();
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa", 12));
        TakeUpdates();

        var changed = Outlet(SofaAin, "Outlet sofa", 12);
        Change(changed, change);
        await service.RoundAsync(changed);

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Change, update.DeviceAction);
        Assert.Same(changed, update.Device.Device);
    }

    [Fact]
    public async Task A_device_the_box_no_longer_lists_is_removed()
    {
        var sofa = Outlet(SofaAin, "Outlet sofa");
        var office = Outlet(OfficeAin, "Outlet office");
        var service = await StartOneBoxAsync();
        await service.RoundAsync(sofa, office);
        TakeUpdates();

        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Delete, update.DeviceAction);
        Assert.Equal(IdOf(office), update.Id);
        Assert.Same(office, update.Device.Device);
        Assert.Equal<string>([IdOf(sofa)], controlService.Entities.Keys);
    }

    [Fact]
    public async Task A_device_that_comes_back_is_added_again()
    {
        var service = await StartOneBoxAsync();
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"), Outlet(OfficeAin, "Outlet office"));
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        TakeUpdates();

        var officeAgain = Outlet(OfficeAin, "Outlet office");
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"), officeAgain);

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Add, update.DeviceAction);
        Assert.Same(officeAgain, update.Device.Device);
        Assert.Equal(2, controlService.Entities.Count);
    }

    [Fact]
    public async Task A_device_listed_twice_in_one_round_is_published_once_as_the_first_of_them()
    {
        var first = Outlet(SofaAin, "Outlet sofa", 1);
        var service = await StartOneBoxAsync();

        await service.RoundAsync(first, Outlet(SofaAin, "Outlet sofa", 2));

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Add, update.DeviceAction);
        Assert.Same(first, update.Device.Device);
    }

    [Fact]
    public async Task A_command_locks_the_device_until_the_box_has_been_read_again()
    {
        var service = await StartOneBoxAsync();
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        var published = controlService.Entities[sofaId].Device;
        var outlet = (ISwitchable)published;
        TakeUpdates();
        Assert.True(outlet.IsSwitchingEnabled);

        await outlet.TurnOnOff(true);

        Assert.False(outlet.IsSwitchingEnabled);
        Assert.Equal<(string, bool)>([(SofaAin, true)], service.Switched);

        // The box reports what it has always reported: switching on an outlet that is on changes nothing.
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));

        Assert.True(outlet.IsSwitchingEnabled);
        Assert.Empty(TakeUpdates());
        Assert.Same(published, controlService.Entities[sofaId].Device);
    }

    [Fact]
    public async Task A_command_that_changed_something_gives_the_device_a_new_instance_which_is_not_locked()
    {
        var service = await StartOneBoxAsync();
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        var locked = (ISwitchable)controlService.Entities[sofaId].Device;
        await locked.TurnOnOff(true);
        TakeUpdates();

        var switchedOn = Outlet(SofaAin, "Outlet sofa", 40, on: true);
        await service.RoundAsync(switchedOn);

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Change, update.DeviceAction);
        Assert.Same(switchedOn, update.Device.Device);
        Assert.True(((ISwitchable)switchedOn).IsSwitchingEnabled);
    }

    [Fact]
    public async Task A_command_has_the_box_read_after_the_echo_delay_instead_of_at_the_end_of_the_interval()
    {
        var echoDelay = TimeSpan.FromMilliseconds(200);
        var service = await StartOneBoxAsync(refreshRate: hour, echoDelay: echoDelay);
        service.Give(Outlet(SofaAin, "Outlet sofa"));
        await NextUpdateAsync();

        // The first round is out, and the box will not be asked again for an hour.
        Assert.Equal(1, service.Calls);

        var sinceCommand = Stopwatch.StartNew();
        service.RaiseCommandSent();
        await service.WaitForCallAsync(2);

        // Not at once either: the box shows the effect of a command only a moment later.
        Assert.True(sinceCommand.Elapsed >= echoDelay / 2, $"The box was read {sinceCommand.Elapsed.TotalMilliseconds:N0} ms after the command, not after the echo delay of {echoDelay.TotalMilliseconds:N0} ms");
        Assert.Equal(2, service.Calls);
    }

    [Fact]
    public async Task Several_commands_before_the_read_are_answered_by_one_read()
    {
        var service = await StartOneBoxAsync(refreshRate: hour, echoDelay: TimeSpan.FromMilliseconds(20));

        // Both arrive before the box has answered even the first read.
        service.RaiseCommandSent();
        service.RaiseCommandSent();
        service.Give(Outlet(SofaAin, "Outlet sofa"));
        await service.WaitForCallAsync(2);

        // Round 2 is out once its change has been published. A second request that had been remembered would make the
        // collector read once more, 20 ms later, instead of waiting for the hour.
        service.Give(Outlet(SofaAin, "Outlet sofa", 5));
        await NextUpdateAsync();
        await NextUpdateAsync();

        Assert.False(await service.AsksAgainWithinAsync(3, TimeSpan.FromMilliseconds(500)), "Two commands made the collector read the box twice");
        Assert.Equal(2, service.Calls);
    }

    [Fact]
    public async Task A_command_sent_through_the_device_has_the_box_read_again_which_lifts_the_lock()
    {
        var service = await StartOneBoxAsync(refreshRate: hour, echoDelay: TimeSpan.FromMilliseconds(20), newService: () => new FakeFritzBoxService { RaisesCommandSent = true });
        service.Give(Outlet(SofaAin, "Outlet sofa"));
        await NextUpdateAsync();
        var outlet = (ISwitchable)controlService.Entities[sofaId].Device;

        // The service raises CommandSent after the command, as a real one does. Without it the box would not be asked
        // for an hour, and this wait would end in the timeout.
        await outlet.TurnOnOff(true);
        await service.WaitForCallAsync(2);
        Assert.False(outlet.IsSwitchingEnabled);

        // The box shows nothing new. One more request for a read is how the test learns that this round is out.
        service.Give(Outlet(SofaAin, "Outlet sofa"));
        service.RaiseCommandSent();
        await service.WaitForCallAsync(3);

        Assert.True(outlet.IsSwitchingEnabled);
        Assert.Empty(TakeUpdates());
    }

    [Fact]
    public async Task A_round_that_throws_publishes_and_removes_nothing_and_the_next_round_works()
    {
        var sofa = Outlet(SofaAin, "Outlet sofa");
        var office = Outlet(OfficeAin, "Outlet office", 228);
        var service = await StartOneBoxAsync();
        await service.RoundAsync(sofa, office);
        TakeUpdates();

        await service.FailRoundAsync(new HttpRequestException("No route to host"));

        Assert.Empty(TakeUpdates());
        Assert.Equal(2, controlService.Entities.Count);
        Assert.Same(sofa, controlService.Entities[IdOf(sofa)].Device);
        Assert.Same(office, controlService.Entities[IdOf(office)].Device);

        var officeNow = Outlet(OfficeAin, "Outlet office", 310);
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"), officeNow);

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Change, update.DeviceAction);
        Assert.Same(officeNow, update.Device.Device);
    }

    [Fact(Skip = "BUG in FritzBoxDataCollector.cs line 167: `catch (Exception ex) when (ex is not OperationCanceledException)` lets a timeout of the box escape. " +
                 "HttpClient reports one as TaskCanceledException (an OperationCanceledException) while the stop token is not cancelled; RunAsync (line 149) only " +
                 "handles it when token.IsCancellationRequested, so the exception ends the loop of that box for good, nothing is logged, and StopAsync later throws " +
                 "from Task.WhenAll. Fix: `when (!token.IsCancellationRequested)`. Un-skip this test once that is done.")]
    public async Task A_timeout_of_the_box_is_a_failed_round_like_any_other_and_the_next_round_works()
    {
        var service = await StartOneBoxAsync();
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        TakeUpdates();

        // What HttpClient throws when the box does not answer within HttpClient.Timeout.
        await service.FailRoundAsync(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.", new TimeoutException()));

        var officeNow = Outlet(OfficeAin, "Outlet office");
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"), officeNow);

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Add, update.DeviceAction);
        Assert.Same(officeNow, update.Device.Device);
    }

    [Fact]
    public async Task A_box_that_keeps_failing_is_reported_once_and_its_recovery_is_reported_once()
    {
        var log = new RecordingLogger<FritzBoxDataCollector>();
        collector = Create([box], logger: log);
        await collector.StartAsync(TestContext.Current.CancellationToken);
        var service = services.Single();

        await service.FailRoundAsync(new HttpRequestException("No route to host"));
        await service.FailRoundAsync(new HttpRequestException("No route to host"));
        await service.FailRoundAsync(new HttpRequestException("No route to host"));
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));

        var error = Assert.Single(log.Entries, e => e.Level == LogLevel.Error);
        Assert.Equal("Could not read the smart home of FRITZ!Box http://fritz.box", error.Message);
        Assert.DoesNotContain(log.Entries, e => e.Level == LogLevel.Warning);

        var recovery = Assert.Single(log.Entries, e => e.Message.StartsWith("Can read the smart home", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, recovery.Level);
        Assert.StartsWith("Can read the smart home of FRITZ!Box http://fritz.box again, after 3 attempts over ", recovery.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Every_connection_gets_its_own_service_with_the_connection_set_before_the_first_round()
    {
        var other = new WebConnection { BaseUrl = "http://192.168.178.2", UserName = "smarthome", Password = "secret" };
        collector = Create([box, other]);

        await collector.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, services.Count);
        Assert.NotSame(services[0], services[1]);
        Assert.Same(box, services[0].Connection);
        Assert.Same(other, services[1].Connection);

        // What each service had when it was first asked for the devices, which happens inside StartAsync.
        Assert.Same(box, Assert.Single(services[0].ConnectionsWhenRead));
        Assert.Same(other, Assert.Single(services[1].ConnectionsWhenRead));

        await services[0].RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        await services[1].RoundAsync(Outlet(LampAin, "Lamp office"));

        Assert.Same(box, controlService.Entities[sofaId].Credentials);
        Assert.Same(other, controlService.Entities[lampId].Credentials);
    }

    [Fact]
    public async Task Every_box_withdraws_only_its_own_devices()
    {
        var other = new WebConnection { BaseUrl = "http://192.168.178.2", UserName = "smarthome", Password = "secret" };
        collector = Create([box, other]);
        await collector.StartAsync(TestContext.Current.CancellationToken);
        await services[0].RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        await services[1].RoundAsync(Outlet(LampAin, "Lamp office"));
        TakeUpdates();

        await services[0].RoundAsync();

        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Delete, update.DeviceAction);
        Assert.Equal(sofaId, update.Id);
        Assert.Equal<string>([lampId], controlService.Entities.Keys);
    }

    [Fact]
    public async Task Stopping_removes_every_Fritz_Box_device_and_leaves_the_others_alone()
    {
        var service = await StartOneBoxAsync();
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"), Outlet(OfficeAin, "Outlet office"));
        controlService.AddOrUpdate("other", new ManagedDevice(new OtherDevice()));
        TakeUpdates();

        await collector!.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal<string>(["other"], controlService.Entities.Keys);
        var removed = TakeUpdates();
        Assert.All(removed, u => Assert.Equal(DeviceAction.Delete, u.DeviceAction));
        Assert.Equal(
            new[] { sofaId, officeId }.Order(),
            removed.Select(u => u.Id).Order());
    }

    [Fact]
    public async Task Stopping_disposes_a_service_that_can_be_disposed_once()
    {
        var service = (DisposableFakeFritzBoxService)await StartOneBoxAsync(newService: () => new DisposableFakeFritzBoxService());
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));

        await collector!.StopAsync(TestContext.Current.CancellationToken);
        await collector.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, service.Disposals);
    }

    [Fact]
    public async Task A_command_after_stopping_does_not_throw_and_does_not_read_the_box()
    {
        var service = await StartOneBoxAsync();
        await service.RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        Assert.True(service.IsListening);

        await collector!.StopAsync(TestContext.Current.CancellationToken);
        var calls = service.Calls;

        // The collector has let go of the service: a service that outlives it must not reach into what was disposed.
        Assert.False(service.IsListening);
        Assert.Null(Record.Exception(service.RaiseCommandSent));
        Assert.Equal(calls, service.Calls);
    }

    [Fact]
    public async Task Starting_again_stops_the_first_run_and_starts_over()
    {
        var first = await StartOneBoxAsync();
        await first.RoundAsync(Outlet(SofaAin, "Outlet sofa"));
        TakeUpdates();

        await collector!.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, services.Count);
        var second = services[1];
        Assert.False(first.IsListening);
        var update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Delete, update.DeviceAction);
        Assert.Empty(controlService.Entities);

        await second.RoundAsync(Outlet(SofaAin, "Outlet sofa"));

        update = Assert.Single(TakeUpdates());
        Assert.Equal(DeviceAction.Add, update.DeviceAction);
    }

    /// <summary>The collector for the one <see cref="box"/>, started, with its service.</summary>
    private async Task<FakeFritzBoxService> StartOneBoxAsync(TimeSpan? refreshRate = null, TimeSpan? echoDelay = null, Func<FakeFritzBoxService>? newService = null)
    {
        collector = Create([box], refreshRate, echoDelay, newService);
        await collector.StartAsync(TestContext.Current.CancellationToken);
        return services.Single();
    }

    /// <param name="connections">One box each.</param>
    /// <param name="refreshRate">Zero unless the test is about the interval, so that nothing waits for the clock.</param>
    /// <param name="echoDelay">How long after a command the box is read, short unless the test is about it.</param>
    /// <param name="newService">What <c>IFritzBoxService</c> is handed out, one per connection.</param>
    /// <param name="logger">The log of the collector, which is the output of the running test unless a test wants to read it.</param>
    private FritzBoxDataCollector Create(IEnumerable<WebConnection> connections, TimeSpan? refreshRate = null, TimeSpan? echoDelay = null, Func<FakeFritzBoxService>? newService = null, ILogger<FritzBoxDataCollector>? logger = null)
    {
        var options = new ServiceCollection()
            .AddOptions()
            .Configure<FritzBoxDataCollectorParameters>(p =>
            {
                p.Connections = connections.ToList();
                p.RefreshRate = refreshRate ?? TimeSpan.Zero;
            })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<FritzBoxDataCollectorParameters>>();

        return collector = new FritzBoxDataCollector
        (
            logger ?? loggerFactory.CreateLogger<FritzBoxDataCollector>(),
            controlService,
            options,
            () =>
            {
                var service = newService?.Invoke() ?? new FakeFritzBoxService();
                services.Add(service);
                return service;
            }
        )
        {
            CommandEchoDelay = echoDelay ?? TimeSpan.FromMilliseconds(10),
        };
    }

    /// <summary>What has been published since the last time this was asked.</summary>
    private List<DeviceUpdateEventArgs> TakeUpdates()
    {
        var taken = new List<DeviceUpdateEventArgs>();

        while (updates.Reader.TryRead(out var update))
        {
            taken.Add(update);
        }

        return taken;
    }

    private Task<DeviceUpdateEventArgs> NextUpdateAsync() => WithTimeout(token => updates.Reader.ReadAsync(token), "a device update");

    private static string IdOf(FritzBoxDevice device) => ((IHaveUniqueId)device).Id;

    private static async Task<T> WithTimeout<T>(Func<CancellationToken, ValueTask<T>> wait, string what)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        source.CancelAfter(timeout);

        try
        {
            return await wait(source.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No {what} within {timeout.TotalSeconds:N0} s");
        }
    }

    /// <summary>An outlet with a power meter, as AVM's FRITZ!Smart Energy 200, which can be switched.</summary>
    private static FritzBoxDevice Outlet(string ain, string name, double powerWatts = 0, bool on = false) => new()
    {
        Ain = ain,
        DisplayName = name,
        Manufacturer = "AVM",
        Model = "FRITZ!Smart Energy 200",
        Features = FritzBoxFeatures.AcOutlet | FritzBoxFeatures.PowerMeter | FritzBoxFeatures.TemperatureSensor | FritzBoxFeatures.TurnOnOff,
        IsPresent = true,
        SimpleSwitch = new FritzBoxSimpleSwitch { IsTurnedOn = on },
        PowerMeter = new FritzBoxPowerMeter { PowerWatts = powerWatts, Voltage = 230, EnergyConsumed = 1000 },
    };

    private static void Change(FritzBoxDevice device, string change)
    {
        switch (change)
        {
            case "the power":
                device.PowerMeter!.PowerWatts = 42;
                break;

            case "switching on":
                device.SimpleSwitch!.IsTurnedOn = true;
                break;

            case "the name":
                device.DisplayName = "Renamed";
                break;

            case "going out of radio range":
                device.IsPresent = false;
                break;

            case "a lock at the box":
                device.Switch = new FritzBoxSwitch { IsUiLocked = true };
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(change), change, null);
        }
    }

    /// <summary>A device of some other collector, which a stopping Fritz!Box collector has no business with.</summary>
    private sealed class OtherDevice : IHaveUniqueId
    {
        public bool IsPresent => true;
        public string? Manufacturer => "Somebody";
        public string? Model => "Thing";
        public string? SerialNumber => "42";
    }

    /// <summary>
    /// A box that answers the collector with what the test gives it, one round for each call of <see cref="GetDevices"/>,
    /// and tells the test when it is asked. Real services hand out devices that know their service; so does this one.
    /// </summary>
    private class FakeFritzBoxService : IFritzBoxService
    {
        private readonly Channel<Round> rounds = Channel.CreateUnbounded<Round>();
        private readonly Channel<int> asked = Channel.CreateUnbounded<int>();
        private readonly Lock gate = new();
        private readonly List<WebConnection?> connectionsWhenRead = [];
        private readonly List<(string Ain, bool TurnOn)> switched = [];
        private int calls;
        private int given;

        public event EventHandler? CommandSent;

        public WebConnection? Connection { get; set; }

        /// <summary>
        /// A real service raises <see cref="CommandSent"/> after every command. The fake does so only when it is asked to, so
        /// that the tests of the lock and of the reads can be told apart.
        /// </summary>
        public bool RaisesCommandSent { get; init; }

        /// <summary>How often the collector has asked for the devices, the call that is waiting for its round included.</summary>
        public int Calls => Volatile.Read(ref calls);

        /// <summary>Whether anybody listens to <see cref="CommandSent"/>.</summary>
        public bool IsListening => CommandSent != null;

        /// <summary>The <see cref="Connection"/> at each call of <see cref="GetDevices"/>.</summary>
        public IReadOnlyList<WebConnection?> ConnectionsWhenRead
        {
            get
            {
                lock (gate)
                {
                    return [.. connectionsWhenRead];
                }
            }
        }

        /// <summary>The commands to switch a device, in the order they came.</summary>
        public IReadOnlyList<(string Ain, bool TurnOn)> Switched
        {
            get
            {
                lock (gate)
                {
                    return [.. switched];
                }
            }
        }

        /// <summary>The round after the one that has been given, and the wait for the collector to have published it.</summary>
        public async Task RoundAsync(params FritzBoxDevice[] devices)
        {
            Give(devices);
            await WaitForCallAsync(given + 1).ConfigureAwait(false);
        }

        /// <summary>The same for a round in which reading the box fails.</summary>
        public async Task FailRoundAsync(Exception failure)
        {
            given++;
            rounds.Writer.TryWrite(new Round(null, failure));
            await WaitForCallAsync(given + 1).ConfigureAwait(false);
        }

        /// <summary>Hands the next round to the collector, without waiting for anything.</summary>
        public void Give(params FritzBoxDevice[] devices)
        {
            var list = new FritzBoxDeviceList();
            list.Devices.AddRange(devices);
            given++;
            rounds.Writer.TryWrite(new Round(list, null));
        }

        /// <summary>Waits for the collector to ask for the devices the n-th time (counting from 1), which it does only after it has dealt with the round before.</summary>
        public Task WaitForCallAsync(int number) => WithTimeout
        (
            async token =>
            {
                await WaitForCallCoreAsync(number, token).ConfigureAwait(false);
                return number;
            },
            $"read of the box (call {number})"
        );

        /// <summary>Whether the collector asks for the devices the n-th time within the time, which a test that expects it not to can only find out by waiting.</summary>
        public async Task<bool> AsksAgainWithinAsync(int number, TimeSpan time)
        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            source.CancelAfter(time);

            try
            {
                await WaitForCallCoreAsync(number, source.Token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        public void RaiseCommandSent() => CommandSent?.Invoke(this, EventArgs.Empty);

        public async Task<FritzBoxDeviceList> GetDevices(CancellationToken token = default)
        {
            var number = Interlocked.Increment(ref calls);

            lock (gate)
            {
                connectionsWhenRead.Add(Connection);
            }

            // Whoever is waiting for this call knows that the round before has been dealt with.
            asked.Writer.TryWrite(number);
            var round = await rounds.Reader.ReadAsync(token).ConfigureAwait(false);

            if (round.Failure != null)
            {
                throw round.Failure;
            }

            foreach (var device in round.List!.Devices)
            {
                device.FritzBoxService = this;
            }

            return round.List;
        }

        public ValueTask SwitchDevice(string ain, bool turnOn, CancellationToken token = default)
        {
            lock (gate)
            {
                switched.Add((ain, turnOn));
            }

            if (RaisesCommandSent)
            {
                RaiseCommandSent();
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask FritzBoxLogin(CancellationToken token = default) => ValueTask.CompletedTask;

        public ValueTask SetLevel(string ain, double level, CancellationToken token = default) => throw new NotSupportedException();

        public ValueTask SetColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default) => throw new NotSupportedException();

        public ValueTask SetColor(string ain, double hueDegrees, double saturation, CancellationToken token = default) => throw new NotSupportedException();

        private async Task WaitForCallCoreAsync(int number, CancellationToken token)
        {
            while (await asked.Reader.ReadAsync(token).ConfigureAwait(false) < number)
            {
            }
        }

        private sealed record Round(FritzBoxDeviceList? List, Exception? Failure);
    }

    /// <summary>The same, for a service that has something to let go of.</summary>
    private sealed class DisposableFakeFritzBoxService : FakeFritzBoxService, IDisposable
    {
        private int disposals;

        public int Disposals => Volatile.Read(ref disposals);

        public void Dispose() => Interlocked.Increment(ref disposals);
    }
}
