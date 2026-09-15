namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public abstract class PolledWebConnectionParameterBase
{
    public IEnumerable<WebConnection>? Connections { get; set; }
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromSeconds(5);
}
