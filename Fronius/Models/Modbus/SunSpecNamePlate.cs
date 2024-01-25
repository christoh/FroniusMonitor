namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecNamePlate(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : SunSpecModelBase(data, modelNumber, absoluteRegister)
{
    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 120, };
    
    public override ushort MinimumDataLength => 25;

    [Modbus(0)]
    public SunSpecDerType DeviceType
    {
        get => Get<SunSpecDerType>();
        set => Set(value);
    }

    [Modbus(1)]
    public ushort MaximumContinuousActivePowerI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(2)]
    public short MaximumContinuousActivePowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumContinuousActivePower
    {
        get => ToDouble(MaximumContinuousActivePowerI, MaximumContinuousActivePowerSf);
        set => MaximumContinuousActivePowerI = FromDouble<ushort>(value, MaximumContinuousActivePowerSf);
    }
    
    [Modbus(3)]
    public ushort MaximumContinuousApparentPowerI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(4)]
    public short MaximumContinuousApparentPowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumContinuousApparentPower
    {
        get => ToDouble(MaximumContinuousApparentPowerI, MaximumContinuousApparentPowerSf);
        set => MaximumContinuousApparentPowerI = FromDouble<ushort>(value, MaximumContinuousApparentPowerSf);
    }

    [Modbus(5)]
    public short MaximumContinuousReactivePowerQ1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(6)]
    public short MaximumContinuousReactivePowerQ2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(7)]
    public short MaximumContinuousReactivePowerQ3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(8)]
    public short MaximumContinuousReactivePowerQ4I
    {
        get => Get<short>();
        set => Set(value);
    }
    
    [Modbus(9)]
    public short MaximumContinuousReactivePowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumContinuousReactivePowerQ1
    {
        get => ToDouble(MaximumContinuousReactivePowerQ1I, MaximumContinuousReactivePowerSf);
        set => MaximumContinuousReactivePowerQ1I = FromDouble<short>(value, MaximumContinuousReactivePowerSf);
    }

    public double? MaximumContinuousReactivePowerQ2
    {
        get => ToDouble(MaximumContinuousReactivePowerQ2I, MaximumContinuousReactivePowerSf);
        set => MaximumContinuousReactivePowerQ2I = FromDouble<short>(value, MaximumContinuousReactivePowerSf);
    }

    public double? MaximumContinuousReactivePowerQ3
    {
        get => ToDouble(MaximumContinuousReactivePowerQ3I, MaximumContinuousReactivePowerSf);
        set => MaximumContinuousReactivePowerQ3I = FromDouble<short>(value, MaximumContinuousReactivePowerSf);
    }

    public double? MaximumContinuousReactivePowerQ4
    {
        get => ToDouble(MaximumContinuousReactivePowerQ4I, MaximumContinuousReactivePowerSf);
        set => MaximumContinuousReactivePowerQ4I = FromDouble<short>(value, MaximumContinuousReactivePowerSf);
    }

    [Modbus(10)]
    public ushort MaximumRmsCurrentI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(11)]
    public short MaximumRmsCurrentSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumRmsCurrent
    {
        get => ToDouble(MaximumRmsCurrentI, MaximumRmsCurrentSf);
        set => MaximumRmsCurrentI = FromDouble<ushort>(value, MaximumRmsCurrentSf);
    }

    [Modbus(12)]
    public short MaximumPowerFactorQ1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(13)]
    public short MaximumPowerFactorQ2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(14)]
    public short MaximumPowerFactorQ3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(15)]
    public short MaximumPowerFactorQ4I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(16)]
    public short MaximumPowerFactorSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumPowerFactorQ1
    {
        get => ToDouble(MaximumPowerFactorQ1I, MaximumPowerFactorSf);
        set => MaximumPowerFactorQ1I = FromDouble<short>(value, MaximumPowerFactorSf);
    }

    public double? MaximumPowerFactorQ2
    {
        get => ToDouble(MaximumPowerFactorQ2I, MaximumPowerFactorSf);
        set => MaximumPowerFactorQ2I = FromDouble<short>(value, MaximumPowerFactorSf);
    }

    public double? MaximumPowerFactorQ3
    {
        get => ToDouble(MaximumPowerFactorQ3I, MaximumPowerFactorSf);
        set => MaximumPowerFactorQ3I = FromDouble<short>(value, MaximumPowerFactorSf);
    }

    public double? MaximumPowerFactorQ4
    {
        get => ToDouble(MaximumPowerFactorQ4I, MaximumPowerFactorSf);
        set => MaximumPowerFactorQ4I = FromDouble<short>(value, MaximumPowerFactorSf);
    }

    [Modbus(17)]
    public ushort GrossEnergyCapacityI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(18)]
    public short EnergyCapacitySf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? GrossEnergyCapacity
    {
        get => ToDouble(GrossEnergyCapacityI, EnergyCapacitySf);
        set => GrossEnergyCapacityI = FromDouble<ushort>(value, EnergyCapacitySf);
    }

    [Modbus(19)]
    public ushort AmpereHoursCapacityI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(20)]
    public short AmpereHoursCapacitySf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? AmpereHoursCapacity
    {
        get => ToDouble(AmpereHoursCapacityI, AmpereHoursCapacitySf);
        set => AmpereHoursCapacityI = FromDouble<ushort>(value, AmpereHoursCapacitySf);
    }

    [Modbus(21)]
    public ushort MaximumChargeRateI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(22)]
    public short MaximumChargeRateSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumChargeRate
    {
        get => ToDouble(MaximumChargeRateI, MaximumChargeRateSf);
        set => MaximumChargeRateI = FromDouble<ushort>(value, MaximumChargeRateSf);
    }

    [Modbus(23)]
    public ushort MaximumDischargeRateI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(24)]
    public short MaximumDischargeRateSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? MaximumDischargeRate
    {
        get => ToDouble(MaximumDischargeRateI, MaximumDischargeRateSf);
        set => MaximumDischargeRateI = FromDouble<ushort>(value, MaximumDischargeRateSf);
    }
}
