namespace De.Hochstaetter.Fronius.Models.Settings;

public abstract class SettingsBase : BindableBase, ICloneable
{
    public event EventHandler<EventArgs>? SettingsChanged;

    [XmlElement]
    public ElectricityPriceSettings ElectricityPrice
    {
        get;
        set => Set(ref field, value);
    } = new();

    [XmlElement, DefaultValue(null)]
    public WebConnection FritzBoxConnection
    {
        get;
        set => Set(ref field, value);
    } = new() { BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty };

    [DefaultValue((byte)5)]
    public byte FroniusUpdateRate
    {
        get;
        set => Set(ref field, value);
    } = 5;

    [DefaultValue(null)]
    public string? DriftFileName
    {
        get;
        set => Set(ref field, value);
    }

    [DefaultValue(null)]
    public string? EnergyHistoryFileName
    {
        get;
        set => Set(ref field, value);
    }

    [DefaultValue(null)]
    public AwattarParameters? Awattar
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement, DefaultValue(null)]
    public WebConnection FroniusConnection
    {
        get;
        set => Set(ref field, value);
    } = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection FroniusConnection2
    {
        get;
        set => Set(ref field, value);
    } = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };

    [XmlAttribute, DefaultValue(35d)]
    public double MaximumDnoLineCurrentPerPhase
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(MaximumDnoLineCurrentTotal)));
    } = 35;

    [XmlAttribute, DefaultValue(false)]
    public bool ColorAllGaugeTicks
    {
        get;
        set => Set(ref field, value);
    }

    [XmlIgnore] public double MaximumDnoLineCurrentTotal => MaximumDnoLineCurrentPerPhase * 3;

    [XmlElement, DefaultValue(null)]
    public WebConnection WattPilotConnection
    {
        get;
        set => Set(ref field, value);
    } = new() { BaseUrl = "ws://192.168.178.YYY", Password = string.Empty };

    [XmlAttribute, DefaultValue(null)]
    public string? Language
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute]
    public bool ShowFritzBox
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute]
    public bool HaveWattPilot
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute]
    public bool HaveTwoInverters
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute]
    public bool ShowWattPilot
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute]
    public bool HaveFritzBox
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute]
    public bool HaveToshibaAc
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute]
    public bool ShowToshibaAc
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement]
    public AzureConnection ToshibaAcConnection
    {
        get;
        set => Set(ref field, value);
    } = new()
    {
        BaseUrl = "https://mobileapi.toshibahomeaccontrols.com",
        UserName = string.Empty,
        Password = string.Empty,
        Protocol = Protocol.Amqp,
        TunnelMode = TunnelMode.Auto
    };

    [XmlIgnore]
    public Guid AzureDeviceId
    {
        get;
        set => Set(ref field, value);
    } = Guid.NewGuid();

    [XmlElement(nameof(AzureDeviceId))]
    public string AzureDeviceIdString
    {
        get => AzureDeviceId.ToString("D");
        set => AzureDeviceId = Guid.Parse(value, CultureInfo.InvariantCulture);
    }

    [XmlElement, DefaultValue(false)]
    public bool AddInverterPowerToConsumption
    {
        get;
        set => Set(ref field, value);
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

    public void NotifySettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
}
