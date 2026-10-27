using De.Hochstaetter.FritzBox.Http;
using De.Hochstaetter.FritzBox.Internet;

namespace De.Hochstaetter.HomeAutomationServer.Services.Ipv4Watchdog;

/// <summary>
/// Watches the public IPv4 of a FRITZ!Box whose provider sometimes drops IPv4 and keeps IPv6, and brings it back the
/// way it is brought back by hand: IPv6 switched off, and on again a little later.
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="Ipv4WatchdogSettings.IntervalSeconds"/> the box's public IPv4 is pinged, up to
/// <see cref="Ipv4WatchdogSettings.PingCount"/> times; one answer is enough. When none comes, that is an outage, and
/// a cycle is run if three things hold: this side's own IPv4 works (one of the reference addresses answers), fewer
/// than <see cref="Ipv4WatchdogSettings.MaximumCyclesPerHour"/> cycles started in the last hour, and the box answers
/// through the tunnel and does not have IPv6 off already - somebody else switched it off then, and it is theirs.
/// </para>
/// <para>
/// A cycle is stored before IPv6 is switched off, so a server that stops in the middle finds it at the next start and
/// switches IPv6 back on. After the switch the box reconnects, IPv4 included, and the tunnel with it: the watchdog
/// waits <see cref="Ipv4WatchdogSettings.SwitchOnDelaySeconds"/> and then for as long as it takes the box to answer
/// again, switches back to the mode the box had, waits <see cref="Ipv4WatchdogSettings.VerifyDelaySeconds"/>, reads
/// the mode back and pings once more. The live test of 2026-10-27 took about 28 s for each switch.
/// </para>
/// <para>
/// Everything runs in <see cref="TickAsync"/>, every <see cref="Ipv4WatchdogParameters.TickInterval"/>, one tick at a
/// time, with the clock of the injected <see cref="TimeProvider"/>: what is due is done, everything else waits for a
/// later tick. So a test moves the clock and ticks by hand, and nothing in here sleeps.
/// </para>
/// </remarks>
public sealed class Ipv4WatchdogService : IHomeAutomationRunner, IIpv4WatchdogService
{
    /// <summary>What the log calls the box.</summary>
    private const string Box = "FRITZ!Box";

    private static readonly TimeSpan reminderInterval = TimeSpan.FromHours(1);

    private readonly ILogger<Ipv4WatchdogService> logger;
    private readonly IOptionsMonitor<Ipv4WatchdogParameters> options;
    private readonly IIpv4WatchdogStore store;
    private readonly IPinger pinger;
    private readonly TimeProvider clock;
    private readonly Func<FritzBoxHttpClientOptions, FritzBoxHttpClient> createHttpClient;
    private readonly SemaphoreSlim tickSemaphore = new(1, 1);
    private readonly RepeatedFailure tickFailures;

    private ITimer? timer;
    private CancellationTokenSource? tokenSource;
    private FritzBoxHttpClient? http;
    private Ipv6Client? ipv6;
    private Ipv4WatchdogSettings settings = new();
    private IPAddress publicAddress = IPAddress.None;
    private IReadOnlyList<IPAddress> referenceAddresses = [];

    private Ipv4WatchdogPhase phase = Ipv4WatchdogPhase.Stopped;
    private DateTime nextCheckUtc;
    private DateTime dueUtc;
    private DateTime? lastGoodUtc;
    private DateTime? lastCheckUtc;
    private bool? lastCheckAnswered;
    private Ipv4Outage? outage;
    private Ipv4WatchdogCycle? cycle;
    private volatile bool testRequested;
    private bool unreachableWarned;
    private string? holdReason;

    public Ipv4WatchdogService
    (
        ILogger<Ipv4WatchdogService> logger,
        IOptionsMonitor<Ipv4WatchdogParameters> options,
        IIpv4WatchdogStore store,
        IPinger pinger
    ) : this(logger, options, store, pinger, TimeProvider.System, o => new FritzBoxHttpClient(o)) { }

    internal Ipv4WatchdogService
    (
        ILogger<Ipv4WatchdogService> logger,
        IOptionsMonitor<Ipv4WatchdogParameters> options,
        IIpv4WatchdogStore store,
        IPinger pinger,
        TimeProvider clock,
        Func<FritzBoxHttpClientOptions, FritzBoxHttpClient> createHttpClient
    )
    {
        this.logger = logger;
        this.options = options;
        this.store = store;
        this.pinger = pinger;
        this.clock = clock;
        this.createHttpClient = createHttpClient;
        tickFailures = new RepeatedFailure(reminderInterval, clock);
    }

    public bool IsEnabled => options.CurrentValue.Settings is { IsConfigured: true };

    public Ipv4WatchdogStatus Status => new()
    {
        Phase = phase,
        LastCheckUtc = lastCheckUtc,
        LastCheckAnswered = lastCheckAnswered,
        IsTestCycleRequested = testRequested,
        Outage = outage,
        Cycle = cycle,
    };

    private DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);

        if (options.CurrentValue.Settings is not { IsConfigured: true } configured)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No IPv4 watchdog: {FileName} has no Ipv4Watchdog element with a PublicAddress, a BaseUrl and a UserName", Settings.SettingsFileName);
            }

            return;
        }

        settings = configured;
        Uri baseUri;

        // A mistake in the element must not keep the server from starting: the watchdog is the only thing it stops.
        try
        {
            publicAddress = IPAddress.Parse(settings.PublicAddress);
            referenceAddresses = [.. settings.ReferenceAddresses.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(IPAddress.Parse)];
            baseUri = new Uri(settings.BaseUrl);
            await store.InitializeAsync(token).ConfigureAwait(false);
            outage = await store.GetOpenOutageAsync(token).ConfigureAwait(false);
            cycle = await store.GetUnfinishedCycleAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FormatException or UriFormatException or IOException or Microsoft.Data.Sqlite.SqliteException)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "IPv4 watchdog not started: the Ipv4Watchdog element of {FileName} or the outage log is not usable", Settings.SettingsFileName);
            }

            return;
        }

        http = createHttpClient(new FritzBoxHttpClientOptions
        {
            BaseUri = baseUri,
            UserName = settings.UserName,
            Password = settings.Password,
            Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds),
            AcceptUntrustedCertificate = new UntrustedCertificatePolicy(baseUri, settings.CertificateFingerprint, logger).Accept,
        });

        ipv6 = new Ipv6Client(http);
        nextCheckUtc = UtcNow;

        if (cycle is null)
        {
            phase = Ipv4WatchdogPhase.Monitoring;
        }
        else
        {
            // The server stopped in the middle of a cycle. Whether it got to switch IPv6 off or back on, the box
            // knows: SwitchOnAsync reads its mode and switches only when it has to.
            phase = Ipv4WatchdogPhase.SwitchingOn;
            dueUtc = UtcNow;

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("IPv4 watchdog: cycle {Cycle} of {Started:u} was not finished when the server stopped; making sure IPv6 of {Box} {BaseUri} is back to {Mode}", cycle.Id, cycle.StartedUtc, Box, baseUri, cycle.PreviousMode);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation
            (
                "IPv4 watchdog: pinging {PublicAddress} every {Interval} s ({PingCount} pings); {Box} {BaseUri} switches IPv6 off and on when none answers, at most {MaximumCycles} times an hour",
                publicAddress, settings.IntervalSeconds, settings.PingCount, Box, baseUri, settings.MaximumCyclesPerHour
            );
        }

        tokenSource = new CancellationTokenSource();
        timer = clock.CreateTimer(TimerElapsed, null, TimeSpan.Zero, options.CurrentValue.TickInterval);
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (timer is not null)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
            timer = null;
        }

        if (tokenSource is not null)
        {
            await tokenSource.CancelAsync().ConfigureAwait(false);
        }

        // Waits for a tick that is running, so that the client is not disposed under it.
        await tickSemaphore.WaitAsync(token).ConfigureAwait(false);

        try
        {
            if (phase is Ipv4WatchdogPhase.SwitchingOn or Ipv4WatchdogPhase.Verifying && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("IPv4 watchdog stopped in the middle of cycle {Cycle}; it is finished at the next start", cycle?.Id);
            }

            tokenSource?.Dispose();
            tokenSource = null;
            http?.Dispose();
            http = null;
            ipv6 = null;
            phase = Ipv4WatchdogPhase.Stopped;
            testRequested = false;
        }
        finally
        {
            tickSemaphore.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.LogCritical(ex, "IPv4 watchdog did not stop correctly");
            }
        }

        tickSemaphore.Dispose();
    }

    public Ipv4WatchdogTestRequest RequestTestCycle()
    {
        if (phase == Ipv4WatchdogPhase.Stopped)
        {
            return Ipv4WatchdogTestRequest.NotConfigured;
        }

        if (phase != Ipv4WatchdogPhase.Monitoring)
        {
            return Ipv4WatchdogTestRequest.CycleRunning;
        }

        testRequested = true;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("IPv4 watchdog: a test cycle was asked for; it starts at the next tick");
        }

        return Ipv4WatchdogTestRequest.Accepted;
    }

    /// <summary>Does what is due, if anything. One tick at a time; a tick never throws.</summary>
    public async Task TickAsync(CancellationToken token = default)
    {
        await tickSemaphore.WaitAsync(token).ConfigureAwait(false);

        try
        {
            await DoTickAsync(token).ConfigureAwait(false);
        }
        finally
        {
            tickSemaphore.Release();
        }
    }

    private async void TimerElapsed(object? state)
    {
        // A tick that is still running (a slow box, five unanswered pings) is not queued behind: the next timer tick
        // comes soon enough.
        if (!await tickSemaphore.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await DoTickAsync(tokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // DoTickAsync catches everything but its own cancellation; this is the last line, because an exception
            // escaping an async void method ends the process.
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "IPv4 watchdog tick failed");
            }
        }
        finally
        {
            tickSemaphore.Release();
        }
    }

    private async Task DoTickAsync(CancellationToken token)
    {
        if (ipv6 is null)
        {
            return;
        }

        try
        {
            switch (phase)
            {
                case Ipv4WatchdogPhase.Monitoring when UtcNow >= nextCheckUtc || testRequested:
                    nextCheckUtc = UtcNow + TimeSpan.FromSeconds(settings.IntervalSeconds);
                    await CheckAsync(token).ConfigureAwait(false);
                    break;

                case Ipv4WatchdogPhase.SwitchingOn when UtcNow >= dueUtc:
                    await SwitchOnAsync(token).ConfigureAwait(false);
                    break;

                case Ipv4WatchdogPhase.Verifying when UtcNow >= dueUtc:
                    await VerifyAsync(token).ConfigureAwait(false);
                    break;
            }

            tickFailures.LogRecovery(logger, "run the IPv4 watchdog", Box, settings.BaseUrl);
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            tickFailures.Log(logger, ex, "run the IPv4 watchdog", Box, settings.BaseUrl);
        }
    }

    /// <summary>The check of every interval: is the box's public IPv4 there?</summary>
    private async Task CheckAsync(CancellationToken token)
    {
        var answered = await AnswersAsync(publicAddress, token).ConfigureAwait(false);
        var now = UtcNow;
        lastCheckUtc = now;
        lastCheckAnswered = answered;

        if (answered)
        {
            lastGoodUtc = now;
            holdReason = null;

            if (outage is not null)
            {
                await EndOutageAsync(now, Ipv4Recovery.ByItself, token).ConfigureAwait(false);
            }

            if (testRequested)
            {
                testRequested = false;
                await TryStartCycleAsync(isTest: true, token).ConfigureAwait(false);
            }

            return;
        }

        testRequested = false;

        if (outage is null)
        {
            outage = new Ipv4Outage { LastGoodUtc = lastGoodUtc, DetectedUtc = now };
            outage = outage with { Id = await store.AddOutageAsync(outage, token).ConfigureAwait(false) };

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("IPv4 watchdog: {PublicAddress} of {Box} {BaseUri} answers none of {PingCount} pings (outage {Outage})", publicAddress, Box, settings.BaseUrl, settings.PingCount, outage.Id);
            }
        }

        if (!await AnyAnswersAsync(referenceAddresses, token).ConfigureAwait(false))
        {
            Hold("none of the reference addresses answers either, so it is this side's IPv4 that is down; switching the box would not help");
            return;
        }

        await TryStartCycleAsync(isTest: false, token).ConfigureAwait(false);
    }

    /// <summary>Starts a cycle if the hourly limit allows it and the box is in a state to start one from.</summary>
    private async Task TryStartCycleAsync(bool isTest, CancellationToken token)
    {
        var now = UtcNow;
        var cycles = await store.CountCyclesSinceAsync(now - TimeSpan.FromHours(1), token).ConfigureAwait(false);

        if (cycles >= settings.MaximumCyclesPerHour)
        {
            Hold($"{cycles} cycles started within the last hour, which is the limit");
            return;
        }

        string mode;

        try
        {
            mode = await ipv6!.GetModeAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            Hold($"the box does not answer at {settings.BaseUrl} ({ex.Message})");
            return;
        }

        if (mode == Ipv6Client.Off)
        {
            Hold("IPv6 is switched off at the box already, by somebody else; that is left alone");
            return;
        }

        holdReason = null;
        cycle = new Ipv4WatchdogCycle { OutageId = isTest ? null : outage?.Id, IsTest = isTest, StartedUtc = now, PreviousMode = mode };
        cycle = cycle with { Id = await store.AddCycleAsync(cycle, token).ConfigureAwait(false) };

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("IPv4 watchdog: switching IPv6 of {Box} {BaseUri} off (cycle {Cycle}{Test}, mode before: {Mode})", Box, settings.BaseUrl, cycle.Id, isTest ? ", a test" : string.Empty, mode);
        }

        try
        {
            await ipv6.SetModeAsync(Ipv6Client.Off, token).ConfigureAwait(false);
            cycle = cycle with { SwitchedOffUtc = UtcNow };
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            // The request may have reached the box or not. SwitchOnAsync reads the mode and finds out.
            cycle = cycle with { Detail = $"Switching off: {ex.Message}" };

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "IPv4 watchdog: switching IPv6 of {Box} {BaseUri} off failed (cycle {Cycle})", Box, settings.BaseUrl, cycle.Id);
            }
        }

        await store.UpdateCycleAsync(cycle, token).ConfigureAwait(false);
        phase = Ipv4WatchdogPhase.SwitchingOn;
        dueUtc = UtcNow + TimeSpan.FromSeconds(settings.SwitchOnDelaySeconds);
        unreachableWarned = false;
    }

    /// <summary>
    /// Switches back to the mode before the cycle, as soon as the box answers again. Until then every tick tries,
    /// and the log warns once when it takes longer than <see cref="Ipv4WatchdogSettings.UnreachableWarningMinutes"/>.
    /// </summary>
    private async Task SwitchOnAsync(CancellationToken token)
    {
        var current = cycle!;

        if (await TryGetModeAsync(current, token).ConfigureAwait(false) is not { } mode)
        {
            return;
        }

        var next = current;

        if (mode == current.PreviousMode)
        {
            if (current.SwitchedOffUtc is null)
            {
                await FinishCycleAsync(Ipv4WatchdogCycleResult.Failed, $"IPv6 was not switched off; the box is in {mode}", answered: null, token).ConfigureAwait(false);
                return;
            }

            if (current.SwitchedOnUtc is null)
            {
                // Somebody switched it back already. A cycle that switched it back itself before the server stopped
                // has SwitchedOnUtc, and only the check is left to do.
                next = current with { SwitchedOnUtc = UtcNow, Detail = Append(current.Detail, $"IPv6 was back in {mode} already") };
            }
        }
        else
        {
            await ipv6!.SetModeAsync(current.PreviousMode, token).ConfigureAwait(false);
            next = current with { SwitchedOnUtc = UtcNow };
        }

        cycle = next;
        await store.UpdateCycleAsync(next, token).ConfigureAwait(false);
        phase = Ipv4WatchdogPhase.Verifying;
        dueUtc = UtcNow + TimeSpan.FromSeconds(settings.VerifyDelaySeconds);
        unreachableWarned = false;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("IPv4 watchdog: IPv6 of {Box} {BaseUri} is back to {Mode} (cycle {Cycle}); checking the result in {Delay} s", Box, settings.BaseUrl, next.PreviousMode, next.Id, settings.VerifyDelaySeconds);
        }
    }

    /// <summary>Reads the mode back and pings once more, once the box answers again.</summary>
    private async Task VerifyAsync(CancellationToken token)
    {
        var current = cycle!;

        if (await TryGetModeAsync(current, token).ConfigureAwait(false) is not { } mode)
        {
            return;
        }

        var answered = await AnswersAsync(publicAddress, token).ConfigureAwait(false);

        if (mode != current.PreviousMode)
        {
            await FinishCycleAsync(Ipv4WatchdogCycleResult.Failed, $"The box reports {mode} instead of {current.PreviousMode}", answered, token).ConfigureAwait(false);
        }
        else
        {
            await FinishCycleAsync(answered ? Ipv4WatchdogCycleResult.Ipv4Back : Ipv4WatchdogCycleResult.Ipv4StillDown, null, answered, token).ConfigureAwait(false);
        }
    }

    private async Task FinishCycleAsync(Ipv4WatchdogCycleResult result, string? detail, bool? answered, CancellationToken token)
    {
        var now = UtcNow;
        var finished = cycle! with { FinishedUtc = now, Result = result, Detail = Append(cycle.Detail, detail) };
        await store.UpdateCycleAsync(finished, token).ConfigureAwait(false);

        var level = result == Ipv4WatchdogCycleResult.Ipv4Back ? LogLevel.Information : LogLevel.Error;

        if (logger.IsEnabled(level))
        {
            logger.Log(level, "IPv4 watchdog: cycle {Cycle} of {Box} {BaseUri} ended after {Duration:N0} s: {Result}{Detail}", finished.Id, Box, settings.BaseUrl, (now - finished.StartedUtc).TotalSeconds, result, finished.Detail is null ? string.Empty : $" ({finished.Detail})");
        }

        if (answered is { } ipv4Answers)
        {
            lastCheckUtc = now;
            lastCheckAnswered = ipv4Answers;

            if (ipv4Answers)
            {
                lastGoodUtc = now;

                if (outage is not null)
                {
                    await EndOutageAsync(now, Ipv4Recovery.AfterCycle, token).ConfigureAwait(false);
                }
            }
        }

        cycle = null;
        phase = Ipv4WatchdogPhase.Monitoring;
        nextCheckUtc = now + TimeSpan.FromSeconds(settings.IntervalSeconds);
    }

    private async Task EndOutageAsync(DateTime now, Ipv4Recovery recovery, CancellationToken token)
    {
        var ended = outage! with { RecoveredUtc = now, Recovery = recovery };
        await store.UpdateOutageAsync(ended, token).ConfigureAwait(false);
        outage = null;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("IPv4 watchdog: {PublicAddress} of {Box} {BaseUri} answers again, {Recovery}, after {Duration:N0} s (outage {Outage})", publicAddress, Box, settings.BaseUrl, recovery, (now - ended.DetectedUtc).TotalSeconds, ended.Id);
        }
    }

    /// <summary>The box's mode, or <see langword="null"/> while the box does not answer.</summary>
    private async Task<string?> TryGetModeAsync(Ipv4WatchdogCycle current, CancellationToken token)
    {
        try
        {
            return await ipv6!.GetModeAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            var since = current.SwitchedOnUtc ?? current.SwitchedOffUtc ?? current.StartedUtc;

            if (!unreachableWarned && UtcNow - since >= TimeSpan.FromMinutes(settings.UnreachableWarningMinutes) && logger.IsEnabled(LogLevel.Error))
            {
                unreachableWarned = true;

                logger.LogError
                (
                    "IPv4 watchdog: {Box} {BaseUri} has not answered for {Minutes:N0} minutes since IPv6 was switched (cycle {Cycle}): {Message}. It keeps being tried; if the box does not come back, switch IPv6 back to {Mode} at the box.",
                    Box, settings.BaseUrl, (UtcNow - since).TotalMinutes, current.Id, ex.Message, current.PreviousMode
                );
            }
            else if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("IPv4 watchdog: {Box} {BaseUri} does not answer yet (cycle {Cycle}): {Message}", Box, settings.BaseUrl, current.Id, ex.Message);
            }

            return null;
        }
    }

    /// <summary>Says once per reason why no cycle is started for the outage that lasts.</summary>
    private void Hold(string reason)
    {
        if (reason == holdReason)
        {
            return;
        }

        holdReason = reason;

        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("IPv4 watchdog: no cycle for {Box} {BaseUri}: {Reason}", Box, settings.BaseUrl, reason);
        }
    }

    /// <summary>Up to <see cref="Ipv4WatchdogSettings.PingCount"/> pings; the first answer is enough.</summary>
    private async Task<bool> AnswersAsync(IPAddress address, CancellationToken token)
    {
        var timeout = TimeSpan.FromMilliseconds(settings.PingTimeoutMilliseconds);

        for (var ping = 0; ping < settings.PingCount; ping++)
        {
            if (await pinger.PingAsync(address, timeout, token).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> AnyAnswersAsync(IEnumerable<IPAddress> addresses, CancellationToken token)
    {
        foreach (var address in addresses)
        {
            if (await AnswersAsync(address, token).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private static string? Append(string? detail, string? more) =>
        string.IsNullOrEmpty(detail) ? more : string.IsNullOrEmpty(more) ? detail : $"{detail}. {more}";
}
