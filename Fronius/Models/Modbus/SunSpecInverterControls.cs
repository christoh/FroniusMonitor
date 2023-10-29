namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecInverterControls : SunSpecModelBase
{
    public SunSpecInverterControls(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 123, };

    public override ushort MinimumDataLength => 24;

    [Modbus(0)]
    public ushort ConnectDisconnectTimeWindow
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(1)]
    public ushort ConnectDisconnectTimeOut
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(2)]
    public SunSpecConnectionState ConnectionState
    {
        get => Get<SunSpecConnectionState>();
        set => Set(value);
    }

    [Modbus(3)]
    public ushort RelativeActivePowerLimitI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(21)]
    public short RelativeActivePowerLimitSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? RelativeActivePowerLimit
    {
        get => ToDouble(RelativeActivePowerLimitI, RelativeActivePowerLimitSf) / 100;
        set => RelativeActivePowerLimitI = FromDouble<ushort>(value * 100, RelativeActivePowerLimitSf);
    }

    [Modbus(4)]
    public ushort ActivePowerLimitTimeWindow
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(5)]
    public ushort ActivePowerLimitTimeOut
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(6)]
    public ushort ActivePowerLimitRampTime
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(7)]
    public SunSpecOnOff ActivePowerLimitEnabled
    {
        get => Get<SunSpecOnOff>();
        set => Set(value);
    }

    [Modbus(8)]
    public short PowerFactorI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(22)]
    public short PowerFactorSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? PowerFactor
    {
        get => ToDouble(PowerFactorI, PowerFactorSf);
        set => PowerFactorI = FromDouble<short>(value, PowerFactorSf);
    }

    [Modbus(9)]
    public ushort PowerFactorTimeWindow
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(10)]
    public ushort PowerFactorTimeOut
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(11)]
    public ushort PowerFactorRampTime
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(12)]
    public SunSpecOnOff PowerFactorEnabled
    {
        get => Get<SunSpecOnOff>();
        set => Set(value);
    }

    [Modbus(13)]
    public short ReactivePowerRelativeToMaxActivePowerI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(14)]
    public short ReactivePowerRelativeToMaxReactivePowerI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(15)]
    public short ReactivePowerRelativeToAvailableReactivePowerI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(23)]
    public short ReactivePowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? ReactivePowerRelativeToMaxActivePower
    {
        get => ToDouble(ReactivePowerRelativeToMaxActivePowerI, ReactivePowerSf) / 100;
        set => ReactivePowerRelativeToMaxActivePowerI = FromDouble<short>(value * 100, ReactivePowerSf);
    }

    public double? ReactivePowerRelativeToMaxReactivePower
    {
        get => ToDouble(ReactivePowerRelativeToMaxReactivePowerI, ReactivePowerSf) / 100;
        set => ReactivePowerRelativeToMaxReactivePowerI = FromDouble<short>(value * 100, ReactivePowerSf);
    }

    public double? ReactivePowerRelativeToAvailableReactivePower
    {
        get => ToDouble(ReactivePowerRelativeToAvailableReactivePowerI, ReactivePowerSf) / 100;
        set => ReactivePowerRelativeToAvailableReactivePowerI = FromDouble<short>(value * 100, ReactivePowerSf);
    }

    [Modbus(16)]
    public ushort ReactivePowerTimeWindow
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(17)]
    public ushort ReactivePowerTimeOut
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(18)]
    public ushort ReactivePowerRampTime
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(19)]
    public SunSpecReactivePowerLimitMode ReactivePowerLimitMode
    {
        get => Get<SunSpecReactivePowerLimitMode>();
        set => Set(value);
    }

    [Modbus(20)]
    public SunSpecOnOff ReactivePowerLimitEnabled
    {
        get => Get<SunSpecOnOff>();
        set => Set(value);
    }
}
