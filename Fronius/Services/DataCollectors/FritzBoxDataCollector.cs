namespace De.Hochstaetter.Fronius.Services.DataCollectors;

public class FritzBoxDataCollector : IDataCollector
{
    private readonly ILogger<FritzBoxDataCollector> logger;
    private readonly IDataControlService dataControlService;
    private readonly IList<IFritzBoxService> fritzBoxServices = new List<IFritzBoxService>();
    private readonly IOptionsMonitor<FritzBoxParameters> options;
    private IDictionary<string, IPowerConsumer1P>? currentDevices;
    private CancellationTokenSource? tokenSource;
    private Thread? runner;

    public FritzBoxDataCollector(ILogger<FritzBoxDataCollector> logger, IDataControlService dataControlService, IOptionsMonitor<FritzBoxParameters> options)
    {
        this.logger = logger;
        this.dataControlService = dataControlService;
        this.options = options;
    }

    private FritzBoxParameters Parameters => options.CurrentValue;
    private IEnumerable<WebConnection> Connections => Parameters.Connections ?? throw new ArgumentNullException(nameof(options), @$"{nameof(options)} are not configured");

    
    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);
        tokenSource = new CancellationTokenSource();

        foreach (var webConnection in Connections)
        {
            var fritzBoxService = IoC.Get<IFritzBoxService>();
            fritzBoxService.Connection = webConnection;
            logger.LogInformation("Adding Fritz!Box at {Url} with user name {UserName}",webConnection.BaseUrl, webConnection.UserName);
            fritzBoxServices.Add(fritzBoxService);
        }

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

    protected virtual async ValueTask DisposeAsyncCore()
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
                IReadOnlyList<(WebConnection Connection, Task<FritzBoxDeviceList> Task)> connectionTasks = 
                    fritzBoxServices.Select(service => (service.Connection!, service.GetFritzBoxDevices(linkedTokenSource.Token))).ToArray();
                
                var tasks = connectionTasks.Select(c => c.Task).ToArray();

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    connectionTasks.Where(ct => ct.Task.IsCanceled).Apply(ct => logger.LogWarning("No response from Fritz!Box at {Url}",ct.Connection.BaseUrl));
                    token.ThrowIfCancellationRequested();
                }

                tasks
                    .Where(t => t is { IsFaulted: true, Exception: not null })
                    .SelectMany(t => t.Exception!.InnerExceptions)
                    .Apply(ex => logger.LogError(ex, "{Exception}", ex.Message));

                IReadOnlyList<IPowerConsumer1P> fritzBoxDevices = tasks
                    .Where(t => t.IsCompletedSuccessfully)
                    .SelectMany(t => t.Result.Devices)
                    .Cast<IPowerConsumer1P>()
                    .Where(p => p is { IsPresent: true }).ToArray();

                if (currentDevices != null)
                {
                    var devicesToDelete = currentDevices.Keys.Where(k => !fritzBoxDevices.Select(f => f.Id).Contains(k));
                }

                currentDevices = fritzBoxDevices.ToDictionary(f => f.Id);
                await dataControlService.AddOrUpdateAsync(fritzBoxDevices, token).ConfigureAwait(true);
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

            await Task.Delay(Parameters.RefreshRate, token).ConfigureAwait(true);
        }
    }
}
