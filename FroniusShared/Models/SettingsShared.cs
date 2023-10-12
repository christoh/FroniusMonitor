namespace De.Hochstaetter.FroniusShared.Models;

public abstract class SettingsShared : SettingsBase
{
    private WebConnection fritzBoxConnection = new() { BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection FritzBoxConnection
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

    private WebConnection froniusConnection = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection FroniusConnection
    {
        get => froniusConnection;
        set => Set(ref froniusConnection, value);
    }

    private WebConnection froniusConnection2 = new() { BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection FroniusConnection2
    {
        get => froniusConnection2;
        set => Set(ref froniusConnection2, value);
    }

    private WebConnection wattPilotConnection = new() { BaseUrl = "ws://192.168.178.YYY", Password = string.Empty };

    [XmlElement, DefaultValue(null)]
    public WebConnection WattPilotConnection
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
        set => Set(ref haveFritzBox, value);
    }

    private bool haveToshibaAc;

    [XmlAttribute]
    public bool HaveToshibaAc
    {
        get => haveToshibaAc;
        set => Set(ref haveToshibaAc, value);
    }

    private bool showToshibaAc;

    [XmlAttribute]
    public bool ShowToshibaAc
    {
        get => showToshibaAc;
        set => Set(ref showToshibaAc, value);
    }

    private AzureConnection toshibaAcConnection = new AzureConnection
    {
        BaseUrl = "https://mobileapi.toshibahomeaccontrols.com",
        UserName = string.Empty,
        Password = string.Empty,
        Protocol = Protocol.Amqp,
        TunnelMode = TunnelMode.Auto
    };

    [XmlElement]
    public AzureConnection ToshibaAcConnection
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
}
