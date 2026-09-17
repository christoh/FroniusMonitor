using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// Translates between a view and the address that shows it, in both directions: the app writes the address when
/// the user navigates, and reads it when somebody arrives with a link.
/// </summary>
/// <remarks>
/// <para>
/// The address of a device is <c>/&lt;view&gt;/&lt;manufacturer&gt;/&lt;serial number&gt;</c>. Manufacturer and
/// serial number are what the user sees on the device and on its type plate, so a link can be typed by hand, and
/// together they identify one device of an installation.
/// </para>
/// <para>
/// A page that shows the installation as a whole has no device to name, so its address is the single segment of
/// its own: <see cref="PowerFlow"/>. There is one of each, which is why a constant is enough where a device needs
/// <see cref="For"/> and <see cref="Find"/>.
/// </para>
/// <para>
/// The two values are percent escaped with <see cref="Uri.EscapeDataString(string)"/>, which is what a browser expects:
/// a space becomes %20 and stays a space, it is never turned into an underscore. Do not use
/// <see cref="De.Hochstaetter.Fronius.Contracts.IHaveUniqueId.Id"/> for an address - that one does replace
/// spaces and slashes, which makes it unusable for a round trip.
/// </para>
/// </remarks>
public static class ViewPath
{
    /// <summary>The dashboard is the root of the app.</summary>
    public const string Dashboard = "/";

    /// <summary>
    /// The power flow page. Unlike a detail view it belongs to no device, so it is one address rather than a
    /// family of them.
    /// </summary>
    public const string PowerFlow = "/" + PowerFlowPage;

    private const string PowerFlowPage = "powerflow";

    private const string InverterView = "inverterdetails";
    private const string BatteryView = "batterydetails";
    private const string SmartMeterView = "smartmeterdetails";
    private const string WattPilotView = "wattpilotdetails";

    /// <summary>
    /// The address that shows <paramref name="device"/>, or the dashboard for a device without a detail view.
    /// </summary>
    public static string For(object? device)
    {
        if (Identify(device) is not { } identity)
        {
            return Dashboard;
        }

        return $"/{identity.View}/{Uri.EscapeDataString(identity.Manufacturer ?? string.Empty)}/{Uri.EscapeDataString(identity.SerialNumber ?? string.Empty)}";
    }

    /// <summary>
    /// Whether <paramref name="path"/> is the address of the power flow page. Case-insensitive, like every other
    /// address here, so a handwritten link works.
    /// </summary>
    public static bool IsPowerFlow(string? path) => Segments(path) is [var page] && string.Equals(page, PowerFlowPage, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The device of <paramref name="devices"/> that <paramref name="path"/> points at, or <see langword="null"/>
    /// where the path is the dashboard, malformed, or names a device this installation does not have.
    /// </summary>
    public static IKeyedDevice? Find(IEnumerable<IKeyedDevice> devices, string? path)
    {
        if (Parse(path) is not { } requested)
        {
            return null;
        }

        return devices.FirstOrDefault(device =>
            Identify(device.Device) is { } identity
            && string.Equals(requested.View, identity.View, StringComparison.OrdinalIgnoreCase)
            && string.Equals(requested.Manufacturer, identity.Manufacturer ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && string.Equals(requested.SerialNumber, identity.SerialNumber ?? string.Empty, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// The three parts of a path, unescaped, or <see langword="null"/> where it has a different shape. Comparing
    /// the unescaped values rather than the escaped ones keeps a handwritten link working, no matter whether the
    /// browser wrote %C3%A4 or %c3%a4.
    /// </summary>
    private static (string View, string Manufacturer, string SerialNumber)? Parse(string? path)
    {
        return Segments(path) is [var view, var manufacturer, var serialNumber]
            ? (view, Uri.UnescapeDataString(manufacturer), Uri.UnescapeDataString(serialNumber))
            : null;
    }

    /// <summary>The parts of a path, without the empty ones a leading or trailing slash produces.</summary>
    private static string[] Segments(string? path) => (path ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// The view of a device and the two values that identify it. Add a device type here and it has an address in
    /// both directions at once.
    /// </summary>
    private static (string View, string? Manufacturer, string? SerialNumber)? Identify(object? device) => device switch
    {
        Gen24System inverter => (InverterView, inverter.Manufacturer, inverter.SerialNumber),
        Gen24Storage storage => (BatteryView, storage.Manufacturer, storage.SerialNumber),
        Gen24PowerMeter3P smartMeter => (SmartMeterView, smartMeter.Manufacturer, smartMeter.SerialNumber),
        WattPilot wattPilot => (WattPilotView, wattPilot.Manufacturer, wattPilot.SerialNumber),
        _ => null,
    };
}
