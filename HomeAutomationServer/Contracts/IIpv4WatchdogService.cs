namespace De.Hochstaetter.HomeAutomationServer.Contracts;

/// <summary>What the controller may ask of the IPv4 watchdog.</summary>
public interface IIpv4WatchdogService
{
    /// <summary>Whether <c>Settings.xml</c> configures a watchdog.</summary>
    bool IsEnabled { get; }

    /// <summary>What the watchdog is doing right now.</summary>
    Ipv4WatchdogStatus Status { get; }

    /// <summary>
    /// Asks for a test cycle: IPv6 switched off and on again although IPv4 may work, so that the installed watchdog
    /// can be tried without an outage. It runs at the next tick, under the same rules as any cycle.
    /// </summary>
    Ipv4WatchdogTestRequest RequestTestCycle();
}
