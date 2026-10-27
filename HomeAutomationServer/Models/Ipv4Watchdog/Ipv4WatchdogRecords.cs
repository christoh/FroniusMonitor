namespace De.Hochstaetter.HomeAutomationServer.Models.Ipv4Watchdog;

/// <summary>How an outage of the watched box's IPv4 ended.</summary>
public enum Ipv4Recovery
{
    /// <summary>IPv4 answered again without a cycle having switched it back.</summary>
    ByItself,

    /// <summary>IPv4 answered again when a cycle verified its work.</summary>
    AfterCycle,
}

/// <summary>What a cycle achieved.</summary>
public enum Ipv4WatchdogCycleResult
{
    /// <summary>IPv6 is back in the mode it had, and the public IPv4 answers.</summary>
    Ipv4Back,

    /// <summary>IPv6 is back in the mode it had, but the public IPv4 still does not answer.</summary>
    Ipv4StillDown,

    /// <summary>The box did not do what it was told: it reported another mode after the switch.</summary>
    Failed,
}

/// <summary>
/// An outage of the watched box's IPv4, as the watchdog saw it: from the round that found every ping unanswered to
/// the round that got an answer again.
/// </summary>
public sealed record Ipv4Outage
{
    /// <summary>The store's key. 0 until the outage is stored.</summary>
    public long Id { get; init; }

    /// <summary>The last round before the outage that got an answer, if the watchdog saw one since it started.</summary>
    public DateTime? LastGoodUtc { get; init; }

    /// <summary>The first round in which no ping was answered.</summary>
    public DateTime DetectedUtc { get; init; }

    /// <summary>The first round that got an answer again. <see langword="null"/> while the outage lasts.</summary>
    public DateTime? RecoveredUtc { get; init; }

    /// <summary>How it ended. <see langword="null"/> while the outage lasts.</summary>
    public Ipv4Recovery? Recovery { get; init; }
}

/// <summary>
/// One cycle: IPv6 switched off and back on to bring IPv4 back, or a test cycle an operator asked for. It is
/// stored before IPv6 is switched off, so that a server that stops in the middle finds it at the next start and
/// switches IPv6 back on.
/// </summary>
public sealed record Ipv4WatchdogCycle
{
    /// <summary>The store's key. 0 until the cycle is stored.</summary>
    public long Id { get; init; }

    /// <summary>The outage the cycle was run for; <see langword="null"/> for a test cycle.</summary>
    public long? OutageId { get; init; }

    /// <summary>An operator asked for the cycle; IPv4 may well have worked.</summary>
    public bool IsTest { get; init; }

    public DateTime StartedUtc { get; init; }

    /// <summary>The IPv6 mode before the cycle, which is the mode it is switched back to.</summary>
    public string PreviousMode { get; init; } = string.Empty;

    /// <summary>When the box accepted the switch to off.</summary>
    public DateTime? SwitchedOffUtc { get; init; }

    /// <summary>When the box accepted the switch back to <see cref="PreviousMode"/>.</summary>
    public DateTime? SwitchedOnUtc { get; init; }

    /// <summary>When the result was established. <see langword="null"/> while the cycle runs.</summary>
    public DateTime? FinishedUtc { get; init; }

    /// <summary><see langword="null"/> while the cycle runs.</summary>
    public Ipv4WatchdogCycleResult? Result { get; init; }

    /// <summary>What else is worth knowing, in English: how long the box was unreachable, what went wrong.</summary>
    public string? Detail { get; init; }
}
