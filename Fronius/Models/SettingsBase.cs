﻿namespace De.Hochstaetter.Fronius.Models;

public abstract class SettingsBase : BindableBase
{
    private WebConnection? fritzBoxConnection = new() {BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty};

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

    private byte toshibaAcUpdateRate = 5;

    [DefaultValue((byte)5)]
    public byte ToshibaAcUpdateRate
    {
        get => toshibaAcUpdateRate;
        set => Set(ref toshibaAcUpdateRate, value);
    }

    private WebConnection? froniusConnection = new() {BaseUrl = "http://192.168.178.XXX", UserName = string.Empty, Password = string.Empty};

    [XmlElement, DefaultValue(null)]
    public WebConnection? FroniusConnection
    {
        get => froniusConnection;
        set => Set(ref froniusConnection, value);
    }

    private WebConnection? wattPilotConnection = new() {BaseUrl = "ws://192.168.178.YYY", Password = string.Empty};

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
                FritzBoxConnection ??= new WebConnection {BaseUrl = "http://192.168.178.1", UserName = string.Empty, Password = string.Empty};
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
                ToshibaAcConnection ??= new WebConnection {BaseUrl = "https://mobileapi.toshibahomeaccontrols.com", UserName = string.Empty, Password = string.Empty};
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

    private WebConnection? toshibaAcConnection;

    [XmlElement, DefaultValue(null)]
    public WebConnection? ToshibaAcConnection
    {
        get => toshibaAcConnection;
        set => Set(ref toshibaAcConnection, value);
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

    protected static void UpdateChecksum(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null).Apply(connection => { connection!.PasswordChecksum = connection.CalculatedChecksum; });
    }

    protected static void ClearIncorrectPasswords(params WebConnection?[] connections)
    {
        connections.Where(connection => connection != null && connection.PasswordChecksum != connection.CalculatedChecksum).Apply(connection => connection!.Password = string.Empty);
    }
}
