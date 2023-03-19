using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.FroniusMonitor.Models;

public class Settings : SettingsBase
{
    private static readonly object settingsLockObject = new();

    private Size? mainWindowSize;
    [DefaultValue(null),XmlElement("WindowSize")]
    public Size? MainWindowSize
    {
        get => mainWindowSize;
        set => Set(ref mainWindowSize, value);
    }

    private double controllerGridRowHeight = 375;
    [DefaultValue(375), XmlElement("ControllerGridRowHeight")]
    public double ControllerGridRowHeight
    {
        get => controllerGridRowHeight;
        set => Set(ref controllerGridRowHeight, value);
    }

    public static Task Save() => Save(App.SettingsFileName);

    public static async Task Save(string fileName) => await Task.Run(() =>
    {
        lock (settingsLockObject)
        {
            UpdateChecksum(App.Settings.WattPilotConnection, App.Settings.FritzBoxConnection, App.Settings.FroniusConnection, App.Settings.ToshibaAcConnection);
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
                App.SolarSystemQueryTimer = new(_ => { Environment.Exit(0); }, null, 10000, -1);
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

    public static Task Load() => Load(App.SettingsFileName);
}