namespace De.Hochstaetter.Fronius.Services.DataCollectors;

public sealed class FritzBoxDataCollector
(
    ILogger<FritzBoxDataCollector> logger,
    IDataControlService dataControlService,
    IOptionsMonitor<FritzBoxDataCollectorParameters> options
) : IHomeAutomationRunner, IAsyncDisposable
{
    private CancellationTokenSource? tokenSource;

    private FritzBoxDataCollectorParameters Parameters => options.CurrentValue;
    private IEnumerable<WebConnection> Connections => Parameters.Connections ?? throw new ArgumentNullException(nameof(options), @$"{nameof(options)} are not configured");
    private readonly ConcurrentDictionary<WebConnection, Task> runningTasks = new();

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        tokenSource = new CancellationTokenSource();

        foreach (var connection in Connections)
        {
            var service = IoC.Get<IFritzBoxService>();
            service.Connection = connection;
            logger.LogInformation("Adding Fritz!Box at {Url} with user name {UserName}", connection.BaseUrl, connection.UserName);
            runningTasks[connection] = Update(service);
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (tokenSource != null)
        {
            await tokenSource.CancelAsync().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(0.2), token).ConfigureAwait(false);
            await Task.WhenAll(runningTasks.Values).ConfigureAwait(false);
        }

        var devices = dataControlService.Entities.Where(e => e.Value is FritzBoxDevice).Select(e => e.Key).ToList();
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

    private async Task Update(IFritzBoxService service)
    {
        var startTime = DateTime.UtcNow;

        if (tokenSource == null)
        {
            return;
        }

        try
        {
            var token = tokenSource?.Token ?? throw new OperationCanceledException();
            
            try
            {
                var allFritzBoxDevices = (await service.GetFritzBoxDevices(token).ConfigureAwait(false)).Devices.Cast<IPowerMeter1P>().ToList();

                var presentFritzBoxDevices = allFritzBoxDevices.Where(p => p is { IsPresent: true });

                allFritzBoxDevices
                    .Where(p => p is { IsPresent: false })
                    .Apply(p => logger.LogDebug("No DECT connection to {DisplayName} ({SerialNumber})", p.DisplayName, p.SerialNumber));

                await dataControlService.AddOrUpdateAsync(presentFritzBoxDevices, token).ConfigureAwait(false);
                logger.LogDebug("FritzBox {WebConnection} updated in {Duration:N0} ms", service.Connection, (DateTime.UtcNow - startTime).Milliseconds);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "{Exception}", ex.Message);
            }

            var duration = DateTime.UtcNow - startTime;
            await Task.Delay(Math.Max(0, (int)(Parameters.RefreshRate - duration).TotalMilliseconds), token).ConfigureAwait(false);
            runningTasks[service.Connection!] = Update(service);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Stopped Fritz!Box {WebConnection}", service.Connection);
        }
    }
}
