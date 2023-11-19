namespace De.Hochstaetter.Fronius.Services.DataCollectors;

public sealed class Gen24DataCollector
(
    ILogger<Gen24DataCollector> logger,
    IOptionsMonitor<Gen24DataCollectorParameters> options,
    IDataControlService dataControlService
) : IHomeAutomationRunner
{
    private CancellationTokenSource? tokenSource;
    private Gen24DataCollectorParameters Parameters => options.CurrentValue;
    private IEnumerable<WebConnection> Connections => Parameters.Connections ?? throw new ArgumentNullException(nameof(options), @$"{nameof(options)} are not configured");


    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not properly stop {ServiceName}", GetType().Name);
        }
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token);
        tokenSource = new CancellationTokenSource();

        foreach (var connection in Connections)
        {
            var gen24Service = IoC.Get<IGen24Service>();
            gen24Service.Connection = connection;
            var gen24System = new Gen24System { Service = gen24Service };
            var semaphore = new SemaphoreSlim(1, 1);
            logger.LogInformation("Adding Gen24 inverter {WebConnection}", connection);
            _ = UpdateConfig(gen24System, semaphore);
            _ = Update(gen24System, semaphore);
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (tokenSource != null)
        {
            await tokenSource.CancelAsync().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2), token);
        }

        tokenSource = null;
    }

    private async Task Update(Gen24System gen24System, SemaphoreSlim semaphore)
    {
        var start = DateTime.UtcNow;

        if (tokenSource == null)
        {
            return;
        }

        try
        {
            try
            {
                if (gen24System.Config is null)
                {
                    try
                    {
                        gen24System.Config = await ReadGen24Config(gen24System, semaphore, tokenSource.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogError(ex, "Could not read config for GEN24 inverter {BaseUri}", gen24System.Service.Connection);
                        return;
                    }
                }

                try
                {
                    if (gen24System.Config?.Components is not null)
                    {
                        gen24System.Sensors = await gen24System.Service.GetFroniusData(gen24System.Config.Components, tokenSource.Token).ConfigureAwait(false);
                        dataControlService.AddOrUpdate(gen24System.Service.Connection!.DisplayName, gen24System);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Could not read config for GEN24 inverter {BaseUri}", gen24System.Service.Connection);
                }
            }
            finally
            {
                if (!tokenSource.IsCancellationRequested)
                {
                    var duration = DateTime.UtcNow - start;
                    var waitTime = Math.Max(0, (Parameters.RefreshRate - duration).TotalMilliseconds);
                    await Task.Delay((int)(waitTime), tokenSource.Token).ConfigureAwait(false);
                    _ = Update(gen24System, semaphore);
                }
            }
        }
        catch (OperationCanceledException)
        {
            dataControlService.Remove(gen24System.Service.Connection!.DisplayName);
            logger.LogInformation("Updating {ModbusConnection} stopped", gen24System.Service.Connection);
            semaphore.Dispose();
        }
    }

    private async Task UpdateConfig(Gen24System gen24System, SemaphoreSlim semaphore)
    {
        if (tokenSource == null)
        {
            return;
        }

        try
        {
            await Task.Delay(Parameters.ConfigRefreshRate, tokenSource.Token).ConfigureAwait(false);

            try
            {
                gen24System.Config = await ReadGen24Config(gen24System, semaphore, tokenSource.Token).ConfigureAwait(false);

                if (gen24System.Config != null)
                {
                    dataControlService.AddOrUpdate(gen24System.Service.Connection!.DisplayName, gen24System);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Could not read config for GEN24 inverter {BaseUri}", gen24System.Service.Connection);
                return;
            }

            if (!tokenSource.IsCancellationRequested)
            {
                _ = UpdateConfig(gen24System, semaphore);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<Gen24Config?> ReadGen24Config(Gen24System gen24System, SemaphoreSlim semaphore, CancellationToken token)
    {
        if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(7), token).ConfigureAwait(false))
        {
            return null;
        }

        try
        {
            var versionsToken = (await gen24System.Service.GetFroniusJsonResponse("status/version", token: token).ConfigureAwait(false)).Token;
            var componentsToken = (await gen24System.Service.GetFroniusJsonResponse("components/", token: token).ConfigureAwait(false)).Token;
            var configToken = (await gen24System.Service.GetFroniusJsonResponse("config/", token: token).ConfigureAwait(false)).Token;

#if DEBUG
            // ReSharper disable UnusedVariable
            var configString = configToken.ToString();
            var versionString = versionsToken.ToString();
            var componentsString = componentsToken.ToString();
            // ReSharper restore UnusedVariable
#endif

            return Gen24Config.Parse(versionsToken, componentsToken, configToken);
        }
        finally
        {
            semaphore.Release();
        }
    }
}