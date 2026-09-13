namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     What a controller or a hub asks the energy data collector for: the span it publishes anyway, and any other
///     day assembled from the history.
/// </summary>
public interface IEnergyDataService
{
    /// <summary>False where the settings have no energy data section, so a client can be told there is nothing to show.</summary>
    bool IsEnabled { get; }

    /// <summary>Today and tomorrow, as last published to the control service, or <see langword="null" /> before the first assembly.</summary>
    EnergyChartData? Current { get; }

    /// <summary>
    ///     One local day. A day that is over comes from the history database and is fetched from Awattar only where
    ///     the database does not have it yet; today and tomorrow are what the collector keeps current.
    /// </summary>
    Task<EnergyChartData> GetDayAsync(DateOnly day, CancellationToken token = default);
}
