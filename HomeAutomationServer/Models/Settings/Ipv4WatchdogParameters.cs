namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>What <see cref="Services.Ipv4Watchdog.Ipv4WatchdogService" /> is configured with.</summary>
public class Ipv4WatchdogParameters
{
    /// <summary>The box and the timings from <c>Settings.xml</c>; <see langword="null" /> or an unconfigured one means no watchdog.</summary>
    public Ipv4WatchdogSettings? Settings { get; set; }

    /// <summary>
    ///     How often the watchdog looks whether something is due. The check of the box's IPv4 has its own interval
    ///     (<see cref="Ipv4WatchdogSettings.IntervalSeconds" />); this is how soon a cycle notices that the box answers
    ///     again, and how soon a test cycle starts.
    /// </summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(5);
}
