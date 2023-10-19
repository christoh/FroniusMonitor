namespace De.Hochstaetter.Fronius.Models.Settings;

public class FritzBoxParameters
{
    public IEnumerable<WebConnection>? Connections { get; set; }
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromSeconds(5);
}
