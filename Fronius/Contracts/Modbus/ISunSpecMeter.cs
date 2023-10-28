namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecMeter
{
    public ushort ModelNumber { get; }
    public double? TotalCurrent { get; }
    public double? CurrentL1 { get; }
    public double? CurrentL2 { get; }
    public double? CurrentL3 { get; }

    public double? PhaseVoltageAverage { get; }
    public double? PhaseVoltageL1 { get; }
    public double? PhaseVoltageL2 { get; }
    public double? PhaseVoltageL3 { get; }
    public double? LineVoltageAverage { get; }
    public double? LineVoltageL12 { get; }
    public double? LineVoltageL23 { get; }
    public double? LineVoltageL31 { get; }

    public double? Frequency { get; }

    public double? ActivePowerSum { get; }
    public double? ActivePowerL1 { get; }
    public double? ActivePowerL2 { get; }
    public double? ActivePowerL3 { get; }

    public double? ApparentPowerSum { get; }
    public double? ApparentPowerL1 { get; }
    public double? ApparentPowerL2 { get; }
    public double? ApparentPowerL3 { get; }

    public double? ReactivePowerSum { get; }
    public double? ReactivePowerL1 { get; }
    public double? ReactivePowerL2 { get; }
    public double? ReactivePowerL3 { get; }

    public double? PowerFactorTotal { get; }
    public double? PowerFactorL1 { get; }
    public double? PowerFactorL2 { get; }
    public double? PowerFactorL3 { get; }

    public double? EnergyActiveProduced { get; }
    public double? EnergyActiveProducedL1 { get; }
    public double? EnergyActiveProducedL2 { get; }
    public double? EnergyActiveProducedL3 { get; }
    public double? EnergyActiveConsumed { get; }
    public double? EnergyActiveConsumedL1 { get; }
    public double? EnergyActiveConsumedL2 { get; }
    public double? EnergyActiveConsumedL3 { get; }

    public double? EnergyApparentProduced { get; }
    public double? EnergyApparentProducedL1 { get; }
    public double? EnergyApparentProducedL2 { get; }
    public double? EnergyApparentProducedL3 { get; }
    public double? EnergyApparentConsumed { get; }
    public double? EnergyApparentConsumedL1 { get; }
    public double? EnergyApparentConsumedL2 { get; }
    public double? EnergyApparentConsumedL3 { get; }
    
    public double? EnergyReactiveConsumedQ1 { get; }
    public double? EnergyReactiveConsumedQ1L1 { get; }
    public double? EnergyReactiveConsumedQ1L2 { get; }
    public double? EnergyReactiveConsumedQ1L3 { get; }
    public double? EnergyReactiveConsumedQ2 { get; }
    public double? EnergyReactiveConsumedQ2L1 { get; }
    public double? EnergyReactiveConsumedQ2L2 { get; }
    public double? EnergyReactiveConsumedQ2L3 { get; }
    public double? EnergyReactiveProducedQ3 { get; }
    public double? EnergyReactiveProducedQ3L1 { get; }
    public double? EnergyReactiveProducedQ3L2 { get; }
    public double? EnergyReactiveProducedQ3L3 { get; }
    public double? EnergyReactiveProducedQ4 { get; }
    public double? EnergyReactiveProducedQ4L1 { get; }
    public double? EnergyReactiveProducedQ4L2 { get; }
    public double? EnergyReactiveProducedQ4L3 { get; }

    public SunSpecMeterEvents Events { get; }
}
