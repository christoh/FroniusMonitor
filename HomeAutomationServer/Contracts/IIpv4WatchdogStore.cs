namespace De.Hochstaetter.HomeAutomationServer.Contracts;

/// <summary>
/// The outage log of the IPv4 watchdog: every outage and every cycle, kept across restarts. A cycle without a result
/// is one the server stopped in the middle of; the watchdog finishes it at the next start.
/// </summary>
public interface IIpv4WatchdogStore
{
    /// <summary>Creates the file and the tables where they do not exist.</summary>
    Task InitializeAsync(CancellationToken token = default);

    /// <summary>Stores a new outage and returns its <see cref="Ipv4Outage.Id"/>.</summary>
    Task<long> AddOutageAsync(Ipv4Outage outage, CancellationToken token = default);

    /// <summary>Writes every field of the outage with this <see cref="Ipv4Outage.Id"/>.</summary>
    Task UpdateOutageAsync(Ipv4Outage outage, CancellationToken token = default);

    /// <summary>Stores a new cycle and returns its <see cref="Ipv4WatchdogCycle.Id"/>.</summary>
    Task<long> AddCycleAsync(Ipv4WatchdogCycle cycle, CancellationToken token = default);

    /// <summary>Writes every field of the cycle with this <see cref="Ipv4WatchdogCycle.Id"/>.</summary>
    Task UpdateCycleAsync(Ipv4WatchdogCycle cycle, CancellationToken token = default);

    /// <summary>The newest outage that has not ended, if there is one.</summary>
    Task<Ipv4Outage?> GetOpenOutageAsync(CancellationToken token = default);

    /// <summary>The newest cycle without a result, if there is one.</summary>
    Task<Ipv4WatchdogCycle?> GetUnfinishedCycleAsync(CancellationToken token = default);

    /// <summary>How many cycles, test cycles included, started at or after <paramref name="sinceUtc"/>.</summary>
    Task<int> CountCyclesSinceAsync(DateTime sinceUtc, CancellationToken token = default);

    /// <summary>The newest <paramref name="count"/> outages, newest first.</summary>
    Task<IReadOnlyList<Ipv4Outage>> GetOutagesAsync(int count, CancellationToken token = default);

    /// <summary>The newest <paramref name="count"/> cycles, newest first.</summary>
    Task<IReadOnlyList<Ipv4WatchdogCycle>> GetCyclesAsync(int count, CancellationToken token = default);
}
