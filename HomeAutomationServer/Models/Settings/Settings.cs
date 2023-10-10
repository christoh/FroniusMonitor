using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class Settings
{
    private static int settingLock;

    //[DefaultValue("0.0.0.0")]
    public string ServerIpAddress { get; set; } = "0.0.0.0";

    //[DefaultValue((ushort)1502)]
    public ushort ServerPort { get; set; } = 1502;

    public List<WebConnection> FritzBoxConnections = new();

    public List<ModbusMapping> ModbusMappings = new();

    [XmlIgnore] public static string SettingsFileName { get; set; } = Path.Combine(AppContext.BaseDirectory, "Settings.xml");

    public static async Task<Settings> LoadAsync(string? fileName = null)
    {
        while (Interlocked.Exchange(ref settingLock, 1) != 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20)).ConfigureAwait(false);
        }

        try
        {
            fileName ??= SettingsFileName;
            await using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var serializer = new XmlSerializer(typeof(Settings));
            // ReSharper disable once AccessToDisposedClosure
            return await Task.Run(() => serializer.Deserialize(stream) as Settings ?? throw new SerializationException()).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref settingLock, 0);
        }
    }

    public static Settings Load(string? fileName = null) => LoadAsync(fileName).GetAwaiter().GetResult();

    public async Task SaveAsync(string? fileName = null)
    {
        while (Interlocked.Exchange(ref settingLock, 1) != 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20)).ConfigureAwait(false);
        }

        try
        {
            fileName ??= SettingsFileName;
            UpdateChecksum(FritzBoxConnections.ToArray());
            var serializer = new XmlSerializer(typeof(Settings));
            await using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);

            await using var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = new string(' ', 3),
                NewLineChars = Environment.NewLine,
                Async = true,
            });

            serializer.Serialize(writer, this);
        }
        finally
        {
            Interlocked.Exchange(ref settingLock, 0);
        }
    }

    public void Save(string? fileName = null) => SaveAsync(fileName).GetAwaiter().GetResult();

    private static void UpdateChecksum(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null).Apply(connection => { connection!.PasswordChecksum = connection.CalculatedChecksum; });
    }
}
