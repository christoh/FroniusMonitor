namespace De.Hochstaetter.Fronius.Services.Modbus;

public sealed class SunSpecClientService : IHomeAutomationRunner
{
    private readonly ILogger<SunSpecClientService> logger;
    private readonly IDataControlService dataControlService;
    private readonly IDictionary<ModbusConnection, ISunSpecClient> sunSpecClients = new ConcurrentDictionary<ModbusConnection, ISunSpecClient>();
    private readonly IOptionsMonitor<SunSpecClientParameters> options;
    private CancellationTokenSource? tokenSource;
    private Thread? runner;

    public SunSpecClientService
    (
        ILogger<SunSpecClientService> logger,
        IDataControlService dataControlService,
        IOptionsMonitor<SunSpecClientParameters> options
    )
    {
        this.logger = logger;
        this.dataControlService = dataControlService;
        this.options = options;
    }

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
        (runner = new Thread(Runner)).Start();
    }

    public Task StopAsync(CancellationToken token = default) => Task.Run(() =>
    {
        tokenSource?.Cancel();

        try
        {
            runner?.Join(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not stop the runner thread");
        }

        tokenSource?.Dispose();
        tokenSource = null;
        sunSpecClients.Values.Apply(client => client.Dispose());
        sunSpecClients.Clear();
    }, token);

    private async void Runner()
    {
        try
        {
            while (true)
            {
                tokenSource!.Token.ThrowIfCancellationRequested();
                var start = DateTime.UtcNow;
                await Parallel.ForEachAsync(sunSpecClients, tokenSource.Token, UpdateDevice);
                var executionTime = (DateTime.UtcNow - start).TotalMilliseconds;
                var waitTime = Math.Max(0, Parameters.RefreshRate.TotalMilliseconds - executionTime);
                await Task.Delay((int)waitTime, tokenSource.Token).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async ValueTask UpdateDevice(KeyValuePair<ModbusConnection, ISunSpecClient> entity, CancellationToken token)
    {
        IEnumerable<SunSpecModelBase> group;

        try
        {
            group = await entity.Value.GetDataAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            entity.Value.Dispose();
            logger.LogError(ex, "Could not retrieve data from {X}", entity.Key);

            try
            {
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
    }
}
