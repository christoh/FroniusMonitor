namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>
/// Decides when the user is told about outdated firmware: once per component and offered version. The server pushes
/// the firmware status on every change and the client fetches it again after every reconnect, so the same status
/// arrives many times; the message box must not.
/// </summary>
public sealed class FirmwareUpdateNotice
{
    private readonly HashSet<(string DataSource, long Component, Version Version)> told = [];

    /// <summary>
    /// The components of <paramref name="status"/> that are outdated and have not been announced yet with the
    /// version they are offered. Empty when there is nothing new to say.
    /// </summary>
    public IReadOnlyList<SolarWebFirmwareComponent> Take(SolarWebFirmwareStatus status)
    {
        var fresh = new List<SolarWebFirmwareComponent>();

        foreach (var component in status.Components.Where(c => c.IsOutdated && c.UpdateVersion != null))
        {
            if (told.Add((component.DataSourceId, component.ComponentId, component.UpdateVersion!)))
            {
                fresh.Add(component);
            }
        }

        return fresh;
    }

    /// <summary>Forgets what was told: after a logout the next session starts with a clean slate.</summary>
    public void Reset() => told.Clear();

    /// <summary>One line of the message box: the component, the version it has and the version it is offered.</summary>
    public static string Describe(SolarWebFirmwareComponent component)
    {
        var installed = component.InstalledVersion?.ToSolarWebString() ?? "?";
        var offered = component.UpdateVersion?.ToSolarWebString() ?? "?";
        return $"{component.UpdateFamily}: {installed} → {offered}";
    }
}
