using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class Settings
{
    private static readonly object settingLock = new();

    //[DefaultValue("0.0.0.0")]
    public string ServerIpAddress { get; set; } = "::";

    //[DefaultValue((ushort)1502)]
    public ushort ServerPort { get; set; } = 1502;

    public List<WebConnection> FritzBoxConnections = new();

    public List<ModbusMapping> ModbusMappings = new();

    public List<ModbusConnection> SunSpecClients = new();

    public List<WebConnection> Gen24Connections = new();

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
            UpdateChecksum([.. FritzBoxConnections]);
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
