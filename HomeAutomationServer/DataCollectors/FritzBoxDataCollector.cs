using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServer.DataCollectors;

internal class FritzBoxDataCollector : IDataCollector
{
    private readonly Settings settings;
    private readonly ILogger<FritzBoxDataCollector> logger;
    private readonly SunSpecMeterService sunSpecMeterService;
    private readonly IList<IFritzBoxService> fritzBoxServices = new List<IFritzBoxService>();
    private CancellationTokenSource? tokenSource;
    private Thread? runner;

    public FritzBoxDataCollector(Settings settings, ILogger<FritzBoxDataCollector> logger, SunSpecMeterService sunSpecMeterService)
    {
        this.settings = settings;
        this.logger = logger;
        this.sunSpecMeterService = sunSpecMeterService;
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token);
        tokenSource = new CancellationTokenSource();

        foreach (var webConnection in settings.FritzBoxConnections)
        {
            var service = IoC.Get<IFritzBoxService>();
            service.Connection = webConnection;
            logger.LogInformation($"Adding Fritz!Box at {webConnection.BaseUrl} with user name {webConnection.UserName}");
            fritzBoxServices.Add(service);
        }

        runner = new(RunLoop);
        runner.Start();
    }

    public Task StopAsync(CancellationToken token = default) => Task.Run(() =>
    {
        tokenSource?.Cancel();

        if (runner != null)
        {
            logger.LogInformation("Waiting for Fritz!Box thread to stop");

            if (!runner.Join(TimeSpan.FromSeconds(5)))
            {
                throw new InvalidOperationException("Unable to gracefully end Fritz!Box thread");
            }

            runner = null;
        }

        tokenSource?.Dispose();
        tokenSource = null;
    }, token);

    protected virtual async ValueTask DisposeAsyncCore()
    {
        try
        {
            await StopAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fritz!Box thread did not stop correctly");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private async void RunLoop()
    {
        var token = tokenSource!.Token;

        while (true)
        {
            try
            {
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                linkedTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                // ReSharper disable once AccessToDisposedClosure
                IReadOnlyList<(WebConnection Connection, Task<FritzBoxDeviceList> Task)> connectionTasks = fritzBoxServices.Select(service => (service.Connection!, service.GetFritzBoxDevices(linkedTokenSource.Token))).ToArray();
                var tasks = connectionTasks.Select(c => c.Task).ToArray();

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    connectionTasks.Where(ct => ct.Task.IsCanceled).Apply(ct => logger.LogWarning($"No response from Fritz!Box at {ct.Connection.BaseUrl}"));
                    token.ThrowIfCancellationRequested();
                }

                IReadOnlyList<IPowerMeter1P> powerMeters = tasks.Where(t => t.IsCompletedSuccessfully).SelectMany(t => t.Result.DevicesWithPowerMeter).ToArray();

                tasks
                    .Where(t => t is { IsFaulted: true, Exception: not null })
                    .SelectMany(t => t.Exception!.InnerExceptions)
                    .Apply(ex => logger.LogError(ex, ex.Message));

                settings.ModbusMappings.Apply(mapping => UpdateServer(powerMeters, mapping));
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("The Fritz!Box thread has ended");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(60), token).ConfigureAwait(true);
        }
    }

    //TODO: Modbus mapping must be done at the server to prevent address conflict
    private void UpdateServer(IReadOnlyList<IPowerMeter1P> powerMeters, ModbusMapping mapping)
    {
        var powerMeter = powerMeters.SingleOrDefault(p => string.Equals(p.SerialNumber, mapping.SerialNumber, StringComparison.Ordinal));

        if (powerMeter == null)
        {
            logger?.LogWarning($"Power meter '{mapping.SerialNumber}' at modbus address {mapping.ModbusAddress} cannot be updated");
            return;
        }

        sunSpecMeterService.UpdateMeter(powerMeter, mapping.ModbusAddress);
    }
}