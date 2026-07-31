namespace De.Hochstaetter.Fronius.Models.Settings;

public abstract partial class SettingsBase : BindableBase, ICloneable
{
    public event EventHandler<EventArgs>? SettingsChanged;

    protected SettingsBase()
    {
        ElectricityPrice = new();
        FritzBoxConnection = new() { BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty };
        FroniusUpdateRate = 5;
        FroniusConnection = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };
        FroniusConnection2 = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };
        MaximumDnoLineCurrentPerPhase = 35;
        WattPilotConnection = new() { BaseUrl = "ws://192.168.178.YYY", Password = string.Empty };
        ToshibaHvacSessionTime = DateTime.MinValue;

        ToshibaAcConnection = new()
        {
            BaseUrl = "https://mobileapi.toshibahomeaccontrols.com",
            UserName = string.Empty,
            Password = string.Empty,
            Protocol = Protocol.Amqp,
            TunnelMode = TunnelMode.Auto,
        };

        AzureDeviceId = Guid.NewGuid();
    }

    [XmlElement, DefaultValue(null), ObservableProperty]
    public partial ToshibaHvacSession? ToshibaHvacSession { get; set; }

    [XmlAttribute, ObservableProperty, DefaultValue(typeof(DateTime),default)]
    public partial DateTime ToshibaHvacSessionTime { get; set; }

    //[XmlElement, DefaultValue(null), ObservableProperty]
    //public partial ToshibaHvacAzureCredentials? ToshibaHvacAzureCredentials { get; set; }

    [XmlElement, ObservableProperty]
    public partial ElectricityPriceSettings ElectricityPrice { get; set; }

    [XmlElement, DefaultValue(null), ObservableProperty]
    public partial WebConnection FritzBoxConnection { get; set; }

    [DefaultValue((byte)5), ObservableProperty]
    public partial byte FroniusUpdateRate { get; set; }

    [DefaultValue(null), ObservableProperty]
    public partial string? DriftFileName { get; set; }

    [DefaultValue(null), ObservableProperty]
    public partial string? EnergyHistoryFileName { get; set; }

    [DefaultValue(null), ObservableProperty]
    public partial AwattarParameters? Awattar { get; set; }

    [XmlElement, DefaultValue(null), ObservableProperty]
    public partial WebConnection FroniusConnection { get; set; }

    [XmlElement, DefaultValue(null), ObservableProperty]
    public partial WebConnection FroniusConnection2 { get; set; }

    [XmlAttribute, DefaultValue(35d), ObservableProperty, NotifyPropertyChangedFor(nameof(MaximumDnoLineCurrentTotal))]
    public partial double MaximumDnoLineCurrentPerPhase { get; set; }

    [XmlAttribute, DefaultValue(false), ObservableProperty]
    public partial bool ColorAllGaugeTicks { get; set; }

    [XmlIgnore] public double MaximumDnoLineCurrentTotal => MaximumDnoLineCurrentPerPhase * 3;

    [XmlElement, DefaultValue(null), ObservableProperty]
    public partial WebConnection WattPilotConnection { get; set; }

    [XmlAttribute, DefaultValue(null), ObservableProperty]
    public partial string? Language { get; set; }

    [XmlAttribute, ObservableProperty]
    public partial bool ShowFritzBox { get; set; }

    [XmlAttribute, ObservableProperty]
    public partial bool HaveWattPilot { get; set; }

    [XmlAttribute, ObservableProperty]
    public partial bool HaveTwoInverters { get; set; }

    [XmlAttribute, ObservableProperty]
    public partial bool ShowWattPilot { get; set; }

    [XmlAttribute, ObservableProperty]
    public partial bool HaveFritzBox { get; set; }

    [XmlAttribute, ObservableProperty]
    public partial bool HaveToshibaAc { get; set; }

    [XmlAttribute, ObservableProperty]
    public partial bool ShowToshibaAc { get; set; }

    [XmlElement, ObservableProperty]
    public partial AzureConnection ToshibaAcConnection { get; set; }

    [XmlIgnore, ObservableProperty]
    public partial Guid AzureDeviceId { get; set; }

    [XmlElement(nameof(AzureDeviceId))]
    public string AzureDeviceIdString
    {
        get => AzureDeviceId.ToString("D");
        set => AzureDeviceId = Guid.Parse(value, CultureInfo.InvariantCulture);
    }

    [XmlElement, DefaultValue(false), ObservableProperty]
    public partial bool AddInverterPowerToConsumption { get; set; }

    public abstract Task Save();

    protected static void UpdateChecksum(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null).Apply(connection => { connection!.PasswordChecksum = connection.CalculatedChecksum; });
    }

    protected static void ClearIncorrectPasswords(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null && connection.PasswordChecksum != connection.CalculatedChecksum).Apply(connection => connection!.Password = string.Empty);
    }
    
    public object Clone()
    {
        var clone = (SettingsBase)MemberwiseClone();

        foreach (var propertyInfo in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(p => p is { CanRead: true, CanWrite: true } && p.PropertyType.GetInterface(nameof(ICloneable)) is not null))
        {
            var value = propertyInfo.GetValue(clone);

            if (value != null)
            {
                var cloneMethod = propertyInfo.PropertyType.GetMethod(nameof(ICloneable.Clone));
                propertyInfo.SetValue(clone, cloneMethod?.Invoke(value, null));
            }
        }

        return clone;
    }

    public void CopyFrom(SettingsBase other)
    {
        var otherType = other.GetType();

        if (!GetType().IsAssignableFrom(otherType))
        {
            throw new ArgumentException($"{GetType().Name} is not assignable from {otherType}");
        }

        foreach (var propertyInfo in other.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(p => p is { CanRead: true, CanWrite: true }))
        {
            var value = propertyInfo.GetValue(other);

            if (value is null || propertyInfo.PropertyType.GetInterface(nameof(ICloneable)) == null)
            {
                propertyInfo.SetValue(this, value);
            }
            else
            {
                var cloneMethod = propertyInfo.PropertyType.GetMethod(nameof(ICloneable.Clone));
                propertyInfo.SetValue(this, cloneMethod?.Invoke(value, null));
            }
        }
    }

    public void NotifySettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
}
