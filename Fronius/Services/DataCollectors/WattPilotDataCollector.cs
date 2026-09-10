namespace De.Hochstaetter.Fronius.Services.DataCollectors;

public sealed class WattPilotDataCollector(
    ILogger<WattPilotDataCollector> logger,
    IOptionsMonitor<WattPilotParameters> options,
    IDataControlService dataControlService
) : IHomeAutomationRunner, IAsyncDisposable
{
    private record ServiceState(WebConnection Connection)
    {
        public DateTime LastMessageReceived { get; set; } = DateTime.UtcNow;
    }

    private readonly ConcurrentDictionary<IWattPilotService, ServiceState> services = new ConcurrentDictionary<IWattPilotService, ServiceState>();
    private Timer? timer;

#pragma warning disable CA1816
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
#pragma warning restore CA1816

    ~WattPilotDataCollector() => Dispose();

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        timer = new Timer(TimerElapsed, null, 15000, 15000);

        foreach (var connection in options.CurrentValue.Connections ?? [])
        {
            var service = IoC.Get<IWattPilotService>();
            service.OnUpdate += OnUpdate;
            service.OnLostConnection += OnLostConnection;
            services[service] = new(connection);
            await StartServiceAsync(service).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (timer != null)
        {
            await timer.DisposeAsync();
        }

        foreach (var service in services.Keys)
        {
            service.OnUpdate -= OnUpdate;
            service.OnLostConnection -= OnLostConnection;
            await service.StopAsync().ConfigureAwait(false);
        }
    }

    private void OnUpdate(object? sender, WattPilotUpdateEventArgs e)
    {
        if (sender is not IWattPilotService service)
        {
            logger.LogError("Sender must be {Sender}", nameof(IWattPilotService));
            return;
        }

        try
        {
            var jsonMessage = e.JsonObject.ToJsonString();
            logger.LogDebug("Wattpilot '{WattPilot}': {Token}", e.WattPilot.DisplayName, jsonMessage);
            services[service].LastMessageReceived = DateTime.UtcNow;
            var updateMessage = new WattPilotUpdate(e.WattPilot.SerialNumber ?? string.Empty, jsonMessage);
            dataControlService.AddOrUpdate(((IHaveUniqueId)e.WattPilot).Id, new ManagedDevice(e.WattPilot, services[service].Connection, typeof(IWattPilotService), true));
            dataControlService.AddOrUpdate(((IHaveUniqueId)updateMessage).Id, new ManagedDevice(updateMessage, services[service].Connection, typeof(IWattPilotService)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to update WattPilot {WattPilot}", service.WattPilot?.DisplayName);
        }
    }

    private async ValueTask StartServiceAsync(IWattPilotService service)
    {
        try
        {
            var start = DateTime.UtcNow;
            var connection = services[service].Connection;

            // The attempt itself counts as activity: the handshake may take up to its own ten second timeout, and
            // the watchdog must not take a connection that is still being set up for one that has gone quiet.
            services[service] = new(connection);
            await service.StartAsync(connection).ConfigureAwait(false);
            logger.LogInformation("Connection to WattPilot '{WattPilot}' established in {Duration:N0} ms", service.WattPilot?.DisplayName, (DateTime.UtcNow - start).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to start WattPilot {WattPilot}", service.WattPilot?.DisplayName);
        }
    }

    private async void OnLostConnection(object? sender, WattPilotServiceStoppedEventArgs e)
    {
        try
        {
            logger.LogWarning("Connection to WattPilot '{WattPilot}' lost", e.WattPilot?.DisplayName);

            if (sender is not IWattPilotService service)
            {
                throw new InvalidCastException($"{nameof(sender)} must be {nameof(IWattPilotService)}");
            }

            await StartServiceAsync(service);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to re-establish connection to '{WattPilot}'", e.WattPilot?.DisplayName);
        }
    }

    /// <summary>
    /// The watchdog: a WebSocket that has gone half open delivers nothing and raises nothing, so a charger that
    /// has said nothing for 15 seconds is taken to be gone and connected again. StartAsync ends a connection it
    /// still has without raising OnLostConnection, so this is one restart and not two.
    /// </summary>
    private async void TimerElapsed(object? state)
    {
        try
        {
            foreach (var service in services.Where(s => DateTime.UtcNow - s.Value.LastMessageReceived > TimeSpan.FromSeconds(15)).Select(s => s.Key))
            {
                if (service.Connection is not null)
                {
                    logger.LogWarning("No message from WattPilot '{WattPilot}' for 15 seconds, reconnecting", service.WattPilot?.DisplayName);
                }

                await StartServiceAsync(service).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to re-establish connection to at least one WattPilot");
        }
    }
}
