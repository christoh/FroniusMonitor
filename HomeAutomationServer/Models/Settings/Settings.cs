using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class Settings
{
    private static readonly Lock settingLock = new();

    //[DefaultValue("0.0.0.0")]
    public string ServerIpAddress { get; set; } = "::";

    //[DefaultValue((ushort)1502)]
    public ushort ServerPort { get; set; } = 1502;

    public List<WebConnection> FritzBoxConnections = [];

    public List<WebConnection> WattPilotConnections = [];

    public List<ModbusMapping> ModbusMappings = [];

    public List<ModbusConnection> SunSpecClients = [];

    public List<WebConnection> Gen24Connections = [];

    /// <summary>
    /// How <see cref="Services.DataCollectors.Gen24DataCollector"/> polls every inverter in <see cref="Gen24Connections"/>:
    /// <see cref="Gen24DataCollectorParameters.RefreshRate"/>, <see cref="Gen24DataCollectorParameters.ConfigRefreshRate"/>
    /// and <see cref="Gen24DataCollectorParameters.LogDirectory"/>. <see cref="Gen24DataCollectorParameters.Connections"/>
    /// is not part of this element - it comes from <see cref="Gen24Connections"/> instead.
    /// </summary>
    public Gen24DataCollectorParameters Gen24DataCollector = new();

    public HashSet<User> Users = [];

    /// <summary>
    /// Whether anyone may log in as the built-in <see cref="User.Guest"/> with the password <c>guest</c> and see
    /// what a <see cref="Roles.Guest"/> sees. No <c>[DefaultValue]</c> on purpose: the element is written to
    /// <c>Settings.xml</c> whatever it says, so that a server's answer to this is in the file rather than implied
    /// by its absence.
    /// </summary>
    public bool EnableGuestAccount { get; set; } = true;

    public WebServerSettings WebServerSettings = new WebServerSettings();

    /// <summary>The Toshiba account, or <see langword="null" /> when the server has no air conditioners to collect.</summary>
    [XmlElement, DefaultValue(null)]
    public ToshibaHvacSettings? ToshibaHvac { get; set; }

    /// <summary>The sources of the price chart, or <see langword="null" /> when the server collects no prices and no weather.</summary>
    [XmlElement, DefaultValue(null)]
    public EnergyDataSettings? EnergyData { get; set; }

    /// <summary>The Fronius Solar.web account and PV system, or <see langword="null" /> when the server serves no Solar.web charts.</summary>
    [XmlElement, DefaultValue(null)]
    public SolarWebSettings? SolarWeb { get; set; }

    [XmlIgnore] public static string SettingsFileName { get; set; } = Path.Combine(AppContext.BaseDirectory, "Settings.xml");

    public static Task<Settings> LoadAsync(string? fileName = null, CancellationToken token = default) => Task.Run(() => Load(fileName), token);

    public static Settings Load(string? fileName = null)
    {
        lock (settingLock)
        {
            fileName ??= SettingsFileName;
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var serializer = new XmlSerializer(typeof(Settings));
            return serializer.Deserialize(stream) as Settings ?? throw new SerializationException();
        }
    }

    public Task SaveAsync(string? fileName = null, CancellationToken token = default) => Task.Run(() => Save(fileName), token);

    public void Save(string? fileName = null)
    {
        lock (settingLock)
        {
            fileName ??= SettingsFileName;
            UpdateChecksum([.. FritzBoxConnections, ToshibaHvac, SolarWeb]);
            var serializer = new XmlSerializer(typeof(Settings));
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);

            using var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = new string(' ', 3),
                NewLineChars = Environment.NewLine,
                Async = true,
            });

            serializer.Serialize(writer, this);
        }
    }

    private static void UpdateChecksum(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null).Apply(connection => { connection!.PasswordChecksum = connection.CalculatedChecksum; });
    }
}
