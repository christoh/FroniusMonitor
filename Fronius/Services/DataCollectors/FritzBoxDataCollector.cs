namespace De.Hochstaetter.Fronius.Services.DataCollectors;

public class FritzBoxDataCollector : IHomeAutomationRunner
{
    private readonly ILogger<FritzBoxDataCollector> logger;
    private readonly IDataControlService dataControlService;
    private IReadOnlyList<IFritzBoxService> fritzBoxServices = null!;
    private readonly IOptionsMonitor<FritzBoxDataCollectorParameters> options;
    private IDictionary<string, IPowerConsumer1P>? currentDevices;
    private CancellationTokenSource? tokenSource;
    private Thread? runner;

    public FritzBoxDataCollector(ILogger<FritzBoxDataCollector> logger, IDataControlService dataControlService, IOptionsMonitor<FritzBoxDataCollectorParameters> options)
    {
        this.logger = logger;
        this.dataControlService = dataControlService;
        this.options = options;
    }

    private FritzBoxDataCollectorParameters DataCollectorParameters => options.CurrentValue;
    private IEnumerable<WebConnection> Connections => DataCollectorParameters.Connections ?? throw new ArgumentNullException(nameof(options), @$"{nameof(options)} are not configured");


    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        tokenSource = new CancellationTokenSource();

        fritzBoxServices = Connections.Select(c =>
        {
            var result = IoC.Get<IFritzBoxService>();
            result.Connection = c;
            logger.LogInformation("Adding Fritz!Box at {Url} with user name {UserName}", c.BaseUrl, c.UserName);
            return result;
        }).ToArray();

        (runner = new(RunLoop)).Start();
    }

    public Task StopAsync(CancellationToken token = default) => Task.Run(() =>
    {
        tokenSource?.Cancel();

        if (runner != null)
        {
            logger.LogInformation("Waiting for Fritz!Box thread to stop");

            if (!runner.Join(TimeSpan.FromSeconds(15)))
            {
                throw new InvalidOperationException("Unable to gracefully end Fritz!Box thread");
            }

            runner = null;
        }

        tokenSource?.Dispose();
        tokenSource = null;
    }, token);

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
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
    }

    public void Dispose()
    {
        Dispose(true);
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
                IReadOnlyList<(WebConnection Connection, Task<FritzBoxDeviceList> Task)> connectionTasks =
                    fritzBoxServices.Select(service => (service.Connection!, service.GetFritzBoxDevices(linkedTokenSource.Token))).ToArray();

                var tasks = connectionTasks.Select(c => c.Task).ToArray();

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    connectionTasks.Where(ct => ct.Task.IsCanceled).Apply(ct => logger.LogWarning("No response from Fritz!Box at {Url}", ct.Connection.BaseUrl));
                    token.ThrowIfCancellationRequested();
                }

                tasks
                    .Where(t => t is { IsFaulted: true, Exception: not null })
                    .SelectMany(t => t.Exception!.InnerExceptions)
                    .Apply(ex => logger.LogError(ex, "{Exception}", ex.Message));

                var allFritzBoxDevices = tasks
                    .Where(t => t.IsCompletedSuccessfully)
                    .SelectMany(t => t.Result.Devices)
                    .Cast<IPowerConsumer1P>()
                    .ToList();

                var presentFritzBoxDevices = allFritzBoxDevices
                    .Where(p => p is { IsPresent: true })
                    .ToDictionary(f => f.Id);

                allFritzBoxDevices
                    .Where(p => p is { IsPresent: false })
                    .Apply(p => logger.LogDebug("No DECT connection to {DisplayName} ({SerialNumber})", p.DisplayName, p.SerialNumber));

                //if (currentDevices != null)
                //{
                //    var devicesToDelete = currentDevices.Keys.Where(k => !presentFritzBoxDevices.ContainsKey(k));
                //    await dataControlService.RemoveAsync(devicesToDelete, token).ConfigureAwait(true);
                //}

                currentDevices = presentFritzBoxDevices;
                await dataControlService.AddOrUpdateAsync(presentFritzBoxDevices.Values, token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("The Fritz!Box thread has ended");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Exception}", ex.Message);
            }

            await Task.Delay(DataCollectorParameters.RefreshRate, token).ConfigureAwait(true);
        }
    }
}
