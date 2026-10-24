namespace De.Hochstaetter.FritzBox.Tr064;

/// <summary>A TR-064 service as tr64desc.xml lists it.</summary>
public sealed record Tr064Service(string ServiceType, string ServiceId, string ControlUrl, string ScpdUrl)
{
    /// <summary>
    /// The name part of the service type, <c>X_AVM-DE_USPController</c> for
    /// <c>urn:dslforum-org:service:X_AVM-DE_USPController:1</c>.
    /// </summary>
    public string Name => ServiceType.Split(':') is { Length: >= 5 } parts ? parts[3] : ServiceType;
}

/// <summary>
/// The TR-064 root description of a box (<c>tr64desc.xml</c>): what it is, which firmware it runs and which services
/// it offers.
/// </summary>
public sealed class Tr064Description
{
    public string? FriendlyName { get; private init; }

    public string? ModelName { get; private init; }

    /// <summary>The FRITZ!OS version as the box displays it, for example <c>154.08.20</c>.</summary>
    public string? SoftwareVersion { get; private init; }

    public IReadOnlyList<Tr064Service> Services { get; private init; } = [];

    public static Tr064Description Parse(string xml)
    {
        var root = XDocument.Parse(xml).Root ?? throw new InvalidDataException("tr64desc.xml is empty");
        var ns = root.Name.Namespace;
        var device = root.Element(ns + "device");

        return new Tr064Description
        {
            FriendlyName = device?.Element(ns + "friendlyName")?.Value,
            ModelName = device?.Element(ns + "modelName")?.Value,
            SoftwareVersion = root.Element(ns + "systemVersion")?.Element(ns + "Display")?.Value,
            Services = root.Descendants(ns + "service")
                .Select(s => new Tr064Service
                (
                    s.Element(ns + "serviceType")?.Value ?? string.Empty,
                    s.Element(ns + "serviceId")?.Value ?? string.Empty,
                    s.Element(ns + "controlURL")?.Value ?? string.Empty,
                    s.Element(ns + "SCPDURL")?.Value ?? string.Empty
                ))
                .ToList(),
        };
    }

    /// <summary>The service whose type has the name <paramref name="name"/>, for example <c>DeviceInfo</c>.</summary>
    public Tr064Service? FindService(string name) => Services.FirstOrDefault(s => s.Name == name);
}
