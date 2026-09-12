namespace De.Hochstaetter.Fronius.Services.DataCollectors;

/// <summary>
///     Runs the one <see cref="IToshibaHvacService" /> of the server and publishes every air conditioner of the
///     account to <see cref="IDataControlService" />: once when the connection comes up, again whenever the realtime
///     channel delivers new state for a device, and once more for all of them on every read of the device list.
/// </summary>
/// <remarks>
///     <para>
///         The devices are published with <c>SupportsPushMessages</c> false, so <c>SignalRDispatcher</c> broadcasts
///         the whole <see cref="ToshibaHvacMappingDevice" /> on every change. That is a few hundred bytes a few times
///         a minute per device - nothing like a Wattpilot's delta stream - so no separate update message is needed.
///     </para>
///     <para>
///         One timer does two jobs: it reads the device list again at <see cref="ToshibaHvacDataCollectorParameters.MappingRefreshRate" />,
///         because the realtime channel loses the odd message and the HTTPS side has the full state; and it is the retry
///         for a connection that failed to come up or that the service could not reopen, at a much shorter interval.
///     </para>
/// </remarks>
public sealed class ToshibaHvacDataCollector(
    ILogger<ToshibaHvacDataCollector> logger,
    IOptionsMonitor<ToshibaHvacDataCollectorParameters> options,
    IDataControlService dataControlService,
    IToshibaHvacService service
) : IHomeAutomationRunner, IAsyncDisposable
{
    /// <summary>How soon a connection that is down is tried again.</summary>
    private static readonly TimeSpan retryInterval = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim timerSemaphore = new(1, 1);
    private Timer? timer;
    private bool isStarted;

    private ToshibaHvacDataCollectorParameters Parameters => options.CurrentValue;

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Could not properly stop {ServiceName}", GetType().Name);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Could not properly stop {ServiceName}", GetType().Name);
            }
        }
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);

        if (Parameters.Connection is not { UserName.Length: > 0 })
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No Toshiba HVAC account is configured");
            }

            return;
        }

        isStarted = true;
        service.DeviceUpdated += OnDeviceUpdated;
        service.ConnectionLost += OnConnectionLost;
        timer = new Timer(TimerElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        await ConnectAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        isStarted = false;
        service.DeviceUpdated -= OnDeviceUpdated;
        service.ConnectionLost -= OnConnectionLost;

        if (timer != null)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
            timer = null;
        }

        await service.Stop().ConfigureAwait(false);
        var devices = dataControlService.Entities.Where(e => e.Value.Device is ToshibaHvacDeviceBase).Select(e => e.Key).ToList();
        await dataControlService.RemoveAsync(devices, token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Starts the service and publishes what it found. On failure the timer is set to try again soon; on success
    ///     it is set to the mapping refresh rate.
    /// </summary>
    private async ValueTask ConnectAsync()
    {
        var start = DateTime.UtcNow;
        var parameters = Parameters;
        await service.Start(parameters.Connection, parameters.AzureDeviceId).ConfigureAwait(false);

        if (!service.IsRunning)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("The Toshiba HVAC service did not start, trying again in {Retry:N0} s", retryInterval.TotalSeconds);
            }

            ScheduleNext(retryInterval);
            return;
        }

        var devices = service.AllDevices?.SelectMany(m => m.Devices).ToList() ?? [];
        Publish(devices);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Toshiba HVAC service started with {Count} device(s) in {Duration:N0} ms", devices.Count, (DateTime.UtcNow - start).TotalMilliseconds);
        }

        ScheduleNext(parameters.MappingRefreshRate);
    }

    /// <summary>Publishes every device it is given and withdraws the ones the account no longer has.</summary>
    private void Publish(IReadOnlyCollection<ToshibaHvacMappingDevice> devices)
    {
        var credentials = Parameters.Connection;
        var currentIds = new HashSet<string>();

        foreach (var device in devices)
        {
            var id = ((IHaveUniqueId)device).Id;
            currentIds.Add(id);
            dataControlService.AddOrUpdate(id, new ManagedDevice(device, credentials, typeof(IToshibaHvacService)));
        }

        foreach (var vanished in dataControlService.Entities.Where(e => e.Value.Device is ToshibaHvacDeviceBase && !currentIds.Contains(e.Key)).Select(e => e.Key).ToList())
        {
            dataControlService.Remove(vanished);
        }
    }

    private void OnDeviceUpdated(object? sender, ToshibaHvacDeviceUpdatedEventArgs e)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Toshiba HVAC '{Device}' sent {Command}: {State}", e.Device.DisplayName, e.Command.CommandName, e.Device.State);
            }

            dataControlService.AddOrUpdate(((IHaveUniqueId)e.Device).Id, new ManagedDevice(e.Device, Parameters.Connection, typeof(IToshibaHvacService)));
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Unable to publish Toshiba HVAC '{Device}'", e.Device.DisplayName);
            }
        }
    }

    private void OnConnectionLost(object? sender, EventArgs e)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("The Toshiba realtime connection was lost, reconnecting");
            }

            ScheduleNext(TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            // The timer may be disposed by a concurrent StopAsync; an event handler has nobody to throw to.
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Unable to schedule the Toshiba HVAC reconnect");
            }
        }
    }

    private void ScheduleNext(TimeSpan due)
    {
        if (isStarted)
        {
            timer?.Change(due, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    ///     The timer is one shot and re-armed at the end, so a slow refresh never overlaps the next; the semaphore
    ///     covers a fire that arrives while <see cref="OnConnectionLost" /> has just pulled it forward.
    /// </summary>
    private async void TimerElapsed(object? state)
    {
        // Everything, the semaphore wait included, is inside the try: nothing may escape an async void. And the
        // semaphore is released only if it was taken - a second release of a SemaphoreSlim(1, 1) throws, in the
        // finally, which would be the very crash this guards against.
        var acquired = false;

        try
        {
            acquired = await timerSemaphore.WaitAsync(0).ConfigureAwait(false);

            if (!acquired || !isStarted)
            {
                return;
            }

            if (!service.IsRunning || !service.IsConnected)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("The Toshiba HVAC service is {State}, connecting again", service.IsRunning ? "running without a realtime connection" : "not running");
                }

                await ConnectAsync().ConfigureAwait(false);
                return;
            }

            var start = DateTime.UtcNow;
            var devices = await service.RefreshDevices().ConfigureAwait(false);
            Publish(devices);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Toshiba HVAC device list refreshed with {Count} device(s) in {Duration:N0} ms", devices.Count, (DateTime.UtcNow - start).TotalMilliseconds);
            }

            ScheduleNext(Parameters.MappingRefreshRate);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Unable to refresh the Toshiba HVAC devices, trying again in {Retry:N0} s", retryInterval.TotalSeconds);
            }

            ScheduleNext(retryInterval);
        }
        finally
        {
            if (acquired)
            {
                timerSemaphore.Release();
            }
        }
    }
}
