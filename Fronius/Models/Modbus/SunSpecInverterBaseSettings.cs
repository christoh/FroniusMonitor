namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecInverterBaseSettings : SunSpecModelBase
{
    public SunSpecInverterBaseSettings(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

    public SunSpecInverterBaseSettings(ushort modelNumber, ushort absoluteRegister, ushort dataLength) : base(modelNumber, absoluteRegister, dataLength) { }

    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 121 };
    
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

    public double? ActivePowerMax
    {
        get => ToDouble(ActivePowerMaxI, ActivePowerMaxSf);
        set => ActivePowerMaxI = FromDouble<ushort>(value, ActivePowerMaxSf);
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

    public double? PccVoltage
    {
        get => ToDouble(PccVoltageI, PccVoltageSf);
        set => PccVoltageI = FromDouble<ushort>(value, PccVoltageSf);
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

    public double? VoltageOffsetPccInverter
    {
        get => ToDouble(VoltageOffsetPccInverterI, VoltageOffsetPccInverterSf);
        set => VoltageOffsetPccInverterI = FromDouble<short>(value, VoltageOffsetPccInverterSf);
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

    public double? VoltageMax
    {
        get => ToDouble(VoltageMaxI, VoltageMinMaxSf);
        set => VoltageMaxI = FromDouble<ushort>(value, VoltageMinMaxSf);
    }

    public double? VoltageMin
    {
        get => ToDouble(VoltageMinI, VoltageMinMaxSf);
        set => VoltageMinI = FromDouble<ushort>(value, VoltageMinMaxSf);
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

    public double? ApparentPowerMax
    {
        get => ToDouble(ApparentPowerMaxI, ApparentPowerMaxSf);
        set => ApparentPowerMaxI = FromDouble<ushort>(value, ApparentPowerMaxSf);
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

    public double? ReactivePowerMaxQ1
    {
        get => ToDouble(ReactivePowerMaxQ1I, ReactivePowerMaxSf);
        set => ReactivePowerMaxQ1I = FromDouble<short>(value, ReactivePowerMaxSf);
    }

    public double? ReactivePowerMaxQ2
    {
        get => ToDouble(ReactivePowerMaxQ2I, ReactivePowerMaxSf);
        set => ReactivePowerMaxQ2I = FromDouble<short>(value, ReactivePowerMaxSf);
    }

    public double? ReactivePowerMaxQ3
    {
        get => ToDouble(ReactivePowerMaxQ3I, ReactivePowerMaxSf);
        set => ReactivePowerMaxQ3I = FromDouble<short>(value, ReactivePowerMaxSf);
    }

    public double? ReactivePowerMaxQ4
    {
        get => ToDouble(ReactivePowerMaxQ4I, ReactivePowerMaxSf);
        set => ReactivePowerMaxQ4I = FromDouble<short>(value, ReactivePowerMaxSf);
    }

    [Modbus(10, false)]
    public ushort ActivePowerGradientI // in % ActivePowerMax / sec
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(26)]
    public short ActivePowerGradientSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? ActivePowerGradient
    {
        get => ToDouble(ActivePowerGradientI, ActivePowerGradientSf);
        set => ActivePowerGradientI = FromDouble<ushort>(value, ActivePowerGradientSf);
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

    public double? PowerFactorMinQ1
    {
        get => ToDouble(PowerFactorMinQ1I, PowerFactorMinSf);
        set => PowerFactorMinQ1I = FromDouble<short>(value, PowerFactorMinSf);
    }

    public double? PowerFactorMinQ2
    {
        get => ToDouble(PowerFactorMinQ2I, PowerFactorMinSf);
        set => PowerFactorMinQ2I = FromDouble<short>(value, PowerFactorMinSf);
    }

    public double? PowerFactorMinQ3
    {
        get => ToDouble(PowerFactorMinQ3I, PowerFactorMinSf);
        set => PowerFactorMinQ3I = FromDouble<short>(value, PowerFactorMinSf);
    }

    public double? PowerFactorMinQ4
    {
        get => ToDouble(PowerFactorMinQ4I, PowerFactorMinSf);
        set => PowerFactorMinQ4I = FromDouble<short>(value, PowerFactorMinSf);
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

    public double? MaxRampRate
    {
        get => ToDouble(MaxRampRateI, MaxRampRateSf);
        set => MaxRampRateI = FromDouble<ushort>(value, MaxRampRateSf);
    }

    [Modbus(18, false)]
    public ushort GridFrequencyNominalI
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

    public double? GridFrequencyNominal
    {
        get => ToDouble(GridFrequencyNominalI, GridFrequencySf);
        set => GridFrequencyNominalI = FromDouble<ushort>(value, GridFrequencySf);
    }

    [Modbus(19, false)]
    public ushort ConnectedPhaseI // Single phase inverters only
    {
        get => Get<ushort>();
        set => Set(value);
    }
}