namespace De.Hochstaetter.Fronius.Services.DataCollectors;

public sealed class Gen24DataCollector(
    ILogger<Gen24DataCollector> logger,
    IOptionsMonitor<Gen24DataCollectorParameters> options,
    IDataControlService dataControlService
) : IHomeAutomationRunner, IGen24ConfigRefresher, IAsyncDisposable
{
    /// <summary>
    /// The round of each loop that is currently in flight, one entry per inverter, so that
    /// <see cref="StopAsync"/> can wait for them.
    /// </summary>
    /// <remarks>
    /// Keyed by the <see cref="Gen24System"/> for the same reason as <see cref="configReadRequests"/>, and this
    /// is where it went wrong before: they were keyed by <see cref="WebConnection"/>, which does not compare by
    /// value, while <see cref="Update"/> puts a fresh clone of it in as the key on every round. So no round ever
    /// replaced the entry of the one before it - each one left another entry behind, holding a task that had
    /// finished, for as long as the server ran, and <see cref="StopAsync"/> then waited on all of them.
    /// </remarks>
    private readonly ConcurrentDictionary<Gen24System, Task> runningSensorTasks = new(), runningConfigTasks = new();

    /// <summary>
    /// One per inverter, so that a setting just written can be read back without waiting out
    /// <see cref="Gen24DataCollectorParameters.ConfigRefreshRate"/> - five minutes by default.
    /// </summary>
    /// <remarks>
    /// Keyed by the <see cref="Gen24System"/> and not by its <see cref="WebConnection"/>. The connection is
    /// replaced by a clone on every round of <see cref="Update"/> and <see cref="WebConnection"/> does not
    /// compare by value, so it is not the same key twice; the inverter is created once in
    /// <see cref="StartAsync"/> and lives as long as the collector runs.
    /// </remarks>
    private readonly ConcurrentDictionary<Gen24System, ReadNowRequest> configReadRequests = new();

    /// <summary>
    /// How long an inverter that keeps failing the same way stays out of the log before it is mentioned again.
    /// </summary>
    private static readonly TimeSpan reminderInterval = TimeSpan.FromHours(1);

    private CancellationTokenSource? tokenSource;
    private DateTime lastLogTime = DateTime.MinValue;
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

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
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
            configReadRequests[gen24System] = new ReadNowRequest();
            logger.LogInformation("Adding Gen24 inverter {WebConnection}", connection);

            // One per loop, handed down through the rounds the way the semaphore is, so that an inverter which is
            // simply switched off does not write the same stack trace on every one of them.
            runningSensorTasks[gen24System] = Update(gen24System, semaphore, new RepeatedFailure(reminderInterval));
            runningConfigTasks[gen24System] = UpdateConfig(gen24System, semaphore, new RepeatedFailure(reminderInterval));
        }
    }

    /// <inheritdoc />
    public void ReadConfigNow(string deviceId)
    {
        // The id is built from what the configuration itself says, so an inverter that has not been read yet has
        // none. That is the one case where there is nothing to ask for: the first read is already on its way.
        // Through the interface, because Id is a default implementation on it and not a member of the inverter.
        var inverter = configReadRequests.Keys.FirstOrDefault(candidate => ((IHaveUniqueId)candidate).Id == deviceId);

        if (inverter is null || !configReadRequests.TryGetValue(inverter, out var request))
        {
            logger.LogDebug("Asked to read the config of {DeviceId} now, which this collector does not poll", deviceId);
            return;
        }

        logger.LogInformation("Reading the config of {Entity} now: something was just written to it", inverter.DisplayName);
        request.Request();
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (tokenSource != null)
        {
            await tokenSource.CancelAsync().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(0.2), token).ConfigureAwait(false);
            await Task.WhenAll(runningConfigTasks.Values.Concat(runningSensorTasks.Values)).ConfigureAwait(false);
        }

        foreach (var request in configReadRequests.Values)
        {
            request.Dispose();
        }

        // All three are keyed by the inverters of the run that has just stopped, and StartAsync builds new ones.
        // Without this, every restart would leave the entries of the previous run behind for good.
        configReadRequests.Clear();
        runningSensorTasks.Clear();
        runningConfigTasks.Clear();
        tokenSource = null;
    }

    /// <summary>
    /// Writes about a failure as much as it is worth: the first one in full, then nothing until an hour has
    /// passed, and the recovery when the inverter answers again.
    /// </summary>
    private void Report(RepeatedFailure failures, Exception exception, Gen24System gen24System, string what)
    {
        switch (failures.Record(exception))
        {
            case FailureReport.First:
                logger.LogError(exception, "Could not {What} of GEN24 inverter {BaseUri}", what, gen24System.Service.Connection);
                break;

            case FailureReport.StillFailing:
                logger.LogWarning("Still cannot {What} of GEN24 inverter {BaseUri} after {Attempts} attempts: {Failure}",
                    what, gen24System.Service.Connection, failures.Attempts, exception.Message);
                break;

            default:
                logger.LogDebug(exception, "Could not {What} of GEN24 inverter {BaseUri}, as before", what, gen24System.Service.Connection);
                break;
        }
    }

    /// <summary>Says so once, if the inverter had stopped answering.</summary>
    private void ReportRecovery(RepeatedFailure failures, Gen24System gen24System, string what)
    {
        if (failures.Recover() is not { } outage)
        {
            return;
        }

        logger.LogInformation("Can {What} of GEN24 inverter {BaseUri} again, after {Attempts} attempts over {Duration:g}",
            what, gen24System.Service.Connection, outage.Attempts, outage.Duration);
    }

    private async Task Update(Gen24System gen24System, SemaphoreSlim semaphore, RepeatedFailure failures)
    {
        var start = DateTime.UtcNow;

        if (tokenSource == null)
        {
            return;
        }

        try
        {
            var token = tokenSource?.Token ?? throw new TaskCanceledException();

            try
            {
                if (gen24System.Config is null)
                {
                    try
                    {
                        gen24System.Config = await ReadGen24Config(gen24System, semaphore, token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not TaskCanceledException)
                    {
                        Report(failures, ex, gen24System, "read the config");
                        return;
                    }
                }

                try
                {
                    if (gen24System.Config?.Components is not null)
                    {
                        var sensorsTask = gen24System.Service.GetFroniusData(gen24System.Config.Components, token);
                        var standByStatusTask = gen24System.Service.GetInverterStandByStatus(token);
                        gen24System.Sensors = await sensorsTask.ConfigureAwait(false);
                        gen24System.Sensors.StandByStatus = await standByStatusTask.ConfigureAwait(false);

                        dataControlService.AddOrUpdate(new ManagedDevice(gen24System, gen24System.Service.Connection, typeof(IGen24Service)));

                        ReportRecovery(failures, gen24System, "read the sensors");
                        logger.LogDebug("{Entity} sensors updated in {Duration:N0} ms", gen24System.DisplayName, (DateTime.UtcNow - start).TotalMilliseconds);

                        if (gen24System.Sensors?.PrimaryPowerMeter?.DataTime != null)
                        {
                            var fileName = Path.Combine(Parameters.LogDirectory, @$"EnergyHistory-{gen24System.Sensors.PrimaryPowerMeter.SerialNumber}.log");

                            if (gen24System.Sensors.PrimaryPowerMeter.DataTime - lastLogTime >= TimeSpan.FromMinutes(2) && gen24System.Sensors.PrimaryPowerMeter.DataTime.Value.Minute % 5 == 0)
                            {
                                var item = new SmartMeterCalibrationHistoryItem
                                {
                                    CalibrationDate = gen24System.Sensors.PrimaryPowerMeter.DataTime ?? DateTime.UtcNow,
                                    EnergyRealConsumed = gen24System.Sensors.PrimaryPowerMeter.EnergyActiveConsumed ?? double.NaN,
                                    EnergyRealProduced = gen24System.Sensors.PrimaryPowerMeter.EnergyActiveProduced ?? double.NaN,
                                    ConsumedOffset = double.NaN,
                                    ProducedOffset = double.NaN,
                                };

                                List<SmartMeterCalibrationHistoryItem>? history = null;

                                var serializer = new XmlSerializer(typeof(List<SmartMeterCalibrationHistoryItem>));

                                if (File.Exists(fileName))
                                {
                                    try
                                    {
                                        await using var inStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                                        history = (List<SmartMeterCalibrationHistoryItem>?)serializer.Deserialize(inStream);
                                    }
                                    catch (Exception) { /**/ }
                                }

                                history ??= [];

                                if (history.Count >= 5000)
                                {
                                    history.RemoveAt(0);
                                }

                                history.Add(item);

                                await using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                                await using var writer = new XmlTextWriter(stream, Encoding.UTF8);
                                writer.Indentation = 4;
                                writer.Formatting = Formatting.Indented;
                                serializer.Serialize(writer, history.ToList());
                                lastLogTime = gen24System.Sensors.PrimaryPowerMeter.DataTime ?? DateTime.UtcNow;
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    // It said "read the config" here too, which it was not: the config is read above.
                    Report(failures, ex, gen24System, "read the sensors");
                }
            }
            finally
            {
                var duration = DateTime.UtcNow - start;
                var waitTime = Math.Max(0, (Parameters.RefreshRate - duration).TotalMilliseconds);
                await Task.Delay((int)waitTime, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var connection = gen24System.Service.Connection;
                gen24System.Service.Connection = (connection?.Clone()) as WebConnection;
                runningSensorTasks[gen24System] = Update(gen24System, semaphore, failures);
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogInformation(ex, "Updating {WebConnection} sensors stopped", gen24System.Service.Connection);
            semaphore.Dispose();
        }
    }

    private async Task UpdateConfig(Gen24System gen24System, SemaphoreSlim semaphore, RepeatedFailure failures)
    {
        if (tokenSource == null)
        {
            return;
        }

        var token = tokenSource?.Token ?? throw new TaskCanceledException();

        var startTime = DateTime.UtcNow;

        try
        {
            // Not a plain delay: whoever writes a setting to this inverter asks for the next read through
            // ReadConfigNow, and that has to cut the wait short - see ReadNowRequest. Inside the try, because
            // this is where the collector spends nearly all of its time and so where a stop nearly always
            // arrives; outside it, a stop faulted the task and StopAsync threw while waiting for it.
            var request = configReadRequests.GetOrAdd(gen24System, _ => new ReadNowRequest());
            var wasAskedFor = await request.WaitAsync(Parameters.ConfigRefreshRate, token).ConfigureAwait(false);
            startTime = DateTime.UtcNow;

            gen24System.Config = await ReadGen24Config(gen24System, semaphore, token).ConfigureAwait(false);

            if (gen24System.Config?.Versions?.SerialNumber != null)
            {
                dataControlService.AddOrUpdate(new ManagedDevice(gen24System, gen24System.Service.Connection, typeof(IGen24Service)));

                ReportRecovery(failures, gen24System, "read the config");

                logger.LogInformation("{Entity} config updated in {Duration:N0} ms{OnRequest}", gen24System.DisplayName,
                    (DateTime.UtcNow - startTime).TotalMilliseconds, wasAskedFor ? " on request" : string.Empty);
            }
        }
        catch (OperationCanceledException ex) when (token.IsCancellationRequested)
        {
            // A stop, not a failure. TaskCanceledException from a delay and OperationCanceledException from a
            // semaphore both land here, which the old check for TaskCanceledException alone did not.
            logger.LogInformation(ex, "Updating {WebConnection} config stopped", gen24System.Service.Connection);
        }
        catch (Exception ex)
        {
            Report(failures, ex, gen24System, "read the config");
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                runningConfigTasks[gen24System] = UpdateConfig(gen24System, semaphore, failures);
            }
        }
    }

    private static async Task<Gen24Config?> ReadGen24Config(Gen24System gen24System, SemaphoreSlim semaphore, CancellationToken token)
    {
        if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(10), token).ConfigureAwait(false))
        {
            return null;
        }

        try
        {
            var versionsToken = (await gen24System.Service.GetFroniusJsonResponse("api/status/version", token: token).ConfigureAwait(false)).Token;
            var componentsToken = (await gen24System.Service.GetFroniusJsonResponse("api/components/", token: token).ConfigureAwait(false)).Token;
            var configToken = (await gen24System.Service.GetFroniusJsonResponse("api/config/", token: token).ConfigureAwait(false)).Token;

#if DEBUG
            // ReSharper disable UnusedVariable
            var configString = configToken.ToString();
            var versionString = versionsToken.ToString();
            var componentsString = componentsToken.ToString();
            // ReSharper restore UnusedVariable
#endif

            var result= Gen24Config.Parse(versionsToken, componentsToken, configToken);
            result.BatterySettings?.SocMinPreserve = (await gen24System.Service.GetSystemPreservation(token))?.MinSoc;
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
