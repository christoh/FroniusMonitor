namespace De.Hochstaetter.HomeAutomationServer.Models.Ipv4Watchdog;

/// <summary>What the watchdog is waiting for.</summary>
public enum Ipv4WatchdogPhase
{
    /// <summary>Not configured, or not started.</summary>
    Stopped,

    /// <summary>Pinging the box's public IPv4 every interval.</summary>
    Monitoring,

    /// <summary>IPv6 is off; waiting for the delay to pass and for the box to answer again, to switch it back on.</summary>
    SwitchingOn,

    /// <summary>IPv6 is back on; waiting for the delay to pass, to check the result.</summary>
    Verifying,
}

/// <summary>The answer to <see cref="Contracts.IIpv4WatchdogService.RequestTestCycle"/>.</summary>
public enum Ipv4WatchdogTestRequest
{
    /// <summary>The test cycle starts at the next tick.</summary>
    Accepted,

    /// <summary>There is no watchdog: <c>Settings.xml</c> does not configure one.</summary>
    NotConfigured,

    /// <summary>A cycle is running already.</summary>
    CycleRunning,
}

/// <summary>The watchdog's state, and the newest entries of its outage log.</summary>
public sealed record Ipv4WatchdogStatus
{
    public Ipv4WatchdogPhase Phase { get; init; }

    /// <summary>When the box's public IPv4 was last pinged.</summary>
    public DateTime? LastCheckUtc { get; init; }

    /// <summary>Whether it answered then.</summary>
    public bool? LastCheckAnswered { get; init; }

    /// <summary>A test cycle has been asked for and not started yet.</summary>
    public bool IsTestCycleRequested { get; init; }

    /// <summary>The outage that lasts, if there is one.</summary>
    public Ipv4Outage? Outage { get; init; }

    /// <summary>The cycle that runs, if there is one.</summary>
    public Ipv4WatchdogCycle? Cycle { get; init; }

    /// <summary>The newest outages, newest first. Filled by the controller from the outage log.</summary>
    public IReadOnlyList<Ipv4Outage> RecentOutages { get; init; } = [];

    /// <summary>The newest cycles, newest first. Filled by the controller from the outage log.</summary>
    public IReadOnlyList<Ipv4WatchdogCycle> RecentCycles { get; init; } = [];
}
