namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
///     The Fronius Solar.web account of this server and the PV system whose history it reads. One account, one
///     system, which is why this is a single element and not a list.
/// </summary>
/// <remarks>
///     <para>
///         The element <em>is</em> the connection: a <see cref="WebConnection" />, so <c>BaseUrl</c>, <c>UserName</c>
///         and the password are its attributes, and the usual password handling applies - write
///         <c>ClearTextPassword="..."</c> once, and the server's save replaces it with the encrypted <c>Password</c>
///         and its checksum. The user name is the e-mail address the Fronius login asks for.
///     </para>
///     <para>
///         <see cref="PvSystemId" /> is the GUID in the address of the system's pages,
///         <c>https://www.solarweb.com/PvSystems/PvSystem?pvSystemId=...</c>. Without it, or without a user name,
///         nothing is read.
///     </para>
/// </remarks>
[XmlType("SolarWeb")]
public class SolarWebSettings : WebConnection
{
    public SolarWebSettings()
    {
        BaseUrl = "https://www.solarweb.com";
    }

    /// <summary>The PV system, as the GUID in the address of its Solar.web pages.</summary>
    [XmlAttribute, DefaultValue("")]
    public string PvSystemId { get; set; } = string.Empty;

    /// <summary>
    ///     How old the chart of a period that is still running - today, this month, this year, the whole history -
    ///     may be before Solar.web is asked again. A period that is over is never asked again.
    /// </summary>
    [XmlAttribute, DefaultValue(15)]
    public int RefreshMinutes { get; set; } = 15;

    /// <summary>
    ///     The time zone of the PV system, as an IANA or Windows id: it says when a day is over. Empty means the time
    ///     zone of the machine, which in a container is UTC unless <c>TZ</c> is set.
    /// </summary>
    [XmlAttribute, DefaultValue("")]
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>Whether there is an account and a system to read at all.</summary>
    [XmlIgnore]
    public bool IsConfigured => UserName.Length > 0 && PvSystemId.Length > 0;

    /// <inheritdoc cref="TimeZones.Resolve" />
    public TimeZoneInfo ResolveTimeZone(ILogger? logger = null) => TimeZones.Resolve(TimeZoneId, logger);
}
