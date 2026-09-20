namespace De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;

/// <summary>
///     Serves Solar.web's charts out of <see cref="ISolarWebHistoryStore" />, asks Solar.web only for what the store
///     does not have or has too long ago, and watches the firmware of the system's components.
/// </summary>
/// <remarks>
///     <para>
///         The charts are not polled. A chart is read when a client asks for it and is then kept: for good where its
///         period is over (a day, a month or a year that ended more than <see cref="SolarWebParameters.FinalAfter" /> ago),
///         for <see cref="SolarWebParameters.RefreshRate" /> where the period is still running. Day charts older than
///         <see cref="SolarWebParameters.DayRetention" /> are removed whenever a day chart is written.
///     </para>
///     <para>
///         The firmware status is polled, every <see cref="SolarWebParameters.RefreshRate" />, and read on request
///         as well. It is published to <see cref="IDataControlService" /> under <see cref="SolarWebFirmwareStatus.DeviceId" />
///         whenever it differs from the one before - so the hub pushes a <c>SolarWebFirmwareStatus</c> message to
///         the clients when a firmware becomes outdated, and again when it has been updated.
///     </para>
///     <para>
///         Solar.web answers <c>429</c> when asked too often, <c>503</c> or its maintenance page when it is down.
///         Requests go out one at a time and at least <see cref="SolarWebParameters.MinimumRequestInterval" /> apart,
///         and any of those three stops all of them for the time a <c>Retry-After</c> names, or
///         <see cref="SolarWebParameters.DefaultRateLimitBackoff" /> after a 429 and <see cref="SolarWebParameters.UnavailableBackoff" />
///         otherwise. While that lasts, and whenever Solar.web fails for any other reason, what is cached is served
///         however old it is; only where there is nothing does the failure reach the caller. A rejected login is not
///         tried again at all - see <see cref="SolarWebLoginException" /> - until the server is restarted with other
///         credentials.
///     </para>
/// </remarks>
public sealed class SolarWebService(
    ILogger<SolarWebService> logger,
    IOptionsMonitor<SolarWebParameters> options,
    ISolarWebClient client,
    ISolarWebHistoryStore store,
    IDataControlService dataControlService,
    TimeProvider? timeProvider = null
) : IHomeAutomationRunner, ISolarWebService
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim requestGate = new(1, 1);
    private readonly SemaphoreSlim tickSemaphore = new(1, 1);
    private Timer? timer;
    private bool isStarted;
    private TimeZoneInfo zone = TimeZoneInfo.Local;
    private DateTimeOffset lastRequest = DateTimeOffset.MinValue;
    private DateTimeOffset blockedUntil = DateTimeOffset.MinValue;
    private SolarWebLoginException? loginFailure;

    private SolarWebParameters Parameters => options.CurrentValue;

    public bool IsEnabled => Parameters.Settings is { IsConfigured: true };

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);

    public DateTimeOffset? UnavailableUntil => blockedUntil > clock.GetUtcNow() ? blockedUntil : null;

    public SolarWebFirmwareStatus? FirmwareStatus { get; private set; }

    /// <summary>How many times Solar.web was asked since the start. For the log and the tests.</summary>
    public int Requests { get; private set; }

    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Could not properly stop {ServiceName}", GetType().Name);
            }
        }

        requestGate.Dispose();
        tickSemaphore.Dispose();
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);

        if (Parameters.Settings is not { IsConfigured: true } settings)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No Solar.web account or PV system is configured, no Solar.web charts are served");
            }

            return;
        }

        zone = settings.ResolveTimeZone(logger);
        await store.InitializeAsync(token).ConfigureAwait(false);
        await PurgeAsync(settings, token).ConfigureAwait(false);
        isStarted = true;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Serving Solar.web charts of PV system {PvSystemId} as {UserName} in time zone {Zone}, running periods and the firmware status refreshed every {Refresh}", settings.PvSystemId, settings.UserName, zone.Id, Parameters.RefreshRate);
        }

        // The first firmware check comes with the first client that asks, or with the first tick - not at start,
        // where a login that takes its time would hold up the server.
        timer = new Timer(TimerElapsed, null, Parameters.RefreshRate, Parameters.RefreshRate);
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        isStarted = false;

        if (timer != null)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
            timer = null;
        }

        if (dataControlService.Entities.ContainsKey(SolarWebFirmwareStatus.DeviceId))
        {
            dataControlService.Remove(SolarWebFirmwareStatus.DeviceId);
        }

        FirmwareStatus = null;
    }

    public Task<SolarWebChart> GetChartAsync(SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default)
    {
        var settings = Configured();

        if (interval == SolarWebInterval.Day && view.IsPremium())
        {
            throw new ArgumentException($"Solar.web has no day chart of {view}; that view exists for a month, a year and the whole history", nameof(view));
        }

        var period = SolarWebPeriod.Normalize(interval, date);

        return AskAsync(
            t => store.GetChartAsync(settings.PvSystemId, interval, view, period, t),
            IsGoodEnough,
            t => client.GetChartAsync(settings, interval, view, date, t),
            async (chart, t) =>
            {
                await store.UpsertChartAsync(chart, t).ConfigureAwait(false);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Solar.web answered the {View} chart of {Interval} {Period} with {Series} series, {Sum}", view, interval, chart.Title, chart.Series.Count, chart.SumValue);
                }

                if (interval == SolarWebInterval.Day)
                {
                    await PurgeAsync(settings, t).ConfigureAwait(false);
                }
            },
            $"the {view} chart of {interval} {period:yyyy-MM-dd}",
            token);
    }

    public Task<SolarWebFirmwareStatus> GetFirmwareStatusAsync(CancellationToken token = default) => GetFirmwareStatusAsync(false, token);

    /// <summary>
    ///     One tick of the timer: reads the firmware status again, whatever its age, and publishes it where it changed.
    ///     Public so that a test can drive it with a clock of its own.
    /// </summary>
    public async Task TickAsync(CancellationToken token = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            await GetFirmwareStatusAsync(true, token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SolarWebUnavailableException or SolarWebLoginException or HttpRequestException or InvalidDataException || ex is OperationCanceledException && !token.IsCancellationRequested)
        {
            // Said in the log by AskAsync; there is nobody else to tell.
        }
    }

    private Task<SolarWebFirmwareStatus> GetFirmwareStatusAsync(bool force, CancellationToken token)
    {
        var settings = Configured();

        return AskAsync(
            _ => Task.FromResult(FirmwareStatus),
            status => !force && clock.GetUtcNow() - status.Timestamp < Parameters.RefreshRate,
            t => client.GetFirmwareStatusAsync(settings, t),
            (status, _) =>
            {
                Publish(status);
                return Task.CompletedTask;
            },
            "the firmware status",
            token);
    }

    /// <summary>Keeps the status and pushes it to the clients where it differs from the one before.</summary>
    private void Publish(SolarWebFirmwareStatus status)
    {
        var changed = !status.SameAs(FirmwareStatus);
        FirmwareStatus = status;

        if (!changed)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Solar.web firmware status is unchanged: {Count} components, {Outdated} outdated", status.Components.Count, status.Components.Count(c => c.IsOutdated));
            }

            return;
        }

        if (isStarted)
        {
            dataControlService.AddOrUpdate(SolarWebFirmwareStatus.DeviceId, new ManagedDevice(status, null, typeof(ISolarWebService)));
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            var outdated = status.Components.Where(c => c.IsOutdated).Select(c => $"{c.UpdateFamily} {c.InstalledVersion?.ToSolarWebString()} -> {c.UpdateVersion?.ToSolarWebString()}").ToList();
            logger.LogInformation("Solar.web firmware status: {Count} components, {Managed} managed by Solar.web, outdated: {Outdated}", status.Components.Count, status.Components.Count(c => c.IsManagedBySolarWeb), outdated.Count == 0 ? "none" : string.Join(", ", outdated));
        }
    }

    private SolarWebSettings Configured() => Parameters.Settings is { IsConfigured: true } settings ? settings : throw new InvalidOperationException("No Solar.web account or PV system is configured");

    /// <summary>
    ///     The one way to Solar.web: what is cached is served where it is good enough; otherwise the request goes
    ///     out through the gate, one at a time and spaced, unless a 429, a 503, the maintenance page or a failed
    ///     login says not to - then what is cached is served however old it is, and only without anything cached
    ///     does the refusal reach the caller.
    /// </summary>
    /// <param name="readCache">What is kept from the last time, or <see langword="null" />.</param>
    /// <param name="isGoodEnough">Whether the cached value is served without asking.</param>
    /// <param name="ask">The request to Solar.web.</param>
    /// <param name="keep">What to do with a fresh answer: store it, publish it.</param>
    /// <param name="what">For the log.</param>
    private async Task<T> AskAsync<T>(Func<CancellationToken, Task<T?>> readCache, Func<T, bool> isGoodEnough, Func<CancellationToken, Task<T>> ask, Func<T, CancellationToken, Task> keep, string what, CancellationToken token) where T : class
    {
        var cached = await readCache(token).ConfigureAwait(false);

        if (cached != null && isGoodEnough(cached))
        {
            return cached;
        }

        if (FallBack(cached, out var refusal))
        {
            return cached!;
        }

        if (refusal != null)
        {
            throw refusal;
        }

        await requestGate.WaitAsync(token).ConfigureAwait(false);

        try
        {
            // Another request may have fetched the very thing while this one waited.
            cached = await readCache(token).ConfigureAwait(false);

            if (cached != null && isGoodEnough(cached))
            {
                return cached;
            }

            if (FallBack(cached, out refusal))
            {
                return cached!;
            }

            if (refusal != null)
            {
                throw refusal;
            }

            await SpaceRequestsAsync(token).ConfigureAwait(false);

            try
            {
                Requests++;
                var result = await ask(token).ConfigureAwait(false);
                await keep(result, token).ConfigureAwait(false);
                return result;
            }
            catch (SolarWebUnavailableException ex)
            {
                var now = clock.GetUtcNow();
                blockedUntil = now + (ex.RetryAfter ?? (ex is SolarWebRateLimitException ? Parameters.DefaultRateLimitBackoff : Parameters.UnavailableBackoff));

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Solar.web is left alone until {Until:u}: {Message}", blockedUntil, ex.Message);
                }

                if (cached != null)
                {
                    return cached;
                }

                throw ex is SolarWebRateLimitException ? new SolarWebRateLimitException(blockedUntil - now, ex.Message) : new SolarWebUnavailableException(blockedUntil - now, ex.Message);
            }
            catch (SolarWebLoginException ex)
            {
                loginFailure = ex;

                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "The Solar.web login failed and is not tried again until the server is restarted");
                }

                if (cached != null)
                {
                    return cached;
                }

                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or TaskCanceledException && !token.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "Solar.web did not answer {What}", what);
                }

                if (cached != null)
                {
                    return cached;
                }

                throw;
            }
        }
        finally
        {
            requestGate.Release();
        }
    }

    /// <summary>
    ///     Whether the cached chart is served without asking Solar.web: always where its period is over, and where
    ///     the period is running as long as the chart is younger than the refresh rate.
    /// </summary>
    private bool IsGoodEnough(SolarWebChart cached)
    {
        var now = clock.GetUtcNow();
        return SolarWebPeriod.IsFinal(cached.Interval, cached.Period, now, zone, Parameters.FinalAfter) || now - cached.FetchedUtc < Parameters.RefreshRate;
    }

    /// <summary>
    ///     Whether Solar.web is not to be asked right now - a back-off is running or the login has failed - and
    ///     what to tell the caller where there is nothing cached to serve instead.
    /// </summary>
    private bool FallBack<T>(T? cached, out Exception? refusal) where T : class
    {
        var now = clock.GetUtcNow();

        if (blockedUntil > now)
        {
            refusal = cached == null ? new SolarWebUnavailableException(blockedUntil - now, $"Solar.web is left alone until {blockedUntil:u} after a 429, a 503 or its maintenance page") : null;
            return cached != null;
        }

        if (loginFailure != null)
        {
            refusal = cached == null ? new SolarWebLoginException("The Solar.web login has failed; see the log, then restart the server", loginFailure) : null;
            return cached != null;
        }

        refusal = null;
        return false;
    }

    private async Task SpaceRequestsAsync(CancellationToken token)
    {
        var wait = Parameters.MinimumRequestInterval - (clock.GetUtcNow() - lastRequest);

        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait, clock, token).ConfigureAwait(false);
        }

        lastRequest = clock.GetUtcNow();
    }

    private async Task PurgeAsync(SolarWebSettings settings, CancellationToken token)
    {
        var cutoff = Today.AddDays(-(int)Math.Ceiling(Parameters.DayRetention.TotalDays));
        var deleted = await store.DeleteDayChartsBeforeAsync(settings.PvSystemId, cutoff, token).ConfigureAwait(false);

        if (deleted > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Removed {Count} Solar.web day charts from before {Cutoff}", deleted, cutoff);
        }
    }

    /// <summary>
    ///     The timer callback is an async void: everything, the semaphore wait included, is inside the try, and the
    ///     semaphore is released only if it was taken.
    /// </summary>
    private async void TimerElapsed(object? state)
    {
        var acquired = false;

        try
        {
            acquired = await tickSemaphore.WaitAsync(0).ConfigureAwait(false);

            if (!acquired || !isStarted)
            {
                return;
            }

            await TickAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Checking the Solar.web firmware status failed");
            }
        }
        finally
        {
            if (acquired)
            {
                tickSemaphore.Release();
            }
        }
    }
}
