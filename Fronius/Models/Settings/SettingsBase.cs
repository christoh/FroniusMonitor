namespace De.Hochstaetter.Fronius.Models.Settings;

public abstract class SettingsBase : BindableBase, ICloneable
{
    private WebConnection? fritzBoxConnection = new() { BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection? FritzBoxConnection
    {
        get => fritzBoxConnection;
        set => Set(ref fritzBoxConnection, value);
    }

    private byte froniusUpdateRate = 5;

    [DefaultValue((byte)5)]
    public byte FroniusUpdateRate
    {
        get => froniusUpdateRate;
        set => Set(ref froniusUpdateRate, value);
    }

    private string? driftFileName;

    [DefaultValue(null)]
    public string? DriftFileName
    {
        get => driftFileName;
        set => Set(ref driftFileName, value);
    }

    private WebConnection? froniusConnection = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection? FroniusConnection
    {
        get => froniusConnection;
        set => Set(ref froniusConnection, value);
    }

    private WebConnection? froniusConnection2 = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection? FroniusConnection2
    {
        get => froniusConnection2;
        set => Set(ref froniusConnection2, value);
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

    private bool haveTwoInverters;
    [XmlAttribute]
    public bool HaveTwoInverters
    {
        get => haveTwoInverters;
        set => Set(ref haveTwoInverters, value);
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

    private bool haveToshibaAc;

    [XmlAttribute]
    public bool HaveToshibaAc
    {
        get => haveToshibaAc;
        set => Set(ref haveToshibaAc, value, () =>
        {
            if (value)
            {
                ToshibaAcConnection ??= new AzureConnection
                {
                    BaseUrl = "https://mobileapi.toshibahomeaccontrols.com",
                    UserName = string.Empty,
                    Password = string.Empty,
                    Protocol = Protocol.Amqp,
                    TunnelMode = TunnelMode.Auto
                };
            }
        });
    }

    private bool showToshibaAc;

    [XmlAttribute]
    public bool ShowToshibaAc
    {
        get => showToshibaAc;
        set => Set(ref showToshibaAc, value);
    }

    private AzureConnection? toshibaAcConnection;

    [XmlElement, DefaultValue(null)]
    public AzureConnection? ToshibaAcConnection
    {
        get => toshibaAcConnection;
        set => Set(ref toshibaAcConnection, value);
    }

    private Guid azureDeviceId = Guid.NewGuid();

    [XmlIgnore]
    public Guid AzureDeviceId
    {
        get => azureDeviceId;
        set => Set(ref azureDeviceId, value);
    }

    [XmlElement(nameof(AzureDeviceId))]
    public string AzureDeviceIdString
    {
        get => AzureDeviceId.ToString("D");
        set => AzureDeviceId = Guid.Parse(value, CultureInfo.InvariantCulture);
    }

    private bool addInverterPowerToConsumption;

    [XmlElement, DefaultValue(false)]
    public bool AddInverterPowerToConsumption
    {
        get => addInverterPowerToConsumption;
        set => Set(ref addInverterPowerToConsumption, value);
    }

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
}
