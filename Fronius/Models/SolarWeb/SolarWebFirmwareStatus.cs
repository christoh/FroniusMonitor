namespace De.Hochstaetter.Fronius.Models.SolarWeb;

/// <summary>
///     What Solar.web knows about the firmware of the components of one PV system, as
///     <c>/Firmware/GetComponentUpdateInfos</c> answers it: per component the installed and the available version
///     and where the changelog is.
/// </summary>
/// <remarks>
///     It is an <see cref="IHaveUniqueId" /> although it is no device, for the same reason <c>EnergyChartData</c> is:
///     the server publishes it to <c>IDataControlService</c> under <see cref="DeviceId" /> whenever it changes, and
///     everything that exists for a device then happens for it - <c>SignalRDispatcher</c> broadcasts it as a
///     <c>SolarWebFirmwareStatus</c> hub message, <c>HomeAutomationHub.OnConnectedAsync</c> replays it to a client
///     that connects, and the controller serves it over HTTP. A client that sees <see cref="HasOutdatedFirmware" />
///     tells the user and offers the changelog.
/// </remarks>
public sealed class SolarWebFirmwareStatus : IHaveUniqueId
{
    /// <summary>The id the status is published under in <c>IDataControlService</c>.</summary>
    public const string DeviceId = "SolarWebFirmwareStatus";

    public string PvSystemId { get; set; } = string.Empty;

    /// <summary>When Solar.web answered this, UTC.</summary>
    public DateTime Timestamp { get; set; }

    public List<SolarWebFirmwareComponent> Components { get; set; } = [];

    /// <summary>True where at least one component has a newer version that Solar.web would install.</summary>
    public bool HasOutdatedFirmware => Components.Any(c => c.IsOutdated);

    /// <summary>Whether the components are the same as <paramref name="other" />'s, versions and flags included; the time stamp does not count.</summary>
    public bool SameAs(SolarWebFirmwareStatus? other) => other != null && PvSystemId == other.PvSystemId && Components.SequenceEqual(other.Components);

    #region IHaveUniqueId

    /// <summary>Always true: the status exists as long as the server does, even with no components.</summary>
    public bool IsPresent => true;

    public string Manufacturer => "Home Automation Server";

    public string Model => nameof(SolarWebFirmwareStatus);

    public string SerialNumber => "current";

    #endregion
}

/// <summary>
///     One component - an inverter's main firmware, a battery, a smart meter - as Solar.web lists it. A record, so
///     that two answers compare by value and only a real change is pushed to the clients.
/// </summary>
/// <remarks>
///     Not every component has firmware that Solar.web can update: older inverter models and third-party batteries
///     come with no installed and no available version, <c>UpdateRecommendationInfo</c> "Empty", and
///     <see cref="IsManagedBySolarWeb" /> false. Such a component is listed, but it can never be outdated here.
/// </remarks>
public sealed record SolarWebFirmwareComponent
{
    /// <summary>Solar.web's id of the data source, the data logger or inverter the component hangs off.</summary>
    public string DataSourceId { get; init; } = string.Empty;

    public long ComponentId { get; init; }

    /// <summary>What kind of component: <c>Gen24Main</c>, <c>Battery</c>, and so on.</summary>
    public string UpdateFamily { get; init; } = string.Empty;

    public bool IsOnline { get; init; }

    /// <summary>
    ///     The installed version, or <see langword="null" /> where Solar.web does not manage this firmware. Solar.web
    ///     writes it <c>1.41.11-1</c>: major, minor, build, and the revision after a dash; here the revision is
    ///     <see cref="Version.Revision" />, so it serializes as <c>1.41.11.1</c>. See <see cref="SolarWebVersion" />.
    /// </summary>
    public Version? InstalledVersion { get; init; }

    /// <summary>The version Solar.web offers, which is the installed one when nothing newer exists. Same form as <see cref="InstalledVersion" />.</summary>
    public Version? UpdateVersion { get; init; }

    public string UpdateVersionPrefix { get; init; } = string.Empty;

    /// <summary>The changelog of <see cref="UpdateVersion" />, a PDF, for a client to open in a browser.</summary>
    public string? ChangelogUrl { get; init; }

    public bool NewerVersionAvailable { get; init; }

    public bool IsUpdateAllowed { get; init; }

    /// <summary>When the last update ran, UTC, or <see langword="null" />.</summary>
    public DateTime? LastUpdate { get; init; }

    /// <summary>Solar.web's word on it: <c>LatestVersionInstalled</c>, <c>Empty</c>, and so on.</summary>
    public string UpdateRecommendationInfo { get; init; } = string.Empty;

    /// <summary><c>Success</c>, <c>None</c>, and so on.</summary>
    public string UpdateStatus { get; init; } = string.Empty;

    /// <summary>A note Solar.web attaches, with its state and a link, mostly empty.</summary>
    public string InfoText { get; init; } = string.Empty;

    public string InfoState { get; init; } = string.Empty;

    public string? InfoUrl { get; init; }

    /// <summary>Whether Solar.web knows this component's firmware at all. False for older inverters and third-party batteries.</summary>
    public bool IsManagedBySolarWeb => InstalledVersion != null || UpdateVersion != null;

    /// <summary>Whether a newer version is there and Solar.web would install it.</summary>
    public bool IsOutdated => IsManagedBySolarWeb && NewerVersionAvailable && IsUpdateAllowed;
}

/// <summary>
///     Solar.web's way of writing a firmware version, <c>1.41.11-1</c>: a .NET <see cref="Version" /> with a dash
///     instead of the last dot. The dash is the only difference, so the version is kept as a <see cref="Version" />
///     and the dash is put back for display.
/// </summary>
public static class SolarWebVersion
{
    /// <summary><see langword="null" /> for <see langword="null" /> or empty; anything else has to be a version, or Solar.web has changed its format.</summary>
    public static Version? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var dash = text.LastIndexOf('-');
        var normalized = dash > 0 ? string.Concat(text.AsSpan(0, dash), ".", text.AsSpan(dash + 1)) : text;

        return Version.TryParse(normalized, out var version) ? version : throw new InvalidDataException($"'{text}' is not a firmware version Solar.web is known to write");
    }

    /// <summary>The version as Solar.web writes it, <c>1.41.11-1</c>, or the plain dotted form where it has no revision.</summary>
    public static string ToSolarWebString(this Version version) => version.Revision >= 0 ? FormattableString.Invariant($"{version.Major}.{version.Minor}.{version.Build}-{version.Revision}") : version.ToString();
}
