namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecMeter : SunSpecGroupBase, IPowerMeter3P, ISunSpecMeter
{
    private readonly ISunSpecMeter meter;

    public SunSpecMeter(IEnumerable<SunSpecModelBase> models) : base(models)
    {
        meter = Models.OfType<ISunSpecMeter>().Single();
    }

    public ushort ModelNumber => meter.ModelNumber;
    public double? TotalCurrent => meter.TotalCurrent;
    public double? CurrentL1 => meter.CurrentL1;
    public double? CurrentL2 => meter.CurrentL2;
    public double? CurrentL3 => meter.CurrentL3;

    public double? PhaseVoltageAverage => meter.PhaseVoltageAverage;
    public double? PhaseVoltageL1 => meter.PhaseVoltageL1;
    public double? PhaseVoltageL2 => meter.PhaseVoltageL2;
    public double? PhaseVoltageL3 => meter.PhaseVoltageL3;
    public double? LineVoltageAverage => meter.LineVoltageAverage;
    public double? LineVoltageL12 => meter.LineVoltageL12;
    public double? LineVoltageL23 => meter.LineVoltageL23;
    public double? LineVoltageL31 => meter.LineVoltageL31;

    public double? Frequency => meter.Frequency;

    public double? ActivePowerSum => meter.ActivePowerSum;
    public double? ActivePowerL1 => meter.ActivePowerL1;
    public double? ActivePowerL2 => meter.ActivePowerL2;
    public double? ActivePowerL3 => meter.ActivePowerL3;

    public double? ApparentPowerSum => meter.ApparentPowerSum;
    public double? ApparentPowerL1 => meter.ApparentPowerL1;
    public double? ApparentPowerL2 => meter.ApparentPowerL2;
    public double? ApparentPowerL3 => meter.ApparentPowerL3;

    public double? ReactivePowerSum => meter.ReactivePowerSum;
    public double? ReactivePowerL1 => meter.ReactivePowerL1;
    public double? ReactivePowerL2 => meter.ReactivePowerL2;
    public double? ReactivePowerL3 => meter.ReactivePowerL3;

    public double? PowerFactorTotal => meter.PowerFactorTotal;
    public double? PowerFactorL1 => meter.PowerFactorL1;
    public double? PowerFactorL2 => meter.PowerFactorL2;
    public double? PowerFactorL3 => meter.PowerFactorL3;

    public double? EnergyActiveProduced => meter.EnergyActiveProduced;
    public double? EnergyActiveProducedL1 => meter.EnergyActiveProducedL1;
    public double? EnergyActiveProducedL2 => meter.EnergyActiveProducedL2;
    public double? EnergyActiveProducedL3 => meter.EnergyActiveProducedL3;
    public double? EnergyActiveConsumed => meter.EnergyActiveConsumed;
    public double? EnergyActiveConsumedL1 => meter.EnergyActiveConsumedL1;
    public double? EnergyActiveConsumedL2 => meter.EnergyActiveConsumedL2;
    public double? EnergyActiveConsumedL3 => meter.EnergyActiveConsumedL3;

    public double? EnergyApparentProduced => meter.EnergyApparentProduced;
    public double? EnergyApparentProducedL1 => meter.EnergyApparentProducedL1;
    public double? EnergyApparentProducedL2 => meter.EnergyApparentProducedL2;
    public double? EnergyApparentProducedL3 => meter.EnergyApparentProducedL3;
    public double? EnergyApparentConsumed => meter.EnergyApparentConsumed;
    public double? EnergyApparentConsumedL1 => meter.EnergyApparentConsumedL1;
    public double? EnergyApparentConsumedL2 => meter.EnergyApparentConsumedL2;
    public double? EnergyApparentConsumedL3 => meter.EnergyApparentConsumedL3;

    public double? EnergyReactiveConsumedL1 => EnergyReactiveConsumedQ1L1 + EnergyReactiveConsumedQ2L1;
    public double? EnergyReactiveConsumedL2 => EnergyReactiveConsumedQ1L2 + EnergyReactiveConsumedQ2L2;
    public double? EnergyReactiveConsumedL3 => EnergyReactiveConsumedQ1L3 + EnergyReactiveConsumedQ2L3;
    public double? EnergyReactiveProducedL1 => EnergyReactiveProducedQ3L1 + EnergyReactiveProducedQ4L1;
    public double? EnergyReactiveProducedL2 => EnergyReactiveProducedQ3L2 + EnergyReactiveProducedQ4L2;
    public double? EnergyReactiveProducedL3 => EnergyReactiveProducedQ3L3 + EnergyReactiveProducedQ4L3;

    public double? EnergyReactiveConsumedQ1 => meter.EnergyReactiveConsumedQ1;
    public double? EnergyReactiveConsumedQ1L1 => meter.EnergyReactiveConsumedQ1L1;
    public double? EnergyReactiveConsumedQ1L2 => meter.EnergyReactiveConsumedQ1L2;
    public double? EnergyReactiveConsumedQ1L3 => meter.EnergyReactiveConsumedQ1L3;
    public double? EnergyReactiveConsumedQ2 => meter.EnergyReactiveConsumedQ2;
    public double? EnergyReactiveConsumedQ2L1 => meter.EnergyReactiveConsumedQ2L1;
    public double? EnergyReactiveConsumedQ2L2 => meter.EnergyReactiveConsumedQ2L2;
    public double? EnergyReactiveConsumedQ2L3 => meter.EnergyReactiveConsumedQ2L3;
    public double? EnergyReactiveProducedQ3 => meter.EnergyReactiveProducedQ3;
    public double? EnergyReactiveProducedQ3L1 => meter.EnergyReactiveProducedQ3L1;
    public double? EnergyReactiveProducedQ3L2 => meter.EnergyReactiveProducedQ3L2;
    public double? EnergyReactiveProducedQ3L3 => meter.EnergyReactiveProducedQ3L3;
    public double? EnergyReactiveProducedQ4 => meter.EnergyReactiveProducedQ4;
    public double? EnergyReactiveProducedQ4L1 => meter.EnergyReactiveProducedQ4L1;
    public double? EnergyReactiveProducedQ4L2 => meter.EnergyReactiveProducedQ4L2;
    public double? EnergyReactiveProducedQ4L3 => meter.EnergyReactiveProducedQ4L3;

    public SunSpecMeterEvents Events => meter.Events;
}