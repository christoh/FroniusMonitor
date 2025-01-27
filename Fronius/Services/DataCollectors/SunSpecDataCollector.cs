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

        SunSpecGroupBase? device = null;
        var isCancelled = false;
        var start = DateTime.UtcNow;

        try
        {
            IEnumerable<SunSpecModelBase> group;

            try
            {
                group = await client.GetDataAsync(tokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                client.Dispose();
                client = IoC.Get<ISunSpecClient>();
                logger.LogError(ex, "Could not retrieve data from {X}", connection);

                try
                {
                    tokenSource.Token.ThrowIfCancellationRequested();
                    await client.ConnectAsync(connection.HostName, connection.Port, connection.ModbusAddress, TimeSpan.FromSeconds(1.5)).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    client.Dispose();
                    client = IoC.Get<ISunSpecClient>();
                    logger.LogError(ex2, "Could not connect to {ModbusConnection}", connection);
                }

                return;
            }


            if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is >= 101 and <= 104 or >= 111 and <= 114))
            {
                device = new SunSpecInverter(group);
            }
            else if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is >= 201 and <= 204 or >= 211 and <= 214))
            {
                device = new SunSpecMeter(group);
            }
            else
            {
                logger.LogWarning("Unknown device type received");
            }

            if (device != null)
            {
                dataControlService.AddOrUpdate(new ManagedDevice(device, connection, typeof(ISunSpecClient)));
                logger.LogDebug("{DeviceType} {DeviceName} updated in {Duration:N0} ms", nameof(SunSpecInverter), device, (DateTime.UtcNow - start).TotalMilliseconds);
            }
        }
        catch (OperationCanceledException)
        {
            isCancelled = true;
            client.Dispose();

            if (device is IHaveUniqueId id)
            {
                dataControlService.Remove(id.Id);
            }

            logger.LogInformation("Updating {ModbusConnection} stopped", connection.DisplayName);
        }
        finally
        {
            var executionTime = (DateTime.UtcNow - start).TotalMilliseconds;
            var waitTime = Math.Max(0, Parameters.RefreshRate.TotalMilliseconds - executionTime);

            if (!isCancelled)
            {
                await Task.Delay((int)waitTime, tokenSource.Token).ConfigureAwait(false);
                _ = UpdateDevice(connection, client);
            }
        }
    }
}
