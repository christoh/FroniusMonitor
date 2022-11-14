namespace De.Hochstaetter.FroniusMonitor.Models;

public class Settings : SettingsBase, ICloneable
{
    private static readonly object settingsLockObject = new();

    private Size? mainWindowSize;
    [DefaultValue(null),XmlElement("WindowSize")]
    public Size? MainWindowSize
    {
        get => mainWindowSize;
        set => Set(ref mainWindowSize, value);
    }

    public static async Task Save() => await Save(App.SettingsFileName).ConfigureAwait(false);

    public static async Task Save(string fileName) => await Task.Run(() =>
    {
        lock (settingsLockObject)
        {
            UpdateChecksum(App.Settings.WattPilotConnection, App.Settings.FritzBoxConnection, App.Settings.FroniusConnection);
            var serializer = new XmlSerializer(typeof(Settings));
            Directory.CreateDirectory(App.PerUserDataDir);
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);

            using var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = new string(' ', 3),
                NewLineChars = Environment.NewLine,
            });

            serializer.Serialize(writer, App.Settings);
        }
    }).ConfigureAwait(false);

    public static async Task Load(string fileName) => await Task.Run(() =>
    {
        lock (settingsLockObject)
        {
            try
            {
                App.SolarSystemQueryTimer = new(_ => { Environment.Exit(0); }, null, 2000, -1);
                var serializer = new XmlSerializer(typeof(Settings));
                using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                App.Settings = serializer.Deserialize(stream) as Settings ?? new Settings();
                ClearIncorrectPasswords(App.Settings.WattPilotConnection, App.Settings.FritzBoxConnection, App.Settings.FroniusConnection);
            }
            finally
            {
                App.SolarSystemQueryTimer?.Dispose();
            }
        }
    }).ConfigureAwait(false);

    public static async Task Load() => await Load(App.SettingsFileName).ConfigureAwait(false);

    public object Clone()
    {
        var clone = (Settings)MemberwiseClone();
        clone.FritzBoxConnection = (WebConnection)clone.FritzBoxConnection?.Clone()!;
        clone.FroniusConnection = (WebConnection)clone.FroniusConnection?.Clone()!;
        return clone;
    }

    private static void UpdateChecksum(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null).Apply(connection =>
        {
            connection!.PasswordChecksum = connection.CalculatedChecksum;
        });
    }

    private static void ClearIncorrectPasswords(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null && connection.PasswordChecksum != connection.CalculatedChecksum).Apply(connection => connection!.Password = string.Empty);
    }
}