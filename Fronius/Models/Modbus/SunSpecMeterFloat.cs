namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecMeterFloat : SunSpecModelBase, ISunSpecMeter
{
    public SunSpecMeterFloat(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

    public SunSpecMeterFloat(ushort modelNumber, ushort absoluteRegister, ushort dataLength) : base(modelNumber, absoluteRegister, dataLength)
    {
        GetType().GetProperties().Where(p => p.PropertyType == typeof(float)).Apply(p => p.SetValue(this, float.NaN));
    }

    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 211, 212, 213, 214 };

    public override ushort MinimumDataLength => 124;

    [Modbus(0)]
    public float TotalCurrentF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(2)]
    public float CurrentL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(4)]
    public float CurrentL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(6)]
    public float CurrentL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? TotalCurrent
    {
        get => ToDouble(TotalCurrentF);
        set => TotalCurrentF = FromDouble<float>(value);
    }

    public double? CurrentL1
    {
        get => ToDouble(CurrentL1F);
        set => CurrentL1F = FromDouble<float>(value);
    }

    public double? CurrentL2
    {
        get => ToDouble(CurrentL2F);
        set => CurrentL2F = FromDouble<float>(value);
    }

    public double? CurrentL3
    {
        get => ToDouble(CurrentL3F);
        set => CurrentL1F = FromDouble<float>(value);
    }
    
    [Modbus(8)]
    public float PhaseVoltageAverageF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(10)]
    public float PhaseVoltageL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(12)]
    public float PhaseVoltageL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(14)]
    public float PhaseVoltageL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(16)]
    public float LineVoltageAverageF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(18)]
    public float LineVoltageL12F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(20)]
    public float LineVoltageL23F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(22)]
    public float LineVoltageL31F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? PhaseVoltageAverage
    {
        get => ToDouble(PhaseVoltageAverageF);
        set => PhaseVoltageAverageF = FromDouble<float>(value);
    }

    public double? PhaseVoltageL1
    {
        get => ToDouble(PhaseVoltageL1F);
        set => PhaseVoltageL1F = FromDouble<float>(value);
    }

    public double? PhaseVoltageL2
    {
        get => ToDouble(PhaseVoltageL2F);
        set => PhaseVoltageL2F = FromDouble<float>(value);
    }

    public double? PhaseVoltageL3
    {
        get => ToDouble(PhaseVoltageL3F);
        set => PhaseVoltageL3F = FromDouble<float>(value);
    }

    public double? LineVoltageAverage
    {
        get => ToDouble(LineVoltageAverageF);
        set => LineVoltageAverageF = FromDouble<float>(value);
    }

    public double? LineVoltageL12
    {
        get => ToDouble(LineVoltageL12F);
        set => LineVoltageL12F = FromDouble<float>(value);
    }

    public double? LineVoltageL23
    {
        get => ToDouble(LineVoltageL23F);
        set => LineVoltageL23F = FromDouble<float>(value);
    }

    public double? LineVoltageL31
    {
        get => ToDouble(LineVoltageL31F);
        set => LineVoltageL31F = FromDouble<float>(value);
    }

    [Modbus(24)]
    public float FrequencyF
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? Frequency
    {
        get => ToDouble(FrequencyF);
        set => FrequencyF = FromDouble<float>(value);
    }
    
    [Modbus(26)]
    public float ActivePowerSumF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(28)]
    public float ActivePowerL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(30)]
    public float ActivePowerL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(32)]
    public float ActivePowerL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? ActivePowerSum
    {
        get => ToDouble(ActivePowerSumF);
        set => ActivePowerSumF = FromDouble<float>(value);
    }

    public double? ActivePowerL1
    {
        get => ToDouble(ActivePowerL1F);
        set => ActivePowerL1F = FromDouble<float>(value);
    }

    public double? ActivePowerL2
    {
        get => ToDouble(ActivePowerL2F);
        set => ActivePowerL2F = FromDouble<float>(value);
    }

    public double? ActivePowerL3
    {
        get => ToDouble(ActivePowerL3F);
        set => ActivePowerL3F = FromDouble<float>(value);
    }

    [Modbus(34)]
    public float ApparentPowerSumF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(36)]
    public float ApparentPowerL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(38)]
    public float ApparentPowerL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(40)]
    public float ApparentPowerL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? ApparentPowerSum
    {
        get => ToDouble(ApparentPowerSumF);
        set => ApparentPowerSumF = FromDouble<float>(value);
    }

    public double? ApparentPowerL1
    {
        get => ToDouble(ApparentPowerL1F);
        set => ApparentPowerL1F = FromDouble<float>(value);
    }

    public double? ApparentPowerL2
    {
        get => ToDouble(ApparentPowerL2F);
        set => ApparentPowerL2F = FromDouble<float>(value);
    }

    public double? ApparentPowerL3
    {
        get => ToDouble(ApparentPowerL3F);
        set => ApparentPowerL3F = FromDouble<float>(value);
    }

    [Modbus(42)]
    public float ReactivePowerSumF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(44)]
    public float ReactivePowerL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(46)]
    public float ReactivePowerL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(48)]
    public float ReactivePowerL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? ReactivePowerSum
    {
        get => ToDouble(ReactivePowerSumF);
        set => ReactivePowerSumF = FromDouble<float>(value);
    }

    public double? ReactivePowerL1
    {
        get => ToDouble(ReactivePowerL1F);
        set => ReactivePowerL1F = FromDouble<float>(value);
    }

    public double? ReactivePowerL2
    {
        get => ToDouble(ReactivePowerL2F);
        set => ReactivePowerL2F = FromDouble<float>(value);
    }

    public double? ReactivePowerL3
    {
        get => ToDouble(ReactivePowerL3F);
        set => ReactivePowerL3F = FromDouble<float>(value);
    }

    [Modbus(50)]
    public float PowerFactorTotalF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(52)]
    public float PowerFactorL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(54)]
    public float PowerFactorL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(56)]
    public float PowerFactorL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? PowerFactorTotal
    {
        get => ToDouble(PowerFactorTotalF);
        set => PowerFactorTotalF = FromDouble<float>(value);
    }

    public double? PowerFactorL1
    {
        get => ToDouble(PowerFactorL1F);
        set => PowerFactorL1F = FromDouble<float>(value);
    }

    public double? PowerFactorL2
    {
        get => ToDouble(PowerFactorL2F);
        set => PowerFactorL2F = FromDouble<float>(value);
    }

    public double? PowerFactorL3
    {
        get => ToDouble(PowerFactorL3F);
        set => PowerFactorL3F = FromDouble<float>(value);
    }
    
    [Modbus(58)]
    public float EnergyActiveProducedF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(60)]
    public float EnergyActiveProducedL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(62)]
    public float EnergyActiveProducedL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(64)]
    public float EnergyActiveProducedL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(66)]
    public float EnergyActiveConsumedF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(68)]
    public float EnergyActiveConsumedL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(70)]
    public float EnergyActiveConsumedL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(72)]
    public float EnergyActiveConsumedL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? EnergyActiveProduced
    {
        get => ToDouble(EnergyActiveProducedF);
        set => EnergyActiveProducedF = FromDouble<float>(value);
    }

    public double? EnergyActiveProducedL1
    {
        get => ToDouble(EnergyActiveProducedL1F);
        set => EnergyActiveProducedL1F = FromDouble<float>(value);
    }

    public double? EnergyActiveProducedL2
    {
        get => ToDouble(EnergyActiveProducedL2F);
        set => EnergyActiveProducedL2F = FromDouble<float>(value);
    }

    public double? EnergyActiveProducedL3
    {
        get => ToDouble(EnergyActiveProducedL3F);
        set => EnergyActiveProducedL3F = FromDouble<float>(value);
    }

    public double? EnergyActiveConsumed
    {
        get => ToDouble(EnergyActiveConsumedF);
        set => EnergyActiveConsumedF = FromDouble<float>(value);
    }

    public double? EnergyActiveConsumedL1
    {
        get => ToDouble(EnergyActiveConsumedL1F);
        set => EnergyActiveConsumedL1F = FromDouble<float>(value);
    }

    public double? EnergyActiveConsumedL2
    {
        get => ToDouble(EnergyActiveConsumedL2F);
        set => EnergyActiveConsumedL2F = FromDouble<float>(value);
    }

    public double? EnergyActiveConsumedL3
    {
        get => ToDouble(EnergyActiveConsumedL3F);
        set => EnergyActiveConsumedL3F = FromDouble<float>(value);
    }
    
    [Modbus(74)]
    public float EnergyApparentProducedF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(76)]
    public float EnergyApparentProducedL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(78)]
    public float EnergyApparentProducedL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(80)]
    public float EnergyApparentProducedL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(82)]
    public float EnergyApparentConsumedF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(84)]
    public float EnergyApparentConsumedL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(86)]
    public float EnergyApparentConsumedL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(88)]
    public float EnergyApparentConsumedL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? EnergyApparentProduced
    {
        get => ToDouble(EnergyApparentProducedF);
        set => EnergyApparentProducedF = FromDouble<float>(value);
    }

    public double? EnergyApparentProducedL1
    {
        get => ToDouble(EnergyApparentProducedL1F);
        set => EnergyApparentProducedL1F = FromDouble<float>(value);
    }

    public double? EnergyApparentProducedL2
    {
        get => ToDouble(EnergyApparentProducedL2F);
        set => EnergyApparentProducedL2F = FromDouble<float>(value);
    }

    public double? EnergyApparentProducedL3
    {
        get => ToDouble(EnergyApparentProducedL3F);
        set => EnergyApparentProducedL3F = FromDouble<float>(value);
    }

    public double? EnergyApparentConsumed
    {
        get => ToDouble(EnergyApparentConsumedF);
        set => EnergyApparentConsumedF = FromDouble<float>(value);
    }

    public double? EnergyApparentConsumedL1
    {
        get => ToDouble(EnergyApparentConsumedL1F);
        set => EnergyApparentConsumedL1F = FromDouble<float>(value);
    }

    public double? EnergyApparentConsumedL2
    {
        get => ToDouble(EnergyApparentConsumedL2F);
        set => EnergyApparentConsumedL2F = FromDouble<float>(value);
    }

    public double? EnergyApparentConsumedL3
    {
        get => ToDouble(EnergyApparentConsumedL3F);
        set => EnergyApparentConsumedL3F = FromDouble<float>(value);
    }
    
    [Modbus(90)]
    public float EnergyReactiveConsumedQ1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(92)]
    public float EnergyReactiveConsumedQ1L1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(94)]
    public float EnergyReactiveConsumedQ1L2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(96)]
    public float EnergyReactiveConsumedQ1L3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(98)]
    public float EnergyReactiveConsumedQ2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(100)]
    public float EnergyReactiveConsumedQ2L1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(102)]
    public float EnergyReactiveConsumedQ2L2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(104)]
    public float EnergyReactiveConsumedQ2L3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(106)]
    public float EnergyReactiveProducedQ3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(108)]
    public float EnergyReactiveProducedQ3L1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(110)]
    public float EnergyReactiveProducedQ3L2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(112)]
    public float EnergyReactiveProducedQ3L3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(114)]
    public float EnergyReactiveProducedQ4F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(116)]
    public float EnergyReactiveProducedQ4L1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(118)]
    public float EnergyReactiveProducedQ4L2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(120)]
    public float EnergyReactiveProducedQ4L3F
    {
        get => Get<float>();
        set => Set(value);
    }

    public double? EnergyReactiveConsumedQ1
    {
        get => ToDouble(EnergyReactiveConsumedQ1F);
        set => EnergyReactiveConsumedQ1F = FromDouble<float>(value);
    }

    public double? EnergyReactiveConsumedQ1L1
    {
        get => ToDouble(EnergyReactiveConsumedQ1L1F);
        set => EnergyReactiveConsumedQ1L1F = FromDouble<float>(value);
    }

    public double? EnergyReactiveConsumedQ1L2
    {
        get => ToDouble(EnergyReactiveConsumedQ1L2F);
        set => EnergyReactiveConsumedQ1L2F = FromDouble<float>(value);
    }

    public double? EnergyReactiveConsumedQ1L3
    {
        get => ToDouble(EnergyReactiveConsumedQ1L3F);
        set => EnergyReactiveConsumedQ1L3F = FromDouble<float>(value);
    }


    public double? EnergyReactiveConsumedQ2
    {
        get => ToDouble(EnergyReactiveConsumedQ2F);
        set => EnergyReactiveConsumedQ2F = FromDouble<float>(value);
    }

    public double? EnergyReactiveConsumedQ2L1
    {
        get => ToDouble(EnergyReactiveConsumedQ2L1F);
        set => EnergyReactiveConsumedQ2L1F = FromDouble<float>(value);
    }

    public double? EnergyReactiveConsumedQ2L2
    {
        get => ToDouble(EnergyReactiveConsumedQ2L2F);
        set => EnergyReactiveConsumedQ2L2F = FromDouble<float>(value);
    }

    public double? EnergyReactiveConsumedQ2L3
    {
        get => ToDouble(EnergyReactiveConsumedQ2L3F);
        set => EnergyReactiveConsumedQ2L3F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ3
    {
        get => ToDouble(EnergyReactiveProducedQ3F);
        set => EnergyReactiveProducedQ3F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ3L1
    {
        get => ToDouble(EnergyReactiveProducedQ3L1F);
        set => EnergyReactiveProducedQ3L1F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ3L2
    {
        get => ToDouble(EnergyReactiveProducedQ3L2F);
        set => EnergyReactiveProducedQ3L2F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ3L3
    {
        get => ToDouble(EnergyReactiveProducedQ3L3F);
        set => EnergyReactiveProducedQ3L3F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ4
    {
        get => ToDouble(EnergyReactiveProducedQ4F);
        set => EnergyReactiveProducedQ4F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ4L1
    {
        get => ToDouble(EnergyReactiveProducedQ4L1F);
        set => EnergyReactiveProducedQ4L1F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ4L2
    {
        get => ToDouble(EnergyReactiveProducedQ4L2F);
        set => EnergyReactiveProducedQ4L2F = FromDouble<float>(value);
    }

    public double? EnergyReactiveProducedQ4L3
    {
        get => ToDouble(EnergyReactiveProducedQ4L3F);
        set => EnergyReactiveProducedQ4L3F = FromDouble<float>(value);
    }

    [Modbus(122)]
    public SunSpecMeterEvents Events
    {
        get => Get<SunSpecMeterEvents>();
        set => Set(value);
    }
}