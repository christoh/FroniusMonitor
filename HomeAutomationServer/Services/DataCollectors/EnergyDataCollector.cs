namespace De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;

/// <summary>
///     Collects what the price chart shows and publishes today and tomorrow to <see cref="IDataControlService" />
///     as one <see cref="EnergyChartData" />, so that the hub pushes it to every client like a device.
/// </summary>
/// <remarks>
///     <para>
///         Three sources, none of them a device of the installation: Awattar for the market prices, the tariff
///         components and the sun and wind production of the grid; the DWD for the weather at one station; and
///         the Wattpilot, whose <c>awpl</c> forecast carries the same market prices and fills in where Awattar
///         has not answered. The Wattpilot is not asked - the collector listens to
///         <see cref="IDataControlService.DeviceUpdate" /> and takes the prices off the charger the
///         <see cref="WattPilotDataCollector" /> publishes.
///     </para>
///     <para>
///         Everything that is fetched goes into <see cref="IEnergyHistoryStore" /> first and the chart data is
///         assembled from there, so a day that is over is served from the store and never asked of Awattar again -
///         Awattar allows about a hundred requests a day. One timer looks at the clock every minute and does what
///         is due: tomorrow's prices in the early afternoon until they are there, the production forecast again in
///         the evening, the weather every half hour, the tariff once a day.
///     </para>
/// </remarks>
public sealed class EnergyDataCollector(
    ILogger<EnergyDataCollector> logger,
    IOptionsMonitor<EnergyDataCollectorParameters> options,
    IDataControlService dataControlService,
    IAwattarClient awattar,
    IDwdWeatherClient dwd,
    IEnergyHistoryStore store,
    TimeProvider? timeProvider = null
) : IHomeAutomationRunner, IEnergyDataService, IAsyncDisposable
{
    /// <summary>How soon a day that should be complete but is not is asked for again, outside the windows.</summary>
    private static readonly TimeSpan incompleteDayRetry = TimeSpan.FromHours(1);

    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim tickSemaphore = new(1, 1);
    private Timer? timer;
    private bool isStarted;
    private bool startupFetched;
    private TimeZoneInfo zone = TimeZoneInfo.Local;
    private DateTimeOffset lastPriceAttempt = DateTimeOffset.MinValue;
    private DateTimeOffset lastProductionAttempt = DateTimeOffset.MinValue;
    private DateTimeOffset lastWeatherPoll = DateTimeOffset.MinValue;
    private DateOnly componentsDay = DateOnly.MinValue;
    private string? weatherStationName;
    private WattPilotPriceSnapshot? lastWattPilotPrices;

    private EnergyDataCollectorParameters Parameters => options.CurrentValue;

    public bool IsEnabled => Parameters.Settings != null;

    public EnergyChartData? Current { get; private set; }

    /// <summary>The time zone the day boundaries and the windows are meant in.</summary>
    public TimeZoneInfo TimeZone => zone;

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
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Could not properly stop {ServiceName}", GetType().Name);
            }
        }
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        await StopAsync(token).ConfigureAwait(false);

        if (Parameters.Settings is not { } settings)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No energy data section is configured, no prices and no weather are collected");
            }

            return;
        }

        zone = settings.ResolveTimeZone(logger);
        await store.InitializeAsync(token).ConfigureAwait(false);
        isStarted = true;
        dataControlService.DeviceUpdate += OnDeviceUpdate;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Collecting energy data for {Region} in time zone {Zone}, DWD station '{Station}', tariff query {Tariff}", settings.PriceRegion, zone.Id, settings.DwdStationId, settings.HasTariffQuery ? "configured" : "not configured");
        }

        timer = new Timer(TimerElapsed, null, TimeSpan.Zero, Parameters.TickInterval);
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        isStarted = false;
        dataControlService.DeviceUpdate -= OnDeviceUpdate;

        if (timer != null)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
            timer = null;
        }

        if (dataControlService.Entities.ContainsKey(EnergyChartData.DeviceId))
        {
            dataControlService.Remove(EnergyChartData.DeviceId);
        }

        Current = null;
    }

    public async Task<EnergyChartData> GetDayAsync(DateOnly day, CancellationToken token = default)
    {
        var (from, to) = DayBounds(day);

        if (Parameters.Settings is { } settings && day < Today())
        {
            // A day that is over does not change any more, so whatever the store has is final; only what it does
            // not have yet is asked for, once, and kept.
            var prices = await store.GetPricesAsync(EnergyPriceSource.Awattar, from, to, token).ConfigureAwait(false);

            if (!EnergyPriceCalculator.CoversSpan(prices, from, to))
            {
                await FetchAwattarAsync(settings, from, to, prices: true, productions: false, token).ConfigureAwait(false);
            }

            if ((await store.GetProductionsAsync(settings.PriceRegion, from, to, token).ConfigureAwait(false)).Count == 0)
            {
                await FetchAwattarAsync(settings, from, to, prices: false, productions: true, token).ConfigureAwait(false);
            }

            if (settings.HasTariffQuery && (await store.GetPriceComponentsAsync(day, token).ConfigureAwait(false)).Count == 0)
            {
                await FetchComponentsAsync(settings, day, token).ConfigureAwait(false);
            }
        }

        return await BuildAsync(from, to, day, token).ConfigureAwait(false);
    }

    /// <summary>
    ///     One look at the clock: fetches what is due and publishes when anything changed. Public so that a test can
    ///     drive it with a clock of its own; the timer calls it every <see cref="EnergyDataCollectorParameters.TickInterval" />.
    /// </summary>
    public async Task TickAsync(CancellationToken token = default)
    {
        if (Parameters.Settings is not { } settings)
        {
            return;
        }

        var now = clock.GetUtcNow();
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var today = DateOnly.FromDateTime(local.DateTime);
        var tomorrow = today.AddDays(1);
        var localTime = TimeOnly.FromDateTime(local.DateTime);
        var (todayFrom, todayTo) = DayBounds(today);
        var (tomorrowFrom, tomorrowTo) = DayBounds(tomorrow);
        var parameters = Parameters;
        var changed = false;

        if (!startupFetched)
        {
            // Whatever the store has may be stale after a restart; today and tomorrow are read once at the start.
            startupFetched = true;
            changed |= await FetchAwattarAsync(settings, todayFrom, tomorrowTo, prices: true, productions: true, token).ConfigureAwait(false);
        }
        else
        {
            var todayComplete = await IsCompleteAsync(todayFrom, todayTo, token).ConfigureAwait(false);
            var tomorrowComplete = await IsCompleteAsync(tomorrowFrom, tomorrowTo, token).ConfigureAwait(false);

            if (!todayComplete && now - lastPriceAttempt >= incompleteDayRetry)
            {
                changed |= await FetchAwattarAsync(settings, todayFrom, todayTo, prices: true, productions: true, token).ConfigureAwait(false);
            }

            if (!tomorrowComplete && IsInWindow(localTime, parameters.PriceWindowStart, parameters.PriceWindowEnd) && now - lastPriceAttempt >= parameters.PriceRetryInterval)
            {
                changed |= await FetchAwattarAsync(settings, tomorrowFrom, tomorrowTo, prices: true, productions: true, token).ConfigureAwait(false);
            }

            if (IsInWindow(localTime, parameters.ProductionWindowStart, parameters.ProductionWindowEnd) && now - lastProductionAttempt >= parameters.PriceRetryInterval)
            {
                changed |= await FetchAwattarAsync(settings, todayFrom, tomorrowTo, prices: false, productions: true, token).ConfigureAwait(false);
            }
        }

        if (settings.HasTariffQuery && componentsDay != today)
        {
            if ((await store.GetPriceComponentsAsync(today, token).ConfigureAwait(false)).Count == 0)
            {
                changed |= await FetchComponentsAsync(settings, today, token).ConfigureAwait(false);
            }

            componentsDay = today;
            changed = true;
        }

        if (settings.DwdStationId.Length > 0 && now - lastWeatherPoll >= parameters.WeatherRefreshRate)
        {
            lastWeatherPoll = now;
            changed |= await FetchWeatherAsync(settings.DwdStationId, token).ConfigureAwait(false);
        }

        if (changed || Current == null || Current.From != todayFrom)
        {
            await PublishAsync(today, token).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Awattar's slots where it has them, the Wattpilot's for every span Awattar leaves open. Awattar first
    ///     because it has the history and the components; the Wattpilot's numbers are the same auction, so a mixed
    ///     day is not a contradiction.
    /// </summary>
    public static List<EnergyPricePoint> MergePrices(IReadOnlyList<EnergyPricePoint> preferred, IReadOnlyList<EnergyPricePoint> fallback)
    {
        var result = new List<EnergyPricePoint>(preferred);

        foreach (var candidate in fallback)
        {
            if (!preferred.Any(p => p.StartTime < candidate.EndTime && candidate.StartTime < p.EndTime))
            {
                result.Add(candidate);
            }
        }

        result.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        return result;
    }

    /// <summary>One point per hour: the measurement where there is one, the forecast otherwise.</summary>
    public static List<WeatherPoint> MergeWeather(IEnumerable<WeatherPoint> points)
    {
        return points
            .GroupBy(p => p.Time)
            .Select(g => g.OrderBy(p => p.IsForecast ? 1 : 0).First())
            .OrderBy(p => p.Time)
            .ToList();
    }

    /// <summary>The Wattpilot's forecast as one point per interval.</summary>
    public static List<EnergyPricePoint> ExpandWattPilotPrices(WattPilotElectricityPrice prices)
    {
        var start = DateTime.UnixEpoch.AddSeconds(prices.StartSeconds);
        var interval = TimeSpan.FromSeconds(prices.IntervalSeconds);

        return prices.CentsPerKiloWattHour
            .Select((cents, i) => new EnergyPricePoint
            {
                StartTime = start + interval * i,
                EndTime = start + interval * (i + 1),
                CentsPerKiloWattHour = cents,
                Source = EnergyPriceSource.WattPilot,
            })
            .ToList();
    }

    private async Task<bool> IsCompleteAsync(DateTime from, DateTime to, CancellationToken token)
    {
        var prices = await store.GetPricesAsync(EnergyPriceSource.Awattar, from, to, token).ConfigureAwait(false);
        return EnergyPriceCalculator.CoversSpan(prices, from, to);
    }

    private static bool IsInWindow(TimeOnly time, TimeOnly start, TimeOnly end) => time >= start && time < end;

    private async Task<bool> FetchAwattarAsync(EnergyDataSettings settings, DateTime from, DateTime to, bool prices, bool productions, CancellationToken token)
    {
        var changed = false;

        if (prices)
        {
            lastPriceAttempt = clock.GetUtcNow();

            try
            {
                var list = (await awattar.GetMarketPricesAsync(settings.PriceRegion, from, to, token).ConfigureAwait(false)).Where(p => p.StartTime >= from && p.StartTime < to).ToList();
                await store.UpsertPricesAsync(list, token).ConfigureAwait(false);
                changed = true;

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Awattar answered {Count} market prices for {From:yyyy-MM-dd HH:mm}Z to {To:yyyy-MM-dd HH:mm}Z", list.Count, from, to);
                }
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "Awattar market prices for {From:yyyy-MM-dd HH:mm}Z to {To:yyyy-MM-dd HH:mm}Z could not be read", from, to);
                }
            }
        }

        if (productions)
        {
            lastProductionAttempt = clock.GetUtcNow();

            try
            {
                var list = (await awattar.GetProductionsAsync(settings.PriceRegion, from, to, token).ConfigureAwait(false)).Where(p => p.StartTime >= from && p.StartTime < to).ToList();
                await store.UpsertProductionsAsync(settings.PriceRegion, list, token).ConfigureAwait(false);
                changed = true;

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Awattar answered {Count} production slots for {From:yyyy-MM-dd HH:mm}Z to {To:yyyy-MM-dd HH:mm}Z", list.Count, from, to);
                }
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "Awattar productions for {From:yyyy-MM-dd HH:mm}Z to {To:yyyy-MM-dd HH:mm}Z could not be read", from, to);
                }
            }
        }

        return changed;
    }

    private async Task<bool> FetchComponentsAsync(EnergyDataSettings settings, DateOnly day, CancellationToken token)
    {
        try
        {
            var components = await awattar.GetPriceComponentsAsync(settings, day, token).ConfigureAwait(false);
            await store.UpsertPriceComponentsAsync(day, components, token).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Awattar answered {Count} tariff components for {Day}", components.Count, day);
            }

            return true;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The Awattar tariff for {Day} could not be read", day);
            }

            return false;
        }
    }

    private async Task<bool> FetchWeatherAsync(string stationId, CancellationToken token)
    {
        var changed = false;

        try
        {
            var forecast = await dwd.GetForecastAsync(stationId, token).ConfigureAwait(false);
            weatherStationName = forecast.StationName ?? weatherStationName;
            await store.UpsertWeatherAsync(stationId, forecast.Points, token).ConfigureAwait(false);
            changed = true;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("DWD forecast of {IssueTime:yyyy-MM-dd HH:mm}Z for {Station} has {Count} hours", forecast.IssueTime, forecast.StationName ?? stationId, forecast.Points.Count);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The DWD forecast for station {StationId} could not be read", stationId);
            }
        }

        try
        {
            var observations = await dwd.GetObservationsAsync(stationId, token).ConfigureAwait(false);

            if (observations.Count > 0)
            {
                await store.UpsertWeatherAsync(stationId, observations, token).ConfigureAwait(false);
                changed = true;
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("DWD station {StationId} reported {Count} measured hours", stationId, observations.Count);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The DWD measurements of station {StationId} could not be read", stationId);
            }
        }

        return changed;
    }

    private async Task PublishAsync(DateOnly today, CancellationToken token)
    {
        var (from, _) = DayBounds(today);
        var (_, to) = DayBounds(today.AddDays(1));
        Current = await BuildAsync(from, to, today, token).ConfigureAwait(false);
        dataControlService.AddOrUpdate(EnergyChartData.DeviceId, new ManagedDevice(Current, null, typeof(IEnergyDataService)));

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Published energy data with {Prices} prices ({WattPilot} from the Wattpilot), {Components} components, {Productions} productions, {Weather} weather hours", Current.Prices.Count, Current.Prices.Count(p => p.Source == EnergyPriceSource.WattPilot), Current.PriceComponents.Count, Current.Productions.Count, Current.Weather.Count);
        }
    }

    private async Task<EnergyChartData> BuildAsync(DateTime from, DateTime to, DateOnly componentsOf, CancellationToken token)
    {
        var settings = Parameters.Settings ?? new EnergyDataSettings();
        var awattarPrices = await store.GetPricesAsync(EnergyPriceSource.Awattar, from, to, token).ConfigureAwait(false);
        var wattPilotPrices = await store.GetPricesAsync(EnergyPriceSource.WattPilot, from, to, token).ConfigureAwait(false);
        var components = (await store.GetPriceComponentsAsync(componentsOf, token).ConfigureAwait(false)).ToList();

        if (components.Count > 0 && settings.SurchargeCentsPerKiloWattHour != 0)
        {
            // Awattar's own fee is not in Awattar's answer. Without the other components a buying price would be
            // the market price plus this fee and nothing else, which is no buying price, so it comes only with them.
            components.Add(new EnergyPriceComponent
            {
                Name = "aWATTar",
                Description = Resources.AwattarServiceFee,
                NetPrice = settings.SurchargeCentsPerKiloWattHour,
                TaxRate = settings.VatRate,
                Unit = EnergyPriceComponent.CentsPerKiloWattHourUnit,
            });
        }

        var productions = await store.GetProductionsAsync(settings.PriceRegion, from, to, token).ConfigureAwait(false);
        var weather = settings.DwdStationId.Length > 0 ? MergeWeather(await store.GetWeatherAsync(settings.DwdStationId, from, to, token).ConfigureAwait(false)) : [];

        return new EnergyChartData
        {
            From = from,
            To = to,
            PriceRegion = settings.PriceRegion,
            MarketVatRate = settings.VatRate,
            Prices = MergePrices(awattarPrices, wattPilotPrices),
            PriceComponents = components,
            Productions = [.. productions],
            Weather = weather,
            WeatherStationName = settings.DwdStationId.Length > 0 ? weatherStationName ?? settings.DwdStationId : null,
            Timestamp = clock.GetUtcNow().UtcDateTime,
        };
    }

    private DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);

    /// <summary>The UTC span of one local day. 23 or 25 hours on the days the clocks change.</summary>
    private (DateTime From, DateTime To) DayBounds(DateOnly day)
    {
        return (StartOfDay(day), StartOfDay(day.AddDays(1)));

        DateTime StartOfDay(DateOnly d) => TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone);
    }

    /// <summary>
    ///     The Wattpilot's forecast arrives as part of the charger the Wattpilot collector publishes several times a
    ///     second. Only a forecast that differs from the last one seen is expanded and stored.
    /// </summary>
    private async void OnDeviceUpdate(object? sender, DeviceUpdateEventArgs e)
    {
        try
        {
            if (!isStarted || e.DeviceAction == DeviceAction.Delete || e.Device.Device is not WattPilot { ElectricityPrices: { CentsPerKiloWattHour.Length: > 0, IntervalSeconds: > 0 } prices })
            {
                return;
            }

            if (lastWattPilotPrices?.Matches(prices) == true)
            {
                return;
            }

            lastWattPilotPrices = WattPilotPriceSnapshot.Of(prices);
            var points = ExpandWattPilotPrices(prices);
            await store.UpsertPricesAsync(points).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("The Wattpilot has {Count} prices from {Start:yyyy-MM-dd HH:mm}Z in {Interval} minute steps", points.Count, points[0].StartTime, prices.IntervalSeconds / 60);
            }

            if (isStarted)
            {
                await PublishAsync(Today(), CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Unable to take the prices off the Wattpilot");
            }
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
                logger.LogError(ex, "Collecting energy data failed");
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

    private sealed record WattPilotPriceSnapshot(long StartSeconds, int IntervalSeconds, decimal[] Prices)
    {
        public static WattPilotPriceSnapshot Of(WattPilotElectricityPrice prices) => new(prices.StartSeconds, prices.IntervalSeconds, [.. prices.CentsPerKiloWattHour]);

        public bool Matches(WattPilotElectricityPrice prices) => StartSeconds == prices.StartSeconds && IntervalSeconds == prices.IntervalSeconds && Prices.AsSpan().SequenceEqual(prices.CentsPerKiloWattHour);
    }
}
