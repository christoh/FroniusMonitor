namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class ModbusClientParameters
{
    public List<ModbusConnection>? ModbusConnections { get; set; }
    public TimeSpan RefreshRate { get; set; }
}