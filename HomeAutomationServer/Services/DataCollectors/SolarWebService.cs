namespace De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;

/// <summary>
///     Serves Solar.web's charts out of <see cref="ISolarWebHistoryStore" />, asks Solar.web only for what the store
///     does not have or has too long ago, and watches the firmware of the system's components.
/// </summary>
/// <remarks>
///     <para>
///         The running periods are polled: every <see cref="SolarWebParameters.RefreshRate" /> the service reads
///         today, this month, this year and the whole history in every view, and the periods that ended less than
///         <see cref="SolarWebParameters.FinalAfter" /> ago as long as their charts are not complete, so that the
///         cache holds a period whole once it is over. Solar.web can be hours behind the inverter: a day is complete
///         once every measured series of its chart has a value in the day's last five minutes
///         (<see cref="SolarWebPeriod.IsDayComplete" />), a month or a year once the day chart of its last day is and
///         the month's or the year's chart was read after that. What has not arrived
///         <see cref="SolarWebParameters.FinalAfter" /> after the end never will. One chart that cannot be read does
///         not end the tick, and a tick that Solar.web refuses is repeated as soon as the back-off is over rather
///         than skipped until the next interval.
///         A client is served what is cached, whatever its age - a period that is over does not change, and a
///         running one is as fresh as the last tick - and Solar.web is asked only for a chart the cache does not
///         have. The one exception is a day that has not begun, a forecast: that is read again after
///         <see cref="SolarWebParameters.RefreshRate" />, because nothing else keeps it fresh. A Premium view the
///         account is locked out of is tried again only every <see cref="SolarWebParameters.LockedViewRetry" />.
///         Day charts older than <see cref="SolarWebParameters.DayRetention" /> are removed whenever a day chart is
///         written.
///     </para>
///     <para>
///         The firmware status is polled with the same tick, and read on request as well. It is published to
///         <see cref="IDataControlService" /> under <see cref="SolarWebFirmwareStatus.DeviceId" />
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

        // The first tick comes after one interval, not at start: a client that asks before it is served from the
        // cache or fetches the one chart it wants, while a tick is two dozen requests two seconds apart, which a
        // server that has just come up - possibly restarted several times while being set up - had better not
        // fire at Solar.web every time.
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

        return FetchChartAsync(settings, interval, view, SolarWebPeriod.Normalize(interval, date), IsGoodEnoughForAClient, token);
    }

    /// <summary>
    ///     What is cached is served: a period that is over does not change, and a running one is kept fresh by the
    ///     tick. Only a period that has not begun - a forecast day - is read again after the refresh rate, because
    ///     the tick does not cover it.
    /// </summary>
    private bool IsGoodEnoughForAClient(SolarWebChart cached) => cached.Period <= Today || clock.GetUtcNow() - cached.FetchedUtc < Parameters.RefreshRate;

    /// <summary>
    ///     What the tick leaves alone for the charts of one period: a locked Premium view that was tried recently,
    ///     and a complete chart - a day whose every measured series has a value in its last five minutes, a month or
    ///     a year whose last day's chart is complete and that was read after that day's chart was. Everything else,
    ///     every running period above all, is read again.
    /// </summary>
    /// <remarks>
    ///     The witness of a month or a year is the production day chart of its last day, read here once for all
    ///     four views. It is in the cache because the tick reads the days before the months and the years, so in
    ///     the tick that finds the last day complete the month's chart is read after it, and is complete with it.
    /// </remarks>
    private async Task<Func<SolarWebChart, bool>> TickPredicateAsync(SolarWebSettings settings, SolarWebInterval interval, DateOnly period, CancellationToken token)
    {
        var endUtc = SolarWebPeriod.EndUtc(interval, period, zone);

        var witness = interval is SolarWebInterval.Month or SolarWebInterval.Year
            ? await store.GetChartAsync(settings.PvSystemId, SolarWebInterval.Day, SolarWebView.Production, SolarWebPeriod.End(interval, period)!.Value.AddDays(-1), token).ConfigureAwait(false)
            : null;

        return cached =>
        {
            if (cached.IsPremiumFeature && clock.GetUtcNow() - cached.FetchedUtc < Parameters.LockedViewRetry)
            {
                return true;
            }

            if (endUtc is not { } end)
            {
                // The whole history never ends.
                return false;
            }

            return interval == SolarWebInterval.Day
                ? SolarWebPeriod.IsDayComplete(cached, end)
                : witness != null && SolarWebPeriod.IsDayComplete(witness, end) && cached.FetchedUtc >= witness.FetchedUtc;
        };
    }

    /// <summary>
    ///     The periods the tick reads: the current one, and every one before it that ended less than
    ///     <see cref="SolarWebParameters.FinalAfter" /> ago - yesterday and the day before, the previous month on the
    ///     first two days of a month, the previous year on 1 and 2 January. Whatever an older period is missing is
    ///     never going to arrive, and it is left as it is.
    /// </summary>
    private IEnumerable<DateOnly> PeriodsToRefresh(SolarWebInterval interval)
    {
        var period = SolarWebPeriod.Normalize(interval, Today);
        yield return period;

        var horizon = clock.GetUtcNow().UtcDateTime - Parameters.FinalAfter;

        while (SolarWebPeriod.Previous(interval, period) is { } previous && SolarWebPeriod.EndUtc(interval, previous, zone) is { } end && end > horizon)
        {
            yield return previous;
            period = previous;
        }
    }

    private Task<SolarWebChart> FetchChartAsync(SolarWebSettings settings, SolarWebInterval interval, SolarWebView view, DateOnly period, Func<SolarWebChart, bool> isGoodEnough, CancellationToken token)
    {
        // The whole history has no date; Solar.web ignores the one that is sent, and the cache key is MinValue.
        var date = interval == SolarWebInterval.All ? Today : period;

        return AskAsync(
            t => store.GetChartAsync(settings.PvSystemId, interval, view, period, t),
            isGoodEnough,
            t => client.GetChartAsync(settings, interval, view, date, t),
            async (chart, t) =>
            {
                await store.UpsertChartAsync(chart, t).ConfigureAwait(false);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Solar.web answered the {View} chart of {Interval} {Period} with {Series} series, {Sum}{Completeness}", view, interval, chart.Title, chart.Series.Count, chart.SumValue, DescribeCompleteness(chart));
                }

                if (interval == SolarWebInterval.Day)
                {
                    await PurgeAsync(settings, t).ConfigureAwait(false);
                }
            },
            Describe(interval, view, period),
            token);
    }

    /// <summary>A chart as the log names it.</summary>
    private static string Describe(SolarWebInterval interval, SolarWebView view, DateOnly period) => $"the {view} chart of {interval} {period:yyyy-MM-dd}";

    /// <summary>
    ///     For the log line of a day chart whose day is over: whether the day has arrived whole, and where its data
    ///     ends if not - which is how far Solar.web is behind, and what somebody reading the log wants to know when a
    ///     day is read again tick after tick. Empty for a running day and for the other intervals.
    /// </summary>
    private string DescribeCompleteness(SolarWebChart chart)
    {
        if (chart.Interval != SolarWebInterval.Day || SolarWebPeriod.EndUtc(chart.Interval, chart.Period, zone) is not { } end || end > clock.GetUtcNow().UtcDateTime)
        {
            return string.Empty;
        }

        if (SolarWebPeriod.IsDayComplete(chart, end))
        {
            return ", complete";
        }

        var last = SolarWebPeriod.LastValueUtc(chart, end);
        return last is { } lastValue ? $", not complete yet: the data ends at {Local(new DateTimeOffset(lastValue, TimeSpan.Zero))}" : ", not complete yet: no data";
    }

    public Task<SolarWebFirmwareStatus> GetFirmwareStatusAsync(CancellationToken token = default) => GetFirmwareStatusAsync(false, token);

    /// <summary>
    ///     One tick of the timer: reads the firmware status again, whatever its age, and publishes it where it
    ///     changed; then reads the running periods - today, this month, this year, the whole history, each in every
    ///     view Solar.web has for it - and the periods that ended recently where the cache does not hold them
    ///     complete yet (<see cref="PeriodsToRefresh" />, <see cref="TickPredicateAsync" />). Public so that a test
    ///     can drive it with a clock of its own.
    /// </summary>
    /// <remarks>
    ///     Every request is one <see cref="AskAsync{T}" />, so a refusal is said in the log there and blocks the rest
    ///     of the tick the way it blocks a client: once Solar.web has asked to be left alone, or the login has failed,
    ///     the remaining charts are not tried, because every one of them would be refused the same way. Any other
    ///     failure is that one chart's: it is logged and the tick goes on, so that a chart Solar.web keeps answering
    ///     badly does not leave every chart after it in the list - the day that has just ended, say - unread for good.
    /// </remarks>
    public async Task TickAsync(CancellationToken token = default)
    {
        if (Parameters.Settings is not { IsConfigured: true } settings)
        {
            return;
        }

        await TryAsync(t => GetFirmwareStatusAsync(true, t), "the firmware status", token).ConfigureAwait(false);

        // The days first: the day chart of the last day of a month or a year is what says that the month or the
        // year is complete, so it has to be read before them.
        foreach (var interval in new[] { SolarWebInterval.Day, SolarWebInterval.Month, SolarWebInterval.Year, SolarWebInterval.All })
        {
            foreach (var period in PeriodsToRefresh(interval))
            {
                var isGoodEnough = await TickPredicateAsync(settings, interval, period, token).ConfigureAwait(false);

                foreach (var view in Enum.GetValues<SolarWebView>().Where(v => interval != SolarWebInterval.Day || !v.IsPremium()))
                {
                    if (token.IsCancellationRequested || UnavailableUntil != null || loginFailure != null)
                    {
                        return;
                    }

                    await TryAsync(t => FetchChartAsync(settings, interval, view, period, isGoodEnough, t), Describe(interval, view, period), token).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    ///     One request of the tick. A refusal and a failure to answer are said in the log by <see cref="AskAsync{T}" />
    ///     and there is nobody else to tell; anything else - a store that will not write, an answer the parser does
    ///     not understand - is said here, with what was being read. Nothing but a cancellation of the tick itself gets
    ///     out, because the tick has more to read.
    /// </summary>
    private async Task TryAsync<T>(Func<CancellationToken, Task<T>> request, string what, CancellationToken token)
    {
        try
        {
            await request(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is SolarWebUnavailableException or SolarWebLoginException or HttpRequestException or InvalidDataException or OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Reading {What} from Solar.web failed", what);
            }
        }
    }

    /// <summary>
    ///     When the next tick is due, from the end of one: after <see cref="SolarWebParameters.RefreshRate" /> as a
    ///     rule, but where Solar.web has asked to be left alone, as soon as that is over. A back-off of the same
    ///     length as the interval would otherwise swallow the next tick whole - it would start inside the block and
    ///     do nothing - and the charts that were not reached would wait two intervals instead of one.
    /// </summary>
    internal TimeSpan NextTickDelay()
    {
        var delay = Parameters.RefreshRate;

        if (UnavailableUntil is { } until)
        {
            var afterBlock = until - clock.GetUtcNow() + TimeSpan.FromSeconds(1);
            delay = afterBlock < delay ? afterBlock : delay;
        }

        return delay;
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
                    logger.LogWarning("Solar.web is left alone until {Until}: {Message}", Local(blockedUntil), ex.Message);
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
    ///     Whether Solar.web is not to be asked right now - a back-off is running or the login has failed - and
    ///     what to tell the caller where there is nothing cached to serve instead.
    /// </summary>
    private bool FallBack<T>(T? cached, out Exception? refusal) where T : class
    {
        var now = clock.GetUtcNow();

        if (blockedUntil > now)
        {
            refusal = cached == null ? new SolarWebUnavailableException(blockedUntil - now, $"Solar.web is left alone until {Local(blockedUntil)} after a 429, a 503 or its maintenance page") : null;
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

    /// <summary>A time as the user reads it: in the PV system's zone, which is the user's, with the zone named. The log and the message boxes show this.</summary>
    private string Local(DateTimeOffset time) => $"{TimeZoneInfo.ConvertTime(time, zone):yyyy-MM-dd HH:mm:ss} ({zone.Id})";

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
    ///     The next tick is scheduled from the end of this one (<see cref="NextTickDelay" />), so that a back-off does
    ///     not swallow it. The timer may be gone by then: <see cref="StopAsync" /> disposes it while a tick runs.
    /// </summary>
    private void RearmTimer()
    {
        if (!isStarted)
        {
            return;
        }

        try
        {
            timer?.Change(NextTickDelay(), Parameters.RefreshRate);
        }
        catch (ObjectDisposedException)
        {
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
                logger.LogError(ex, "The Solar.web refresh failed");
            }
        }
        finally
        {
            if (acquired)
            {
                RearmTimer();
                tickSemaphore.Release();
            }
        }
    }
}
