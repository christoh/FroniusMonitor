using De.Hochstaetter.Fronius.Extensions;

namespace De.Hochstaetter.FroniusMonitor.Models;

public class Settings : BindableBase, ICloneable
{
    private static readonly object settingsLockObject = new();
    private WebConnection? fritzBoxConnection = new() { BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty };
    [XmlElement, DefaultValue(null)]
    public WebConnection? FritzBoxConnection
    {
        get => fritzBoxConnection;
        set => Set(ref fritzBoxConnection, value);
    }

    private WebConnection? froniusConnection = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };
    [XmlElement, DefaultValue(null)]
    public WebConnection? FroniusConnection
    {
        get => froniusConnection;
        set => Set(ref froniusConnection, value);
    }

    private WebConnection? wattPilotConnection = new() { BaseUrl = "ws://192.168.178.YYY", Password = string.Empty };
    [XmlElement, DefaultValue(null)]
    public WebConnection? WattPilotConnection
    {
        get => wattPilotConnection;
        set => Set(ref wattPilotConnection, value);
    }

    private string? language;
    [XmlAttribute, DefaultValue(null)]
    public string? Language
    {
        get => language;
        set => Set(ref language, value);
    }

    private bool showFritzBox;
    [XmlAttribute]
    public bool ShowFritzBox
    {
        get => showFritzBox;
        set => Set(ref showFritzBox, value);
    }

    private bool haveWattPilot;
    [XmlAttribute]
    public bool HaveWattPilot
    {
        get => haveWattPilot;
        set => Set(ref haveWattPilot, value);
    }

    private bool showWattPilot;
    [XmlAttribute]
    public bool ShowWattPilot
    {
        get => showWattPilot;
        set => Set(ref showWattPilot, value);
    }

    private bool haveFritzBox;
    [XmlAttribute]
    public bool HaveFritzBox
    {
        get => haveFritzBox;
        set => Set(ref haveFritzBox, value, () =>
        {
            if (value)
            {
                FritzBoxConnection ??= new WebConnection { BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty };
            }
        });
    }

    private bool addInverterPowerToConsumption;
    [XmlElement, DefaultValue(false)]
    public bool AddInverterPowerToConsumption
    {
        get => addInverterPowerToConsumption;
        set => Set(ref addInverterPowerToConsumption, value);
    }

    private double consumedEnergyOffsetWattHours;

    [XmlElement]
    public double ConsumedEnergyOffsetWattHours
    {
        get => consumedEnergyOffsetWattHours;
        set => Set(ref consumedEnergyOffsetWattHours, value);
    }

    private double producedEnergyOffsetWattHours;

    [XmlElement]
    public double ProducedEnergyOffsetWattHours
    {
        get => producedEnergyOffsetWattHours;
        set => Set(ref producedEnergyOffsetWattHours, value);
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
            var serializer = new XmlSerializer(typeof(Settings));
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            App.Settings = (serializer.Deserialize(stream) as Settings) ?? new Settings();
            ClearIncorrectPasswords(App.Settings.WattPilotConnection, App.Settings.FritzBoxConnection, App.Settings.FroniusConnection);
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