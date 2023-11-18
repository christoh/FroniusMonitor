namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecMeterInt : SunSpecModelBase, ISunSpecMeter
{
    public SunSpecMeterInt(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

    public SunSpecMeterInt(ushort modelNumber, ushort absoluteRegister, ushort dataLength = 0) : base(modelNumber, absoluteRegister, dataLength)
    {
        VoltageSf = -1;
        ActivePowerSf = ApparentPowerSf = ReactivePowerSf = -1;
        CurrentSf = -3;
        FrequencySf = -2;
        PowerFactorSf = -1;
        EnergyActiveSf = EnergyApparentSf = EnergyReactiveSf = -3;
    }

    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 201, 202, 203, 204 };
    public override ushort MinimumDataLength => 105;

    [Modbus(0)]
    public short TotalCurrentI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(1)]
    public short CurrentL1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(2)]
    public short CurrentL2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(3)]
    public short CurrentL3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(4)]
    public short CurrentSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? TotalCurrent
    {
        get => ToDouble(TotalCurrentI, CurrentSf);
        set => TotalCurrentI = FromDouble<short>(value, CurrentSf);
    }

    public double? CurrentL1
    {
        get => ToDouble(CurrentL1I, CurrentSf);
        set => CurrentL1I = FromDouble<short>(value, CurrentSf);
    }

    public double? CurrentL2
    {
        get => ToDouble(CurrentL2I, CurrentSf);
        set => CurrentL2I = FromDouble<short>(value, CurrentSf);
    }

    public double? CurrentL3
    {
        get => ToDouble(CurrentL3I, CurrentSf);
        set => CurrentL3I = FromDouble<short>(value, CurrentSf);
    }

    [Modbus(5)]
    public short PhaseVoltageAverageI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(6)]
    public short PhaseVoltageL1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(7)]
    public short PhaseVoltageL2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(8)]
    public short PhaseVoltageL3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(9)]
    public short LineVoltageAverageI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(10)]
    public short LineVoltageL12I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(11)]
    public short LineVoltageL23I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(12)]
    public short LineVoltageL31I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(13)]
    public short VoltageSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? PhaseVoltageAverage
    {
        get => ToDouble(PhaseVoltageAverageI, VoltageSf);
        set => PhaseVoltageAverageI = FromDouble<short>(value, VoltageSf);
    }

    public double? PhaseVoltageL1
    {
        get => ToDouble(PhaseVoltageL1I, VoltageSf);
        set => PhaseVoltageL1I = FromDouble<short>(value, VoltageSf);
    }

    public double? PhaseVoltageL2
    {
        get => ToDouble(PhaseVoltageL2I, VoltageSf);
        set => PhaseVoltageL2I = FromDouble<short>(value, VoltageSf);
    }

    public double? PhaseVoltageL3
    {
        get => ToDouble(PhaseVoltageL3I, VoltageSf);
        set => PhaseVoltageL3I = FromDouble<short>(value, VoltageSf);
    }

    public double? LineVoltageAverage
    {
        get => ToDouble(LineVoltageAverageI, VoltageSf);
        set => LineVoltageAverageI = FromDouble<short>(value, VoltageSf);
    }

    public double? LineVoltageL12
    {
        get => ToDouble(LineVoltageL12I, VoltageSf);
        set => LineVoltageL12I = FromDouble<short>(value, VoltageSf);
    }

    public double? LineVoltageL23
    {
        get => ToDouble(LineVoltageL23I, VoltageSf);
        set => LineVoltageL23I = FromDouble<short>(value, VoltageSf);
    }

    public double? LineVoltageL31
    {
        get => ToDouble(LineVoltageL31I, VoltageSf);
        set => LineVoltageL31I = FromDouble<short>(value, VoltageSf);
    }

    [Modbus(14)]
    public short FrequencyI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(15)]
    public short FrequencySf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? Frequency
    {
        get => ToDouble(FrequencyI, FrequencySf);
        set => FrequencyI = FromDouble<short>(value, FrequencySf);
    }

    [Modbus(16)]
    public short ActivePowerSumI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(17)]
    public short ActivePowerL1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(18)]
    public short ActivePowerL2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(19)]
    public short ActivePowerL3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(20)]
    public short ActivePowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? ActivePowerSum
    {
        get => ToDouble(ActivePowerSumI, ActivePowerSf);
        set => ActivePowerSumI = FromDouble<short>(value, ActivePowerSf);
    }

    public double? ActivePowerL1
    {
        get => ToDouble(ActivePowerL1I, ActivePowerSf);
        set => ActivePowerL1I = FromDouble<short>(value, ActivePowerSf);
    }

    public double? ActivePowerL2
    {
        get => ToDouble(ActivePowerL2I, ActivePowerSf);
        set => ActivePowerL2I = FromDouble<short>(value, ActivePowerSf);
    }

    public double? ActivePowerL3
    {
        get => ToDouble(ActivePowerL3I, ActivePowerSf);
        set => ActivePowerL3I = FromDouble<short>(value, ActivePowerSf);
    }

    [Modbus(21)]
    public short ApparentPowerSumI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(22)]
    public short ApparentPowerL1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(23)]
    public short ApparentPowerL2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(24)]
    public short ApparentPowerL3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(25)]
    public short ApparentPowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? ApparentPowerSum
    {
        get => ToDouble(ApparentPowerSumI, ApparentPowerSf);
        set => ApparentPowerSumI = FromDouble<short>(value, ApparentPowerSf);
    }

    public double? ApparentPowerL1
    {
        get => ToDouble(ApparentPowerL1I, ApparentPowerSf);
        set => ApparentPowerL1I = FromDouble<short>(value, ApparentPowerSf);
    }

    public double? ApparentPowerL2
    {
        get => ToDouble(ApparentPowerL2I, ApparentPowerSf);
        set => ApparentPowerL2I = FromDouble<short>(value, ApparentPowerSf);
    }

    public double? ApparentPowerL3
    {
        get => ToDouble(ApparentPowerL3I, ApparentPowerSf);
        set => ApparentPowerL3I = FromDouble<short>(value, ApparentPowerSf);
    }

    [Modbus(26)]
    public short ReactivePowerSumI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(27)]
    public short ReactivePowerL1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(28)]
    public short ReactivePowerL2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(29)]
    public short ReactivePowerL3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(30)]
    public short ReactivePowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? ReactivePowerSum
    {
        get => ToDouble(ReactivePowerSumI, ReactivePowerSf);
        set => ReactivePowerSumI = FromDouble<short>(value, ReactivePowerSf);
    }

    public double? ReactivePowerL1
    {
        get => ToDouble(ReactivePowerL1I, ReactivePowerSf);
        set => ReactivePowerL1I = FromDouble<short>(value, ReactivePowerSf);
    }

    public double? ReactivePowerL2
    {
        get => ToDouble(ReactivePowerL2I, ReactivePowerSf);
        set => ReactivePowerL2I = FromDouble<short>(value, ReactivePowerSf);
    }

    public double? ReactivePowerL3
    {
        get => ToDouble(ReactivePowerL3I, ReactivePowerSf);
        set => ReactivePowerL3I = FromDouble<short>(value, ReactivePowerSf);
    }

    [Modbus(31)]
    public short PowerFactorTotalI
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(32)]
    public short PowerFactorL1I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(33)]
    public short PowerFactorL2I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(34)]
    public short PowerFactorL3I
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(35)]
    public short PowerFactorSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? PowerFactorTotal
    {
        get => ToDouble(PowerFactorTotalI, PowerFactorSf) / 100;
        set => PowerFactorTotalI = FromDouble<short>(value * 100, PowerFactorSf);
    }

    public double? PowerFactorL1
    {
        get => ToDouble(PowerFactorL1I, PowerFactorSf) / 100;
        set => PowerFactorL1I = FromDouble<short>(value * 100, PowerFactorSf);
    }

    public double? PowerFactorL2
    {
        get => ToDouble(PowerFactorL2I, PowerFactorSf) / 100;
        set => PowerFactorL2I = FromDouble<short>(value * 100, PowerFactorSf);
    }

    public double? PowerFactorL3
    {
        get => ToDouble(PowerFactorL3I, PowerFactorSf) / 100;
        set => PowerFactorL3I = FromDouble<short>(value * 100, PowerFactorSf);
    }

    [Modbus(36)]
    public uint EnergyActiveProducedI
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(38)]
    public uint EnergyActiveProducedL1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(40)]
    public uint EnergyActiveProducedL2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(42)]
    public uint EnergyActiveProducedL3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(44)]
    public uint EnergyActiveConsumedI
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(46)]
    public uint EnergyActiveConsumedL1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(48)]
    public uint EnergyActiveConsumedL2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(50)]
    public uint EnergyActiveConsumedL3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(52)]
    public short EnergyActiveSf
    {
        get => Get<short>();
        set => Set(value);
    }

    public double? EnergyActiveProduced
    {
        get => ToDouble(EnergyActiveProducedI, EnergyActiveSf, true);
        set => EnergyActiveProducedI = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    public double? EnergyActiveProducedL1
    {
        get => ToDouble(EnergyActiveProducedL1I, EnergyActiveSf, true);
        set => EnergyActiveProducedL1I = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    public double? EnergyActiveProducedL2
    {
        get => ToDouble(EnergyActiveProducedL2I, EnergyActiveSf, true);
        set => EnergyActiveProducedL2I = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    public double? EnergyActiveProducedL3
    {
        get => ToDouble(EnergyActiveProducedL3I, EnergyActiveSf, true);
        set => EnergyActiveProducedL3I = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    public double? EnergyActiveConsumed
    {
        get => ToDouble(EnergyActiveConsumedI, EnergyActiveSf, true);
        set => EnergyActiveConsumedI = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    public double? EnergyActiveConsumedL1
    {
        get => ToDouble(EnergyActiveConsumedL1I, EnergyActiveSf, true);
        set => EnergyActiveConsumedL1I = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    public double? EnergyActiveConsumedL2
    {
        get => ToDouble(EnergyActiveConsumedL2I, EnergyActiveSf, true);
        set => EnergyActiveConsumedL2I = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    public double? EnergyActiveConsumedL3
    {
        get => ToDouble(EnergyActiveConsumedL3I, EnergyActiveSf, true);
        set => EnergyActiveConsumedL3I = FromDouble<uint>(value, EnergyActiveSf, true);
    }

    [Modbus(53)]
    public uint EnergyApparentProducedI
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(55)]
    public uint EnergyApparentProducedL1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(57)]
    public uint EnergyApparentProducedL2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(59)]
    public uint EnergyApparentProducedL3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(61)]
    public uint EnergyApparentConsumedI
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(63)]
    public uint EnergyApparentConsumedL1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(65)]
    public uint EnergyApparentConsumedL2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(67)]
    public uint EnergyApparentConsumedL3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(69)]
    public short EnergyApparentSf
    {
        get => Get<short>();
        set => Set(value);
    }
    public double? EnergyApparentProduced
    {
        get => ToDouble(EnergyApparentProducedI, EnergyApparentSf, true);
        set => EnergyApparentProducedI = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    public double? EnergyApparentProducedL1
    {
        get => ToDouble(EnergyApparentProducedL1I, EnergyApparentSf, true);
        set => EnergyApparentProducedL1I = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    public double? EnergyApparentProducedL2
    {
        get => ToDouble(EnergyApparentProducedL2I, EnergyApparentSf, true);
        set => EnergyApparentProducedL2I = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    public double? EnergyApparentProducedL3
    {
        get => ToDouble(EnergyApparentProducedL3I, EnergyApparentSf, true);
        set => EnergyApparentProducedL3I = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    public double? EnergyApparentConsumed
    {
        get => ToDouble(EnergyApparentConsumedI, EnergyApparentSf, true);
        set => EnergyApparentConsumedI = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    public double? EnergyApparentConsumedL1
    {
        get => ToDouble(EnergyApparentConsumedL1I, EnergyApparentSf, true);
        set => EnergyApparentConsumedL1I = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    public double? EnergyApparentConsumedL2
    {
        get => ToDouble(EnergyApparentConsumedL2I, EnergyApparentSf, true);
        set => EnergyApparentConsumedL2I = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    public double? EnergyApparentConsumedL3
    {
        get => ToDouble(EnergyApparentConsumedL3I, EnergyApparentSf, true);
        set => EnergyApparentConsumedL3I = FromDouble<uint>(value, EnergyApparentSf, true);
    }

    [Modbus(70)]
    public uint EnergyReactiveConsumedQ1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(72)]
    public uint EnergyReactiveConsumedQ1L1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(74)]
    public uint EnergyReactiveConsumedQ1L2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(76)]
    public uint EnergyReactiveConsumedQ1L3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(78)]
    public uint EnergyReactiveConsumedQ2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(80)]
    public uint EnergyReactiveConsumedQ2L1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(82)]
    public uint EnergyReactiveConsumedQ2L2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(84)]
    public uint EnergyReactiveConsumedQ2L3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(86)]
    public uint EnergyReactiveProducedQ3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(88)]
    public uint EnergyReactiveProducedQ3L1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(90)]
    public uint EnergyReactiveProducedQ3L2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(92)]
    public uint EnergyReactiveProducedQ3L3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(94)]
    public uint EnergyReactiveProducedQ4I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(96)]
    public uint EnergyReactiveProducedQ4L1I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(98)]
    public uint EnergyReactiveProducedQ4L2I
    {
        get => Get<uint>();
        set => Set(value);
    }

    [Modbus(100)]
    public uint EnergyReactiveProducedQ4L3I
    {
        get => Get<uint>();
        set => Set(value);
    }

    public double? EnergyReactiveConsumedQ1
    {
        get => ToDouble(EnergyReactiveConsumedQ1I, EnergyReactiveSf, true);
        set => EnergyReactiveConsumedQ1I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveConsumedQ1L1
    {
        get => ToDouble(EnergyReactiveConsumedQ1L1I, EnergyReactiveSf, true);
        set => EnergyReactiveConsumedQ1L1I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveConsumedQ1L2
    {
        get => ToDouble(EnergyReactiveConsumedQ1L2I, EnergyReactiveSf, true);
        set => EnergyReactiveConsumedQ1L2I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveConsumedQ1L3
    {
        get => ToDouble(EnergyReactiveConsumedQ1L3I, EnergyReactiveSf, true);
        set => EnergyReactiveConsumedQ1L3I = FromDouble<uint>(value, EnergyReactiveSf);
    }


    public double? EnergyReactiveConsumedQ2
    {
        get => ToDouble(EnergyReactiveConsumedQ2I, EnergyReactiveSf, true);
        set => EnergyReactiveConsumedQ2I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveConsumedQ2L1
    {
        get => ToDouble(EnergyReactiveConsumedQ2L1I, EnergyReactiveSf, true);
        set => EnergyReactiveConsumedQ2L1I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveConsumedQ2L2
    {
        get => ToDouble(EnergyReactiveConsumedQ2L2I, EnergyReactiveSf, true);
        set => EnergyReactiveConsumedQ2L2I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveConsumedQ2L3
    {
        get => ToDouble(EnergyReactiveConsumedQ2L3I, EnergyReactiveSf);
        set => EnergyReactiveConsumedQ2L3I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveProducedQ3
    {
        get => ToDouble(EnergyReactiveProducedQ3I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ3I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveProducedQ3L1
    {
        get => ToDouble(EnergyReactiveProducedQ3L1I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ3L1I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveProducedQ3L2
    {
        get => ToDouble(EnergyReactiveProducedQ3L2I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ3L2I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveProducedQ3L3
    {
        get => ToDouble(EnergyReactiveProducedQ3L3I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ3L3I = FromDouble<uint>(value, EnergyReactiveSf);
    }


    public double? EnergyReactiveProducedQ4
    {
        get => ToDouble(EnergyReactiveProducedQ4I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ4I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveProducedQ4L1
    {
        get => ToDouble(EnergyReactiveProducedQ4L1I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ4L1I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveProducedQ4L2
    {
        get => ToDouble(EnergyReactiveProducedQ4L2I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ4L2I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    public double? EnergyReactiveProducedQ4L3
    {
        get => ToDouble(EnergyReactiveProducedQ4L3I, EnergyReactiveSf, true);
        set => EnergyReactiveProducedQ4L3I = FromDouble<uint>(value, EnergyReactiveSf);
    }

    [Modbus(102)]
    public short EnergyReactiveSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(103)]
    public SunSpecMeterEvents Events
    {
        get => Get<SunSpecMeterEvents>();
        set => Set(value);
    }
}