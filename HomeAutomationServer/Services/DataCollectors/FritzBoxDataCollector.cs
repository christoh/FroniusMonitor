using System.Text.Json;

namespace De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;

/// <summary>
/// Polls every configured FRITZ!Box for its smart home and publishes the devices to <see cref="IDataControlService"/>:
/// a device when it appears, again only when something about it has changed, and its removal when the box no longer
/// has it.
/// </summary>
/// <remarks>
/// <para>
/// <c>SignalRDispatcher</c> sends whatever is published to every client, and the box is polled every
/// <see cref="PolledWebConnectionParameterBase.RefreshRate"/> - two seconds. Publishing every device on every poll
/// sent the same state of a dozen outlets to every client every two seconds. Now the clients only hear of a change,
/// the way they do of an air conditioner; a client that connects is greeted with every device all the same.
/// </para>
/// <para>
/// "Changed" means the device's JSON differs from what was last published, which is exactly what a client would
/// notice. A device that has not changed still takes the values of every poll (<see cref="FritzBoxDevice.CopyFrom"/>),
/// unseen by the clients, because that is what lifts the lock a command puts on it: <c>DevicesController</c> refuses
/// the next command until the device has been read again, and a command that changes nothing - switching on an
/// outlet that is on - would otherwise have locked it for good.
/// </para>
/// <para>
/// The box shows the effect of a command about a second later (FRITZ!OS 8.25, measured 2026-10-24), so a command
/// sent through <see cref="IFritzBoxService.CommandSent"/> has the box read <see cref="CommandEchoDelay"/> after it
/// rather than at the end of the interval.
/// </para>
/// </remarks>
public sealed class FritzBoxDataCollector : IHomeAutomationRunner, IAsyncDisposable
{
    /// <summary>How long an outage of one box stays out of the log before it is mentioned again.</summary>
    private static readonly TimeSpan reminderInterval = TimeSpan.FromHours(1);

    /// <summary>What the log calls a box.</summary>
    private const string FritzBox = "FRITZ!Box";

    private readonly ILogger<FritzBoxDataCollector> logger;
    private readonly IDataControlService dataControlService;
    private readonly IOptionsMonitor<FritzBoxDataCollectorParameters> options;
    private readonly Func<IFritzBoxService> createService;
    private readonly List<Box> boxes = [];
    private CancellationTokenSource? tokenSource;

    public FritzBoxDataCollector(ILogger<FritzBoxDataCollector> logger, IDataControlService dataControlService, IOptionsMonitor<FritzBoxDataCollectorParameters> options)
        : this(logger, dataControlService, options, IoC.Get<IFritzBoxService>) { }

    internal FritzBoxDataCollector
    (
        ILogger<FritzBoxDataCollector> logger,
        IDataControlService dataControlService,
        IOptionsMonitor<FritzBoxDataCollectorParameters> options,
        Func<IFritzBoxService> createService
    )
    {
        this.logger = logger;
        this.dataControlService = dataControlService;
        this.options = options;
        this.createService = createService;
    }

    /// <summary>How long after a command the box is read. Settable for the tests.</summary>
    internal TimeSpan CommandEchoDelay { get; init; } = TimeSpan.FromSeconds(1);

    private FritzBoxDataCollectorParameters Parameters => options.CurrentValue;

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        tokenSource = new CancellationTokenSource();

        foreach (var connection in Parameters.Connections ?? throw new InvalidOperationException("No FRITZ!Box connections are configured"))
        {
            var service = createService();
            service.Connection = connection;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Adding Fritz!Box at {Url} with user name {UserName}", connection.BaseUrl, connection.UserName);
            }

            var box = new Box(connection, service);
            box.Task = RunAsync(box, tokenSource.Token);
            boxes.Add(box);
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (tokenSource != null)
        {
            await tokenSource.CancelAsync().ConfigureAwait(false);
            await Task.WhenAll(boxes.Select(b => b.Task)).ConfigureAwait(false);
        }

        foreach (var box in boxes)
        {
            box.Dispose();
        }

        boxes.Clear();
        var devices = dataControlService.Entities.Where(e => e.Value.Device is FritzBoxDevice).Select(e => e.Key).ToList();
        await dataControlService.RemoveAsync(devices, token).ConfigureAwait(false);
        tokenSource?.Dispose();
        tokenSource = null;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fritz!Box thread did not stop correctly");
        }
    }

    public void Dispose()
    {
        try
        {
            StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fritz!Box thread did not stop correctly");
        }
    }

    /// <summary>Reads the box, every interval and soon after a command, until the collector is stopped.</summary>
    private async Task RunAsync(Box box, CancellationToken token)
    {
        try
        {
            while (true)
            {
                var start = DateTime.UtcNow;
                await PollAsync(box, token).ConfigureAwait(false);
                var wait = Parameters.RefreshRate - (DateTime.UtcNow - start);

                if (await box.ReadNow.WaitAsync(wait > TimeSpan.Zero ? wait : TimeSpan.Zero, token).ConfigureAwait(false))
                {
                    await Task.Delay(CommandEchoDelay, token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Stopped Fritz!Box {WebConnection}", box.Connection);
            }
        }
    }

    /// <summary>
    /// One round. Whatever goes wrong in it is logged and ends the round only - a timeout of the box too, which
    /// <see cref="HttpClient"/> reports as a <see cref="TaskCanceledException"/>. Only the collector's own token
    /// being cancelled ends the loop.
    /// </summary>
    private async Task PollAsync(Box box, CancellationToken token)
    {
        var start = DateTime.UtcNow;
        FritzBoxDeviceList list;
        int changed;

        try
        {
            list = await box.Service.GetDevices(token).ConfigureAwait(false);
            changed = Publish(box, list.Devices);
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            box.Failures.Log(logger, ex, "read the smart home", FritzBox, box.Connection);
            return;
        }

        box.Failures.LogRecovery(logger, "read the smart home", FritzBox, box.Connection);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Fritz!Box {WebConnection} read in {Duration:N0} ms, {Changed} of {Count} devices changed", box.Connection, (DateTime.UtcNow - start).TotalMilliseconds, changed, list.Devices.Count);
        }
    }

    /// <summary>Publishes what is new or has changed and withdraws what the box no longer has.</summary>
    /// <returns>How many devices were published.</returns>
    private int Publish(Box box, IEnumerable<FritzBoxDevice> devices)
    {
        var seen = new HashSet<string>();
        var changed = 0;

        foreach (var device in devices)
        {
            var id = ((IHaveUniqueId)device).Id;

            if (!seen.Add(id))
            {
                continue;
            }

            var json = JsonSerializer.Serialize(device);

            if (box.Published.TryGetValue(id, out var publishedJson) && publishedJson == json && dataControlService.Entities.TryGetValue(id, out var managed) && managed.Device is FritzBoxDevice published)
            {
                published.CopyFrom(device);
                continue;
            }

            if (!device.IsPresent && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No DECT connection to {DisplayName} ({SerialNumber})", device.DisplayName, ((IHaveUniqueId)device).SerialNumber);
            }

            box.Published[id] = json;
            dataControlService.AddOrUpdate(id, new ManagedDevice(device, box.Connection, typeof(IFritzBoxService)));
            changed++;
        }

        foreach (var vanished in box.Published.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            box.Published.Remove(vanished);
            dataControlService.Remove(vanished);
        }

        return changed;
    }

    /// <summary>One box: its service, what was last published of it, and the request to read it now.</summary>
    private sealed class Box : IDisposable
    {
        public Box(WebConnection connection, IFritzBoxService service)
        {
            Connection = connection;
            Service = service;
            Service.CommandSent += OnCommandSent;
        }

        public WebConnection Connection { get; }
        public IFritzBoxService Service { get; }
        public ReadNowRequest ReadNow { get; } = new();
        public RepeatedFailure Failures { get; } = new(reminderInterval);

        /// <summary>The JSON last published, by device id. Only the poll loop of this box touches it.</summary>
        public Dictionary<string, string> Published { get; } = [];

        public Task Task { get; set; } = Task.CompletedTask;

        public void Dispose()
        {
            Service.CommandSent -= OnCommandSent;
            ReadNow.Dispose();
            (Service as IDisposable)?.Dispose();
        }

        private void OnCommandSent(object? sender, EventArgs e) => ReadNow.Request();
    }
}
