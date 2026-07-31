using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.FroniusMonitor.Models;

public class Settings : SettingsBase
{
    private static readonly Lock settingsLock = new();

    [DefaultValue(null), XmlElement("WindowSize")]
    public Size? MainWindowSize
    {
        get;
        set => Set(ref field, value);
    }

    [DefaultValue(375), XmlElement("ControllerGridRowHeight")]
    public double ControllerGridRowHeight
    {
        get;
        set => Set(ref field, value);
    } = 375;

    [DefaultValue(false), XmlAttribute("ShowRibbon")]
    public bool ShowRibbon
    {
        get;
        set => Set(ref field, value);
    }

    [DefaultValue(null)]
    public string? CustomSolarPanelLayout
    {
        get;
        set => Set(ref field, value);
    }

    public override Task Save() => Save(this, App.SettingsFileName);

    public static Task Save(string fileName) => Save(App.Settings, fileName);

    private static Task Save(Settings settings, string fileName) => Task.Run(() =>
    {
        lock (settingsLock)
        {
            UpdateChecksum(settings.WattPilotConnection, settings.FritzBoxConnection, settings.FroniusConnection, settings.FroniusConnection2, settings.ToshibaAcConnection);
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

            serializer.Serialize(writer, settings);
        }
    });

    public static Task Load(string fileName) => Task.Run(() =>
    {
        lock (settingsLock)
        {
            try
            {
                App.SolarSystemQueryTimer = new(_ => { Environment.Exit(0); }, null, 10000, -1);
                var serializer = new XmlSerializer(typeof(Settings));
                using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var loaded = serializer.Deserialize(stream) as Settings ?? new Settings();

                if (App.Settings is null)
                {
                    App.Settings = loaded;
                }
                else
                {
                    App.Settings.CopyFrom(loaded);
                }

                ClearIncorrectPasswords(App.Settings.WattPilotConnection, App.Settings.FritzBoxConnection, App.Settings.FroniusConnection);
            }
            finally
            {
                App.SolarSystemQueryTimer?.Dispose();
            }
        }
    });

    public static Task Load() => Load(App.SettingsFileName);
}