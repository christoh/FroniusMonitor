using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class Settings : SettingsBase
{
    //[DefaultValue("0.0.0.0")]
    public string ServerIpAddress { get; set; } = "0.0.0.0";

    //[DefaultValue((ushort)1502)]
    public ushort ServerPort { get; set; } = 1502;

    public List<WebConnection> FritzBoxConnections = new();

    public List<ModbusMapping> ModbusMappings = new();

    [XmlIgnore] public static string SettingsFileName { get; set; } = Path.Combine(AppContext.BaseDirectory, "Settings.xml");

    public static async Task<Settings> LoadAsync(string? fileName = null) => await Task.Run(() => Load<Settings>(fileName ?? SettingsFileName));

    public async Task SaveAsync(string? fileName = null) => await Task.Run(() => Save<Settings>(this, fileName ?? SettingsFileName));
}
