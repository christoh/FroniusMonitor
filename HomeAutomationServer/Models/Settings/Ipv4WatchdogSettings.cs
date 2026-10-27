namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
///     The IPv4 watchdog of a FRITZ!Box whose provider sometimes drops IPv4 and keeps IPv6: when the box's public
///     IPv4 stops answering pings, the watchdog switches the box's IPv6 off and on again, which brings IPv4 back.
/// </summary>
/// <remarks>
///     <para>
///         The element <em>is</em> the connection to the box's web interface: a <see cref="WebConnection" />, so
///         <c>BaseUrl</c>, <c>UserName</c> and the password are its attributes, and the usual password handling
///         applies - write <c>ClearTextPassword="..."</c> once, and the server's save replaces it with the encrypted
///         <c>Password</c>. The address has to be one the server reaches while the box's IPv4 is down, which is its
///         LAN address through a tunnel that runs over IPv6.
///     </para>
///     <para>
///         Without <see cref="PublicAddress" />, or without a user name, nothing is watched.
///     </para>
/// </remarks>
[XmlType("Ipv4Watchdog")]
public class Ipv4WatchdogSettings : WebConnection
{
    /// <summary>The box's public IPv4 address, which is pinged.</summary>
    [XmlAttribute, DefaultValue("")]
    public string PublicAddress { get; set; } = string.Empty;

    /// <summary>
    ///     Addresses that must answer for an outage to count, separated by spaces: when none of them answers either,
    ///     it is this side's IPv4 that is down, and switching the box would not help.
    /// </summary>
    [XmlAttribute, DefaultValue("1.1.1.1 9.9.9.9")]
    public string ReferenceAddresses { get; set; } = "1.1.1.1 9.9.9.9";

    /// <summary>How often the box's IPv4 is checked.</summary>
    [XmlAttribute, DefaultValue(30)]
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>How many pings a check sends at most. One answer is enough; all of them unanswered is an outage.</summary>
    [XmlAttribute, DefaultValue(5)]
    public int PingCount { get; set; } = 5;

    [XmlAttribute, DefaultValue(1000)]
    public int PingTimeoutMilliseconds { get; set; } = 1000;

    /// <summary>How long IPv6 stays off at least before it is switched back on.</summary>
    [XmlAttribute, DefaultValue(30)]
    public int SwitchOnDelaySeconds { get; set; } = 30;

    /// <summary>How long after switching IPv6 back on the result is checked. The box reconnects for about 20 s.</summary>
    [XmlAttribute, DefaultValue(30)]
    public int VerifyDelaySeconds { get; set; } = 30;

    /// <summary>At most this many cycles, test cycles included, are started within any hour.</summary>
    [XmlAttribute, DefaultValue(3)]
    public int MaximumCyclesPerHour { get; set; } = 3;

    /// <summary>How long the box may stay unreachable after IPv6 was switched off before the log warns about it.</summary>
    [XmlAttribute, DefaultValue(5)]
    public int UnreachableWarningMinutes { get; set; } = 5;

    /// <summary>How long a request to the box's web interface may take.</summary>
    [XmlAttribute, DefaultValue(10)]
    public int RequestTimeoutSeconds { get; set; } = 10;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PublicAddress) && !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(BaseUrl);
}
