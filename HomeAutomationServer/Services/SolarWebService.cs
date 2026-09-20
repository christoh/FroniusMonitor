namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
///     Serves Solar.web's charts out of <see cref="ISolarWebHistoryStore" /> and asks Solar.web only for what the
///     store does not have or has too long ago.
/// </summary>
/// <remarks>
///     <para>
///         Nothing is polled. A chart is read when a client asks for it and is then kept: for good where its period
///         is over (a day, a month or a year that ended more than <see cref="SolarWebParameters.FinalAfter" /> ago), for
///         <see cref="SolarWebParameters.RefreshRate" /> where the period is still running. Day charts older than
///         <see cref="SolarWebParameters.DayRetention" /> are removed whenever a day chart is written.
///     </para>
///     <para>
///         Solar.web answers <c>429</c> when asked too often. Requests go out one at a time and at least
///         <see cref="SolarWebParameters.MinimumRequestInterval" /> apart, and a 429 stops all of them for the time its
///         <c>Retry-After</c> names, or <see cref="SolarWebParameters.DefaultRateLimitBackoff" />. While that lasts, and
///         whenever Solar.web fails for any other reason, a cached chart is served however old it is; only where
///         there is none does the failure reach the caller. A rejected login is not tried again at all - see
///         <see cref="SolarWebLoginException" /> - until the server is restarted with other credentials.
///     </para>
/// </remarks>
public sealed class SolarWebService(
    ILogger<SolarWebService> logger,
    IOptionsMonitor<SolarWebParameters> options,
    ISolarWebClient client,
    ISolarWebHistoryStore store,
    TimeProvider? timeProvider = null
) : IHomeAutomationRunner, ISolarWebService
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim requestGate = new(1, 1);
    private TimeZoneInfo zone = TimeZoneInfo.Local;
    private DateTimeOffset lastRequest = DateTimeOffset.MinValue;
    private DateTimeOffset blockedUntil = DateTimeOffset.MinValue;
    private SolarWebLoginException? loginFailure;

    private SolarWebParameters Parameters => options.CurrentValue;

    public bool IsEnabled => Parameters.Settings is { IsConfigured: true };

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);

    public DateTimeOffset? RateLimitedUntil => blockedUntil > clock.GetUtcNow() ? blockedUntil : null;

    /// <summary>How many times Solar.web was asked since the start. For the log and the tests.</summary>
    public int Requests { get; private set; }

    public void Dispose() => requestGate.Dispose();

    public async Task StartAsync(CancellationToken token = default)
    {
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

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Serving Solar.web charts of PV system {PvSystemId} as {UserName} in time zone {Zone}, running periods refreshed every {Refresh}", settings.PvSystemId, settings.UserName, zone.Id, Parameters.RefreshRate);
        }
    }

    public Task StopAsync(CancellationToken token = default) => Task.CompletedTask;

    public async Task<SolarWebChart> GetChartAsync(SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default)
    {
        var settings = Parameters.Settings is { IsConfigured: true } s ? s : throw new InvalidOperationException("No Solar.web account or PV system is configured");

        if (interval == SolarWebInterval.Day && view.IsPremium())
        {
            throw new ArgumentException($"Solar.web has no day chart of {view}; that view exists for a month, a year and the whole history", nameof(view));
        }

        var period = SolarWebPeriod.Normalize(interval, date);
        var cached = await store.GetChartAsync(settings.PvSystemId, interval, view, period, token).ConfigureAwait(false);

        if (cached != null && IsGoodEnough(cached))
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
            // Another request may have read the very chart while this one waited.
            cached = await store.GetChartAsync(settings.PvSystemId, interval, view, period, token).ConfigureAwait(false);

            if (cached != null && IsGoodEnough(cached))
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
                var chart = await client.GetChartAsync(settings, interval, view, date, token).ConfigureAwait(false);
                await store.UpsertChartAsync(chart, token).ConfigureAwait(false);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Solar.web answered the {View} chart of {Interval} {Period} with {Series} series, {Sum}", view, interval, chart.Title, chart.Series.Count, chart.SumValue);
                }

                if (interval == SolarWebInterval.Day)
                {
                    await PurgeAsync(settings, token).ConfigureAwait(false);
                }

                return chart;
            }
            catch (SolarWebRateLimitException ex)
            {
                blockedUntil = clock.GetUtcNow() + (ex.RetryAfter ?? Parameters.DefaultRateLimitBackoff);

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Solar.web asked to be left alone until {Until:u}: {Message}", blockedUntil, ex.Message);
                }

                return cached ?? throw new SolarWebRateLimitException(blockedUntil - clock.GetUtcNow(), ex.Message);
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
                    logger.LogWarning(ex, "Solar.web did not answer the {View} chart of {Interval} {Period}", view, interval, period);
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
    ///     Whether Solar.web is not to be asked right now - a 429 back-off is running or the login has failed - and
    ///     what to tell the caller where there is nothing cached to serve instead.
    /// </summary>
    private bool FallBack(SolarWebChart? cached, out Exception? refusal)
    {
        var now = clock.GetUtcNow();

        if (blockedUntil > now)
        {
            refusal = cached == null ? new SolarWebRateLimitException(blockedUntil - now, $"Solar.web asked to be left alone until {blockedUntil:u}") : null;
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
}
