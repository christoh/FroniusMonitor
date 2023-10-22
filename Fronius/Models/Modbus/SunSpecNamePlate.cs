namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecNamePlate : SunSpecModelBase
{
    public SunSpecNamePlate(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 120, };

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
}
