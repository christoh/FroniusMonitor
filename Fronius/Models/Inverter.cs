namespace De.Hochstaetter.Fronius.Models;

public class ThreePhasesData : BaseResponse
{
    private double? l1Voltage;

    public double? L1Voltage
    {
        get => l1Voltage;
        set => Set(ref l1Voltage, value, () => NotifyOfPropertyChange(nameof(L1PowerWatts)));
    }

    private double? l2Voltage;

    public double? L2Voltage
    {
        get => l2Voltage;
        set => Set(ref l2Voltage, value, () => NotifyOfPropertyChange(nameof(L2PowerWatts)));
    }

    private double? l3Voltage;

    public double? L3Voltage
    {
        get => l3Voltage;
        set => Set(ref l3Voltage, value, () => NotifyOfPropertyChange(nameof(L3PowerWatts)));
    }

    private double? l1Current;

    public double? L1Current
    {
        get => l1Current;
        set => Set(ref l1Current, value, () => NotifyOfPropertyChange(nameof(L1PowerWatts)));
    }

    private double? l2Current;

    public double? L2Current
    {
        get => l2Current;
        set => Set(ref l2Current, value, () => NotifyOfPropertyChange(nameof(L2PowerWatts)));
    }

    private double? l3Current;

    public double? L3Current
    {
        get => l3Current;
        set => Set(ref l3Current, value, () => NotifyOfPropertyChange(nameof(L3PowerWatts)));
    }

    public double? L1PowerWatts => L1Voltage * L1Current;
    public double? L2PowerWatts => L2Voltage * L2Current;
    public double? L3PowerWatts => L3Voltage * L3Current;
}

public class InverterData : EnergyCounterBase
{
    private InverterStatus status;

    public InverterStatus Status
    {
        get => status;
        set => Set(ref status, value, () => NotifyOfPropertyChange(nameof(InverterStatusString)));
    }

    public string InverterStatusString => Status.ToDisplayName();

    private double? currentString1;

    public double? CurrentString1
    {
        get => currentString1;

        set => Set(ref currentString1, value, () =>
        {
            NotifyOfPropertyChange(nameof(String1PowerWatts));
            NotifyOfPropertyChange(nameof(SolarPowerWatts));
        });
    }

    private double? currentString2;

    public double? CurrentString2
    {
        get => currentString2;

        set => Set(ref currentString2, value, () =>
        {
            NotifyOfPropertyChange(nameof(String2PowerWatts));
            NotifyOfPropertyChange(nameof(SolarPowerWatts));
        });
    }

    private double? currentString3;

    public double? CurrentString3
    {
        get => currentString3;

        set => Set(ref currentString3, value, () =>
        {
            NotifyOfPropertyChange(nameof(String3PowerWatts));
            NotifyOfPropertyChange(nameof(SolarPowerWatts));
        });
    }

    private double? voltageString1;

    public double? VoltageString1
    {
        get => voltageString1;

        set => Set(ref voltageString1, value, () =>
        {
            NotifyOfPropertyChange(nameof(String1PowerWatts));
            NotifyOfPropertyChange(nameof(SolarPowerWatts));
        });
    }

    private double? voltageString2;

    public double? VoltageString2
    {
        get => voltageString2;

        set => Set(ref voltageString2, value, () =>
        {
            NotifyOfPropertyChange(nameof(String2PowerWatts));
            NotifyOfPropertyChange(nameof(SolarPowerWatts));
        });
    }

    private double? voltageString3;

    public double? VoltageString3
    {
        get => voltageString3;

        set => Set(ref voltageString3, value, () =>
        {
            NotifyOfPropertyChange(nameof(String3PowerWatts));
            NotifyOfPropertyChange(nameof(SolarPowerWatts));
        });
    }

    public double? String1PowerWatts => CurrentString1 * VoltageString1;
    public double? String2PowerWatts => CurrentString2 * VoltageString2;
    public double? String3PowerWatts => CurrentString3 * VoltageString3;
    public double? SolarPowerWatts => !String1PowerWatts.HasValue && !String2PowerWatts.HasValue && !String3PowerWatts.HasValue ? null : (String1PowerWatts ?? 0) + (String2PowerWatts ?? 0) + (String3PowerWatts ?? 0);


    private double? totalVoltage;

    public double? TotalVoltage
    {
        get => totalVoltage;
        set => Set(ref totalVoltage, value);
    }

    private double? totalCurrent;

    public double? TotalCurrent
    {
        get => totalCurrent;
        set => Set(ref totalCurrent, value);
    }


    private double? frequency;

    public double? Frequency
    {
        get => frequency;
        set => Set(ref frequency, value);
    }

    private double? acPowerWatts;

    public double? AcPowerWatts
    {
        get => acPowerWatts;
        set => Set(ref acPowerWatts, value);
    }

    private double? absolutePowerToLoad;

    public double? AbsolutePowerToLoad
    {
        get => absolutePowerToLoad;
        set => Set(ref absolutePowerToLoad, value);
    }

    private int errorCode;

    public int ErrorCode
    {
        get => errorCode;
        set => Set(ref errorCode, value);
    }
}

public class Inverter : DeviceInfo
{
    public Inverter()
    {
        DeviceClass = DeviceClass.Inverter;
    }

    public string? CustomName { get; init; } = string.Empty;
    public double MaxPvPowerWatts { get; init; }
    public bool Show { get; init; }
    public override string DisplayName => !string.IsNullOrWhiteSpace(CustomName) ? $"{CustomName} ({Model} #{Id})" : $"{Model} #{Id}";

    private InverterData? data;

    public InverterData? Data
    {
        get => data;
        set => Set(ref data, value);
    }

    private ThreePhasesData? threePhasesData;

    public ThreePhasesData? ThreePhasesData
    {
        get => threePhasesData;
        set => Set(ref threePhasesData, value);
    }
}