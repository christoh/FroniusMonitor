using De.Hochstaetter.Fronius.Models.Events;
using Formatting = Newtonsoft.Json.Formatting;

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
        logger.LogDebug("Wattpilot: {Token}", e.JObject.ToString(Formatting.None));

        if (sender is not IWattPilotService service)
        {
            throw new InvalidCastException($"{nameof(sender)} must be {nameof(IWattPilotService)}");
        }

        services[service].LastMessageReceived = DateTime.UtcNow;
        dataControlService.AddOrUpdate(((IHaveUniqueId)e.WattPilot).Id, new ManagedDevice(e.WattPilot, services[service].Connection, typeof(IWattPilotService)));
    }

    private async ValueTask StartServiceAsync(IWattPilotService service)
    {
        try
        {
            var start = DateTime.UtcNow;
            var connection = services[service].Connection;
            await service.StartAsync(connection).ConfigureAwait(false);
            services[service] = new(connection);
            logger.LogInformation("Connection to WattPilot '{WattPilot}' established in {Duration:N0} ms", service.WattPilot?.DisplayName, (DateTime.UtcNow - start).Milliseconds);
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

            var serviceState = new ServiceState(services[service].Connection)
            {
                LastMessageReceived = services[service].LastMessageReceived,
            };

            services[service] = serviceState;
            await StartServiceAsync(service);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to re-establish connection to '{WattPilot}'", e.WattPilot?.DisplayName);
        }
    }

    private async void TimerElapsed(object? state)
    {
        try
        {
            foreach (var keyValuePair in services.Where(s => DateTime.UtcNow - s.Value.LastMessageReceived > TimeSpan.FromSeconds(15)))
            {
                await StartServiceAsync(keyValuePair.Key).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to re-establish connection to at least one WattPilot");
        }
    }
}
