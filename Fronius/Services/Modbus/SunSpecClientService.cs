namespace De.Hochstaetter.Fronius.Services.Modbus;

public sealed class SunSpecClientService
(
    ILogger<SunSpecClientService> logger,
    IDataControlService dataControlService,
    IOptionsMonitor<SunSpecClientParameters> options
) : IHomeAutomationRunner
{
    private readonly ConcurrentDictionary<ModbusConnection, ISunSpecClient> sunSpecClients = new();
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

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);

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

            sunSpecClients[connection] = client;
        }

        tokenSource = new();
        //(runner = new Thread(Runner)).Start();
        foreach (var client in sunSpecClients)
        {
            _ = UpdateDevice(client, tokenSource.Token);
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        tokenSource?.Cancel();
        await Task.Delay(TimeSpan.FromSeconds(2), default(CancellationToken)).ConfigureAwait(false);
        tokenSource?.Dispose();
        tokenSource = null;
        sunSpecClients.Clear();
    }

    private async Task UpdateDevice(KeyValuePair<ModbusConnection, ISunSpecClient> entity, CancellationToken token)
    {
        try
        {
            var start = DateTime.UtcNow;
            IEnumerable<SunSpecModelBase> group;

            try
            {
                group = await entity.Value.GetDataAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entity.Value.Dispose();
                logger.LogError(ex, "Could not retrieve data from {X}", entity.Key);

                try
                {
                    token.ThrowIfCancellationRequested();
                    await entity.Value.ConnectAsync(entity.Key.HostName, entity.Key.Port, entity.Key.ModbusAddress, TimeSpan.FromSeconds(1.5));
                }
                catch (Exception ex2)
                {
                    logger.LogError(ex2, "Could not connect to {ModbusConnection}", entity.Key);
                }

                return;
            }

            if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is (>= 101 and <= 104) or (>= 111 and <= 114)))
            {
                dataControlService.AddOrUpdate(entity.Key.DisplayName, new SunSpecInverter(group));
            }

            if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is (>= 201 and <= 204) or (>= 211 and <= 214)))
            {
                dataControlService.AddOrUpdate(entity.Key.DisplayName, new SunSpecMeter(group));
            }

            var executionTime = (DateTime.UtcNow - start).TotalMilliseconds;
            var waitTime = Math.Max(0, Parameters.RefreshRate.TotalMilliseconds - executionTime);
            await Task.Delay((int)waitTime, token).ConfigureAwait(false);
            _ = UpdateDevice(entity, token);
        }
        catch (OperationCanceledException)
        {
            entity.Value.Dispose();
            dataControlService.Remove(entity.Key.DisplayName);
            logger.LogInformation("Updating {ModbusConnection} stopped", entity.Key.DisplayName);
        }
    }
}
