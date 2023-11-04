using De.Hochstaetter.Fronius.Services.DataCollectors;

namespace De.Hochstaetter.Fronius.Services.Modbus;

public sealed class SunSpecClientService : IHomeAutomationRunner
{

    private readonly ILogger<SunSpecClientService> logger;
    private readonly IDataControlService dataControlService;
    private readonly IDictionary<ModbusConnection, SunSpecClient> sunSpecClients = new ConcurrentDictionary<ModbusConnection, SunSpecClient>();
    private readonly IOptionsMonitor<SunSpecClientParameters> options;
    private IDictionary<string, SunSpecModelBase>? currentDevices;
    private CancellationTokenSource tokenSource=new();
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

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            //
        }
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        (runner = new Thread(Runner)).Start();

    }

    public Task StopAsync(CancellationToken token = default) => Task.Run(() =>
    {
        tokenSource.Cancel();

        try
        {
            runner?.Join(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not stop the runner thread");
        }

        tokenSource.Dispose();
        tokenSource = new();
        sunSpecClients?.Values.Apply(client => client.Dispose());

    }, token);

    private async void Runner()
    {
        try
        {
            while (true)
            {
                tokenSource.Token.ThrowIfCancellationRequested();

                await foreach (var device in GetDevices())
                {
                    dataControlService.AddOrUpdate(device.Key.DisplayName, device.Value);
                    tokenSource.Token.ThrowIfCancellationRequested();
                }

                await Task.Delay(options.CurrentValue.RefreshRate, tokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            
        }
    }

    private async IAsyncEnumerable<KeyValuePair<ModbusConnection, SunSpecGroupBase>> GetDevices()
    {
        foreach (var entity in sunSpecClients.ToList())
        {
            IEnumerable<SunSpecModelBase> group;

            try
            {
                group = await entity.Value.GetDataAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                sunSpecClients.Remove(entity);
                entity.Value.Dispose();
                logger.LogError(ex, "Could not retrieve data from {X}", entity.Key);
                continue;
            }

            if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is (>= 101 and <= 104) or (>= 111 and <= 114)))
            {
                yield return new KeyValuePair<ModbusConnection, SunSpecGroupBase>(entity.Key, new SunSpecInverter(group));
            }

            if (group.Select(g => g.ModelNumber).Any(modelNumber => modelNumber is (>= 201 and <= 204) or (>= 211 and <= 214)))
            {
                yield return new KeyValuePair<ModbusConnection, SunSpecGroupBase>(entity.Key, new SunSpecMeter(group));
            }
        }
    }
}