namespace De.Hochstaetter.Fronius.Contracts;

public interface IPowerMeter3P
{
    public double? TotalCurrent => CurrentL1 + CurrentL2 + CurrentL3;
    public double? CurrentL1 { get; }
    public double? CurrentL2 { get; }
    public double? CurrentL3 { get; }
    public double? PhaseVoltageL1 { get; }
    public double? PhaseVoltageL2 { get; }
    public double? PhaseVoltageL3 { get; }
    public double? PhaseVoltageAverage => (PhaseVoltageL1 + PhaseVoltageL2 + PhaseVoltageL3) / 3;
    public double? LineVoltageL12 { get; }
    public double? LineVoltageL23 { get; }
    public double? LineVoltageL31 { get; }
    public double? LineVoltageAverage => (LineVoltageL12 + LineVoltageL23 + LineVoltageL31) / 3;
    public double? Frequency { get; }
    public double? ActivePowerSum => ActivePowerL1 + ActivePowerL2 + ActivePowerL3;
    public double? ActivePowerL1 { get; }
    public double? ActivePowerL2 { get; }
    public double? ActivePowerL3 { get; }
    public double? ApparentPowerSum => ApparentPowerL1 + ApparentPowerL2 + ApparentPowerL3;
    public double? ApparentPowerL1 { get; }
    public double? ApparentPowerL2 { get; }
    public double? ApparentPowerL3 { get; }
    public double? ReactivePowerSum => ReactivePowerL1 + ReactivePowerL2 + ReactivePowerL3;
    public double? ReactivePowerL1 { get; }
    public double? ReactivePowerL2 { get; }
    public double? ReactivePowerL3 { get; }
    public double? PowerFactorTotal { get; }
    public double? PowerFactorL1 { get; }
    public double? PowerFactorL2 { get; }
    public double? PowerFactorL3 { get; }
    public double? EnergyActiveProduced => EnergyActiveProducedL1 + EnergyActiveProducedL2 + EnergyActiveProducedL3;
    public double? EnergyActiveProducedL1 { get; }
    public double? EnergyActiveProducedL2 { get; }
    public double? EnergyActiveProducedL3 { get; }
    public double? EnergyActiveConsumed => EnergyActiveConsumedL1 + EnergyActiveConsumedL2 + EnergyActiveConsumedL3;
    public double? EnergyActiveConsumedL1 { get; }
    public double? EnergyActiveConsumedL2 { get; }
    public double? EnergyActiveConsumedL3 { get; }
    public double? EnergyApparentProduced => EnergyApparentProducedL1 + EnergyApparentProducedL2 + EnergyApparentProducedL3;
    public double? EnergyApparentProducedL1 { get; }
    public double? EnergyApparentProducedL2 { get; }
    public double? EnergyApparentProducedL3 { get; }
    public double? EnergyApparentConsumed => EnergyApparentConsumedL1 + EnergyApparentConsumedL2 + EnergyApparentConsumedL3;
    public double? EnergyApparentConsumedL1 { get; }
    public double? EnergyApparentConsumedL2 { get; }
    public double? EnergyApparentConsumedL3 { get; }
    public double? EnergyReactiveConsumed => EnergyReactiveConsumedL1 + EnergyReactiveConsumedL2 + EnergyReactiveConsumedL3;
    public double? EnergyReactiveConsumedL1 { get; }
    public double? EnergyReactiveConsumedL2 { get; }
    public double? EnergyReactiveConsumedL3 { get; }
    public double? EnergyReactiveProduced => EnergyReactiveProducedL1 + EnergyReactiveProducedL2 + EnergyReactiveProducedL3;
    public double? EnergyReactiveProducedL1 { get; }
    public double? EnergyReactiveProducedL2 { get; }
    public double? EnergyReactiveProducedL3 { get; }
}