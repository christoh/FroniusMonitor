namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecMeter : SunSpecDeviceBase, IPowerMeter3P
{
    private SunSpecMeterType? meterType;

    public SunSpecMeterType? MeterType
    {
        get => meterType;
        set => Set(ref meterType, value);
    }

    private SunSpecProtocol? protocol;

    public SunSpecProtocol? Protocol
    {
        get => protocol;
        set => Set(ref protocol, value);
    }

    private double? totalCurrent;

    public double? TotalCurrent
    {
        get => totalCurrent;
        set => Set(ref totalCurrent, value);
    }

    private double? currentL1;

    public double? CurrentL1
    {
        get => currentL1;
        set => Set(ref currentL1, value);
    }

    private double? currentL2;

    public double? CurrentL2
    {
        get => currentL2;
        set => Set(ref currentL2, value);
    }

    private double? currentL3;

    public double? CurrentL3
    {
        get => currentL3;
        set => Set(ref currentL3, value);
    }

    private double? phaseVoltageL1;

    public double? PhaseVoltageL1
    {
        get => phaseVoltageL1;
        set => Set(ref phaseVoltageL1, value);
    }

    private double? phaseVoltageL2;

    public double? PhaseVoltageL2
    {
        get => phaseVoltageL2;
        set => Set(ref phaseVoltageL2, value);
    }

    private double? phaseVoltageL3;

    public double? PhaseVoltageL3
    {
        get => phaseVoltageL3;
        set => Set(ref phaseVoltageL3, value);
    }

    private double? phaseVoltageAverage;

    public double? PhaseVoltageAverage
    {
        get => phaseVoltageAverage;
        set => Set(ref phaseVoltageAverage, value);
    }

    private double? lineVoltageL12;

    public double? LineVoltageL12
    {
        get => lineVoltageL12;
        set => Set(ref lineVoltageL12, value);
    }

    private double? lineVoltageL23;

    public double? LineVoltageL23
    {
        get => lineVoltageL23;
        set => Set(ref lineVoltageL23, value);
    }

    private double? lineVoltageL31;

    public double? LineVoltageL31
    {
        get => lineVoltageL31;
        set => Set(ref lineVoltageL31, value);
    }

    private double? lineVoltageAverage;

    public double? LineVoltageAverage
    {
        get => lineVoltageAverage;
        set => Set(ref lineVoltageAverage, value);
    }

    private double? frequency;

    public double? Frequency
    {
        get => frequency;
        set => Set(ref frequency, value);
    }

    private double? activePowerSum;

    public double? ActivePowerSum
    {
        get => activePowerSum;
        set => Set(ref activePowerSum, value);
    }

    private double? activePowerL1;

    public double? ActivePowerL1
    {
        get => activePowerL1;
        set => Set(ref activePowerL1, value);
    }

    private double? activePowerL2;

    public double? ActivePowerL2
    {
        get => activePowerL2;
        set => Set(ref activePowerL2, value);
    }

    private double? activePowerL3;

    public double? ActivePowerL3
    {
        get => activePowerL3;
        set => Set(ref activePowerL3, value);
    }

    private double? apparentPowerSum;

    public double? ApparentPowerSum
    {
        get => apparentPowerSum;
        set => Set(ref apparentPowerSum, value);
    }

    private double? apparentPowerL1;

    public double? ApparentPowerL1
    {
        get => apparentPowerL1;
        set => Set(ref apparentPowerL1, value);
    }

    private double? apparentPowerL2;

    public double? ApparentPowerL2
    {
        get => apparentPowerL2;
        set => Set(ref apparentPowerL2, value);
    }

    private double? apparentPowerL3;

    public double? ApparentPowerL3
    {
        get => apparentPowerL3;
        set => Set(ref apparentPowerL3, value);
    }

    private double? reactivePowerSum;

    public double? ReactivePowerSum
    {
        get => reactivePowerSum;
        set => Set(ref reactivePowerSum, value);
    }

    private double? reactivePowerL1;

    public double? ReactivePowerL1
    {
        get => reactivePowerL1;
        set => Set(ref reactivePowerL1, value);
    }

    private double? reactivePowerL2;

    public double? ReactivePowerL2
    {
        get => reactivePowerL2;
        set => Set(ref reactivePowerL2, value);
    }

    private double? reactivePowerL3;

    public double? ReactivePowerL3
    {
        get => reactivePowerL3;
        set => Set(ref reactivePowerL3, value);
    }

    private double? powerFactorTotal;

    public double? PowerFactorTotal
    {
        get => powerFactorTotal;
        set => Set(ref powerFactorTotal, value);
    }

    private double? powerFactorL1;

    public double? PowerFactorL1
    {
        get => powerFactorL1;
        set => Set(ref powerFactorL1, value);
    }

    private double? powerFactorL2;

    public double? PowerFactorL2
    {
        get => powerFactorL2;
        set => Set(ref powerFactorL2, value);
    }

    private double? powerFactorL3;

    public double? PowerFactorL3
    {
        get => powerFactorL3;
        set => Set(ref powerFactorL3, value);
    }

    private double? energyActiveProduced;

    public double? EnergyActiveProduced
    {
        get => energyActiveProduced;
        set => Set(ref energyActiveProduced, value);
    }

    private double? energyActiveProducedL1;

    public double? EnergyActiveProducedL1
    {
        get => energyActiveProducedL1;
        set => Set(ref energyActiveProducedL1, value);
    }

    private double? energyActiveProducedL2;

    public double? EnergyActiveProducedL2
    {
        get => energyActiveProducedL2;
        set => Set(ref energyActiveProducedL2, value);
    }

    private double? energyActiveProducedL3;

    public double? EnergyActiveProducedL3
    {
        get => energyActiveProducedL3;
        set => Set(ref energyActiveProducedL3, value);
    }

    private double? energyActiveConsumed;

    public double? EnergyActiveConsumed
    {
        get => energyActiveConsumed;
        set => Set(ref energyActiveConsumed, value);
    }

    private double? energyActiveConsumedL1;

    public double? EnergyActiveConsumedL1
    {
        get => energyActiveConsumedL1;
        set => Set(ref energyActiveConsumedL1, value);
    }

    private double? energyActiveConsumedL2;

    public double? EnergyActiveConsumedL2
    {
        get => energyActiveConsumedL2;
        set => Set(ref energyActiveConsumedL2, value);
    }

    private double? energyActiveConsumedL3;

    public double? EnergyActiveConsumedL3
    {
        get => energyActiveConsumedL3;
        set => Set(ref energyActiveConsumedL3, value);
    }

    private double? energyApparentProduced;

    public double? EnergyApparentProduced
    {
        get => energyApparentProduced;
        set => Set(ref energyApparentProduced, value);
    }

    private double? energyApparentProducedL1;

    public double? EnergyApparentProducedL1
    {
        get => energyApparentProducedL1;
        set => Set(ref energyApparentProducedL1, value);
    }

    private double? energyApparentProducedL2;

    public double? EnergyApparentProducedL2
    {
        get => energyApparentProducedL2;
        set => Set(ref energyApparentProducedL2, value);
    }

    private double? energyApparentProducedL3;

    public double? EnergyApparentProducedL3
    {
        get => energyApparentProducedL3;
        set => Set(ref energyApparentProducedL3, value);
    }

    private double? energyApparentConsumed;

    public double? EnergyApparentConsumed
    {
        get => energyApparentConsumed;
        set => Set(ref energyApparentConsumed, value);
    }

    private double? energyApparentConsumedL1;

    public double? EnergyApparentConsumedL1
    {
        get => energyApparentConsumedL1;
        set => Set(ref energyApparentConsumedL1, value);
    }

    private double? energyApparentConsumedL2;

    public double? EnergyApparentConsumedL2
    {
        get => energyApparentConsumedL2;
        set => Set(ref energyApparentConsumedL2, value);
    }

    private double? energyApparentConsumedL3;

    public double? EnergyApparentConsumedL3
    {
        get => energyApparentConsumedL3;
        set => Set(ref energyApparentConsumedL3, value);
    }

    private double? energyReactiveConsumedQ1;

    public double? EnergyReactiveConsumedQ1
    {
        get => energyReactiveConsumedQ1;
        set => Set(ref energyReactiveConsumedQ1, value, () => NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed)));
    }

    private double? energyReactiveConsumedQ1L1;

    public double? EnergyReactiveConsumedQ1L1
    {
        get => energyReactiveConsumedQ1L1;
        set => Set(ref energyReactiveConsumedQ1L1, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveConsumedL1));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed));
        });
    }

    private double? energyReactiveConsumedQ1L2;

    public double? EnergyReactiveConsumedQ1L2
    {
        get => energyReactiveConsumedQ1L2;
        set => Set(ref energyReactiveConsumedQ1L2, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveConsumedL2));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed));
        });
    }

    private double? energyReactiveConsumedQ1L3;

    public double? EnergyReactiveConsumedQ1L3
    {
        get => energyReactiveConsumedQ1L3;
        set => Set(ref energyReactiveConsumedQ1L3, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveConsumedL3));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed));
        });
    }

    private double? energyReactiveConsumedQ2;

    public double? EnergyReactiveConsumedQ2
    {
        get => energyReactiveConsumedQ2;
        set => Set(ref energyReactiveConsumedQ2, value, () => NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed)));
    }

    private double? energyReactiveConsumedQ2L1;

    public double? EnergyReactiveConsumedQ2L1
    {
        get => energyReactiveConsumedQ2L1;
        set => Set(ref energyReactiveConsumedQ2L1, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveConsumedL1));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed));
        });
    }

    private double? energyReactiveConsumedQ2L2;

    public double? EnergyReactiveConsumedQ2L2
    {
        get => energyReactiveConsumedQ2L2;
        set => Set(ref energyReactiveConsumedQ2L2, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveConsumedL2));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed));
        });
    }

    private double? energyReactiveConsumedQ2L3;

    public double? EnergyReactiveConsumedQ2L3
    {
        get => energyReactiveConsumedQ2L3;
        set => Set(ref energyReactiveConsumedQ2L3, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveConsumedL3));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveConsumed));
        });
    }

    private double? energyReactiveProducedQ3;

    public double? EnergyReactiveProducedQ3
    {
        get => energyReactiveProducedQ3;
        set => Set(ref energyReactiveProducedQ3, value, () => NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced)));
    }

    private double? energyReactiveProducedQ3L1;

    public double? EnergyReactiveProducedQ3L1
    {
        get => energyReactiveProducedQ3L1;
        set => Set(ref energyReactiveProducedQ3L1, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveProducedL1));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced));
        });
    }

    private double? energyReactiveProducedQ3L2;

    public double? EnergyReactiveProducedQ3L2
    {
        get => energyReactiveProducedQ3L2;
        set => Set(ref energyReactiveProducedQ3L2, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveProducedL2));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced));
        });
    }

    private double? energyReactiveProducedQ3L3;

    public double? EnergyReactiveProducedQ3L3
    {
        get => energyReactiveProducedQ3L3;
        set => Set(ref energyReactiveProducedQ3L3, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveProducedL3));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced));
        });
    }

    private double? energyReactiveProducedQ4;

    public double? EnergyReactiveProducedQ4
    {
        get => energyReactiveProducedQ4;
        set => Set(ref energyReactiveProducedQ4, value, () => NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced)));
    }

    private double? energyReactiveProducedQ4L1;

    public double? EnergyReactiveProducedQ4L1
    {
        get => energyReactiveProducedQ4L1;
        set => Set(ref energyReactiveProducedQ4L1, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveProducedL1));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced));
        });
    }

    private double? energyReactiveProducedQ4L2;

    public double? EnergyReactiveProducedQ4L2
    {
        get => energyReactiveProducedQ4L2;
        set => Set(ref energyReactiveProducedQ4L2, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveProducedL2));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced));
        });
    }

    private double? energyReactiveProducedQ4L3;

    public double? EnergyReactiveProducedQ4L3
    {
        get => energyReactiveProducedQ4L3;
        set => Set(ref energyReactiveProducedQ4L3, value, () =>
        {
            NotifyOfPropertyChange(nameof(EnergyReactiveProducedL3));
            NotifyOfPropertyChange(nameof(IPowerMeter3P.EnergyReactiveProduced));
        });
    }

    private uint eventBitField;

    public uint EventBitField
    {
        get => eventBitField;
        set => Set(ref eventBitField, value);
    }

    public double? EnergyReactiveConsumedL1 => EnergyReactiveConsumedQ1L1 + EnergyReactiveConsumedQ2L1;
    public double? EnergyReactiveConsumedL2 => EnergyReactiveConsumedQ1L2 + EnergyReactiveConsumedQ2L2;
    public double? EnergyReactiveConsumedL3 => EnergyReactiveConsumedQ1L3 + EnergyReactiveConsumedQ2L3;
    public double? EnergyReactiveProducedL1 => EnergyReactiveProducedQ3L1 + EnergyReactiveProducedQ4L1;
    public double? EnergyReactiveProducedL2 => EnergyReactiveProducedQ3L2 + EnergyReactiveProducedQ4L2;
    public double? EnergyReactiveProducedL3 => EnergyReactiveProducedQ3L3 + EnergyReactiveProducedQ4L3;
}
