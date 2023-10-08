namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecMeter : SunSpecDeviceBase
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
}
