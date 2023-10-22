namespace De.Hochstaetter.Fronius.Models.Modbus;

internal class SunSpecInverterSettings : SunSpecModelBase
{
    public SunSpecInverterSettings(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

    public SunSpecInverterSettings(ushort modelNumber, ushort absoluteRegister, ushort dataLength) : base(modelNumber, absoluteRegister, dataLength) { }

    public override IReadOnlyList<ushort> SupportedModels { get; } = new[] { (ushort)121 };
    
    public override ushort MinimumDataLength => 30;

    [Modbus(0, false)]
    public ushort ActivePowerMaxI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(20)]
    public short ActivePowerMaxSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(1, false)]
    public ushort PccVoltageI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(21)]
    public short PccVoltageSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(2, false)]
    public short VoltageOffsetPccInverterI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(22)]
    public short VoltageOffsetPccInverterSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(3, false)]
    public ushort VoltageMaxI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(4, false)]
    public ushort VoltageMinI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(23)]
    public short VoltageMinMaxSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(5, false)]
    public ushort ApparentPowerMaxI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(24)]
    public short ApparentPowerMaxSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(6, false)]
    public short ReactivePowerMaxQ1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(7, false)]
    public short ReactivePowerMaxQ2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(8, false)]
    public short ReactivePowerMaxQ3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(9, false)]
    public short ReactivePowerMaxQ4I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(25)]
    public short ReactivePowerMaxSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(10, false)]
    public ushort ActivePowerGradientI // in % ActivePowerMax / sec
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(26)]
    public ushort ActivePowerGradientSf
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(11, false)]
    public short PowerFactorMinQ1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(12, false)]
    public short PowerFactorMinQ2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(13, false)]
    public short PowerFactorMinQ3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(14, false)]
    public short PowerFactorMinQ4I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(27)]
    public short PowerFactorMinSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(15, false)]
    public SunSpecReactivePowerOnChargeDischargeChange ReactivePowerChangeMode
    {
        get => Get<SunSpecReactivePowerOnChargeDischargeChange>();
        set => Set(value);
    }

    [Modbus(16, false)]
    public SunSpecTotalApparentPowerCalculationMethod TotalApparentPowerCalculationMode
    {
        get => Get<SunSpecTotalApparentPowerCalculationMethod>();
        set => Set(value);
    }

    [Modbus(17, false)]
    public ushort MaxRampRateI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(28)]
    public short MaxRampRateSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(18, false)]
    public ushort GridFrequencyI
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(29)]
    public short GridFrequencySf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(19, false)]
    public ushort ConnectedPhaseI // Single phase inverters only
    {
        get => Get<ushort>();
        set => Set(value);
    }
}