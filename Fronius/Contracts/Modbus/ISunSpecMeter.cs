namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecMeter
{
    ushort ModelNumber { get; }
    double? TotalCurrent { get; }
    double? CurrentL1 { get; }
    double? CurrentL2 { get; }
    double? CurrentL3 { get; }

    double? PhaseVoltageAverage { get; }
    double? PhaseVoltageL1 { get; }
    double? PhaseVoltageL2 { get; }
    double? PhaseVoltageL3 { get; }
    double? LineVoltageAverage { get; }
    double? LineVoltageL12 { get; }
    double? LineVoltageL23 { get; }
    double? LineVoltageL31 { get; }

    double? Frequency { get; }

    double? ActivePowerSum { get; }
    double? ActivePowerL1 { get; }
    double? ActivePowerL2 { get; }
    double? ActivePowerL3 { get; }

    double? ApparentPowerSum { get; }
    double? ApparentPowerL1 { get; }
    double? ApparentPowerL2 { get; }
    double? ApparentPowerL3 { get; }

    double? ReactivePowerSum { get; }
    double? ReactivePowerL1 { get; }
    double? ReactivePowerL2 { get; }
    double? ReactivePowerL3 { get; }

    double? PowerFactorTotal { get; }
    double? PowerFactorL1 { get; }
    double? PowerFactorL2 { get; }
    double? PowerFactorL3 { get; }

    double? EnergyActiveProduced { get; }
    double? EnergyActiveProducedL1 { get; }
    double? EnergyActiveProducedL2 { get; }
    double? EnergyActiveProducedL3 { get; }
    double? EnergyActiveConsumed { get; }
    double? EnergyActiveConsumedL1 { get; }
    double? EnergyActiveConsumedL2 { get; }
    double? EnergyActiveConsumedL3 { get; }

    double? EnergyApparentProduced { get; }
    double? EnergyApparentProducedL1 { get; }
    double? EnergyApparentProducedL2 { get; }
    double? EnergyApparentProducedL3 { get; }
    double? EnergyApparentConsumed { get; }
    double? EnergyApparentConsumedL1 { get; }
    double? EnergyApparentConsumedL2 { get; }
    double? EnergyApparentConsumedL3 { get; }

    double? EnergyReactiveConsumedQ1 { get; }
    double? EnergyReactiveConsumedQ1L1 { get; }
    double? EnergyReactiveConsumedQ1L2 { get; }
    double? EnergyReactiveConsumedQ1L3 { get; }
    double? EnergyReactiveConsumedQ2 { get; }
    double? EnergyReactiveConsumedQ2L1 { get; }
    double? EnergyReactiveConsumedQ2L2 { get; }
    double? EnergyReactiveConsumedQ2L3 { get; }
    double? EnergyReactiveProducedQ3 { get; }
    double? EnergyReactiveProducedQ3L1 { get; }
    double? EnergyReactiveProducedQ3L2 { get; }
    double? EnergyReactiveProducedQ3L3 { get; }
    double? EnergyReactiveProducedQ4 { get; }
    double? EnergyReactiveProducedQ4L1 { get; }
    double? EnergyReactiveProducedQ4L2 { get; }
    double? EnergyReactiveProducedQ4L3 { get; }

    SunSpecMeterEvents Events { get; }
}
