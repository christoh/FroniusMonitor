namespace De.Hochstaetter.Fronius.Services.DataCollectors;

public sealed class SunSpecDataCollector
(
    ILogger<SunSpecDataCollector> logger,
    IDataControlService dataControlService,
    IOptionsMonitor<SunSpecClientParameters> options
) : IHomeAutomationRunner, IAsyncDisposable
{
    private CancellationTokenSource? tokenSource;

    private SunSpecClientParameters Parameters => options.CurrentValue;

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            logger.LogError("Could not stop {ServiceName}", GetType().Name);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch
        {
            logger.LogError("Could not stop {ServiceName}", GetType().Name);
        }
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        tokenSource = new();

        foreach (var connection in Parameters.ModbusConnections as IEnumerable<ModbusConnection> ?? Array.Empty<ModbusConnection>())
        {
            var client = IoC.Get<ISunSpecClient>();

            try
            {
                logger.LogInformation("Connecting to SunSpec device {ModbusConnection}", connection);
                await client.ConnectAsync(connection.HostName, connection.Port, connection.ModbusAddress).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not connect to {ModbusConnection}", connection);
            }

            _ = UpdateDevice(connection, client);
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        logger.LogInformation("Stopping {ServiceName}", GetType().Name);

        if (tokenSource != null)
        {
            await tokenSource.CancelAsync().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2), default(CancellationToken)).ConfigureAwait(false);
        }

        tokenSource?.Dispose();
        tokenSource = null;
    }

    private async Task UpdateDevice(ModbusConnection connection, ISunSpecClient client)
    {
        if (tokenSource == null)
        {
            return;
        }
        
        try
        {
            var start = DateTime.UtcNow;
            IEnumerable<SunSpecModelBase> group;

            try
            {
                group = await client.GetDataAsync(tokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                client.Dispose();
                logger.LogError(ex, "Could not retrieve data from {X}", connection);

                try
                {
                    tokenSource.Token.ThrowIfCancellationRequested();
                    await client.ConnectAsync(connection.HostName, connection.Port, connection.ModbusAddress, TimeSpan.FromSeconds(1.5));
                }
                catch (Exception ex2)
                {
                    logger.LogError(ex2, "Could not connect to {ModbusConnection}", connection);
                }

                return;
            }

            if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is >= 101 and <= 104 or >= 111 and <= 114))
            {
                dataControlService.AddOrUpdate(connection.DisplayName, new SunSpecInverter(group));
            }

            if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is >= 201 and <= 204 or >= 211 and <= 214))
            {
                dataControlService.AddOrUpdate(connection.DisplayName, new SunSpecMeter(group));
            }

            var executionTime = (DateTime.UtcNow - start).TotalMilliseconds;
            var waitTime = Math.Max(0, Parameters.RefreshRate.TotalMilliseconds - executionTime);
            await Task.Delay((int)waitTime, tokenSource.Token).ConfigureAwait(false);
            _ = UpdateDevice(connection, client);
        }
        catch (OperationCanceledException)
        {
            client.Dispose();
            dataControlService.Remove(connection.DisplayName);
            logger.LogInformation("Updating {ModbusConnection} stopped", connection.DisplayName);
        }
    }
}
