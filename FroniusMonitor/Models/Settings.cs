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

    private WebConnection? froniusConnection = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty};
    [XmlElement, DefaultValue(null)]
    public WebConnection? FroniusConnection
    {
        get => froniusConnection;
        set => Set(ref froniusConnection, value);
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
        }
    }).ConfigureAwait(false);

    public static async Task Load() => await Load(App.SettingsFileName).ConfigureAwait(false);

    public object Clone()
    {
        var clone=(Settings)MemberwiseClone();
        clone.FritzBoxConnection = (WebConnection)clone.FritzBoxConnection?.Clone()!;
        clone.FroniusConnection= (WebConnection)clone.FroniusConnection?.Clone()!;
        return clone;
    }
}