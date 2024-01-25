namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecStorageBaseSettings(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : SunSpecModelBase(data, modelNumber, absoluteRegister)
{
    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 124, };

    public override ushort MinimumDataLength => 24;

    [Modbus(0, false)]
    public ushort MaximumChargingActivePowerI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(16)]
    public short MaximumChargingActivePowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumChargingActivePower
    {
        get => ToDouble(MaximumChargingActivePowerI, MaximumChargingActivePowerSf);
        set => MaximumChargingActivePowerI = FromDouble<ushort>(value, MaximumChargingActivePowerSf);
    }

    [Modbus(1, false)]
    public ushort ActivePowerChargingGradientI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(17)]
    public short ActivePowerChargingDischargingGradientSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? ActivePowerChargingGradient
    {
        get => ToDouble(ActivePowerChargingGradientI, ActivePowerChargingDischargingGradientSf) / 100;
        set => ActivePowerChargingGradientI = FromDouble<ushort>(value * 100, ActivePowerChargingDischargingGradientSf);
    }

    [Modbus(2, false)]
    public ushort ActivePowerDischargingGradientI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    public double? ActivePowerDischargingGradient
    {
        get => ToDouble(ActivePowerDischargingGradientI, ActivePowerChargingDischargingGradientSf) / 100;
        set => ActivePowerDischargingGradientI = FromDouble<ushort>(value * 100, ActivePowerChargingDischargingGradientSf);
    }

    [Modbus(3, false)]
    public SunSpecChargingLimits ChargingLimits
    {
        get => Get<SunSpecChargingLimits>();
        set => Set(value);
    }

    [Modbus(4, false)]
    public ushort MaximumChargingApparentPowerI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(18)]
    public short MaximumChargingApparentPowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumChargingApparentPower
    {
        get => ToDouble(MaximumChargingApparentPowerI, MaximumChargingApparentPowerSf);
        set => MaximumChargingApparentPowerI = FromDouble<ushort>(value, MaximumChargingApparentPowerSf);
    }

    [Modbus(5, false)]
    public ushort ReservedCapacityRelativeI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(19)]
    public short ReservedCapacityRelativeSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? ReservedCapacityRelative
    {
        get => ToDouble(ReservedCapacityRelativeI, ReservedCapacityRelativeSf) / 100;
        set => ReservedCapacityRelativeI = FromDouble<ushort>(value * 100, ReservedCapacityRelativeSf);
    }

    [Modbus(6)]
    public ushort RelativeCapacityAvailableI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(20)]
    public short RelativeCapacitySf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? RelativeCapacityAvailable
    {
        get => ToDouble(RelativeCapacityAvailableI, RelativeCapacitySf);
        set => RelativeCapacityAvailableI = FromDouble<ushort>(value, RelativeCapacitySf);
    }

    [Modbus(7)]
    public ushort CapacityAvailableI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(21)]
    public short CapacityAvailableSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? CapacityAvailable
    {
        get => ToDouble(CapacityAvailableI, CapacityAvailableSf);
        set => CapacityAvailableI = FromDouble<ushort>(value, CapacityAvailableSf);
    }

    [Modbus(8)]
    public ushort InternalBatteryVoltageI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(22)]
    public short InternalBatteryVoltageSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? InternalBatteryVoltage
    {
        get => ToDouble(InternalBatteryVoltageI, InternalBatteryVoltageSf);
        set => InternalBatteryVoltageI = FromDouble<ushort>(value, InternalBatteryVoltageSf);
    }

    [Modbus(9)]
    public SunSpecStorageState State
    {
        get => Get<SunSpecStorageState>();
        set => Set(value);
    }

    [Modbus(10, false)]
    public short RelativeOutgoingActivePowerMaxI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(11, false)]
    public short RelativeIncomingActivePowerMaxI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(23, false)]
    public short RelativeInOutActivePowerMaxSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? RelativeOutgoingActivePowerMax
    {
        get => ToDouble(RelativeOutgoingActivePowerMaxI, RelativeInOutActivePowerMaxSf) / 100;
        set => RelativeOutgoingActivePowerMaxI = FromDouble<short>(value * 100, RelativeInOutActivePowerMaxSf);
    }

    public double? RelativeIncomingActivePowerMax
    {
        get => ToDouble(RelativeIncomingActivePowerMaxI, RelativeInOutActivePowerMaxSf) / 100;
        set => RelativeIncomingActivePowerMaxI = FromDouble<short>(value * 100, RelativeInOutActivePowerMaxSf);
    }

    [Modbus(12, false)]
    public ushort ActivePowerMaxLimitTimeWindowI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    public double? ActivePowerMaxLimitTimeWindow
    {
        get => ToDouble(ActivePowerMaxLimitTimeWindowI);
        set => ActivePowerMaxLimitTimeWindowI = FromDouble<ushort>(value);
    }

    [Modbus(13, false)]
    public ushort ActivePowerMaxLimitTimeOutI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    public double? ActivePowerMaxLimitTimeOut
    {
        get => ToDouble(ActivePowerMaxLimitTimeOutI);
        set => ActivePowerMaxLimitTimeOutI = FromDouble<ushort>(value);
    }

    [Modbus(14, false)]
    public ushort ActivePowerMaxLimitRampTimeI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    public double? ActivePowerMaxLimitRampTime
    {
        get => ToDouble(ActivePowerMaxLimitRampTimeI);
        set => ActivePowerMaxLimitRampTimeI = FromDouble<ushort>(value);
    }

    [Modbus(15)]
    public SunSpecChargingSource ChargingSource
    {
        get => Get<SunSpecChargingSource>();
        set => Set(value);
    }
}