namespace De.Hochstaetter.Fronius.Contracts.Modbus;

public interface ISunSpecInverter
{
    public double? AcCurrentL1 { get; }
    public double? AcCurrentL2 { get; }
    public double? AcCurrentL3 { get; }
    public double? AcCurrentSum => new[] { AcCurrentL1, AcCurrentL2, AcVoltageL3 }.Sum();

    public double? AcVoltageL12 { get; }
    public double? AcVoltageL23 { get; }
    public double? AcVoltageL31 { get; }
    public double? AcVoltageL1 { get; }
    public double? AcVoltageL2 { get; }
    public double? AcVoltageL3 { get; }
    public double? AcPhaseVoltageAverage => new[] { AcVoltageL1, AcVoltageL2, AcVoltageL3 }.Where(v => v.HasValue).Average();
    public double? AcLineVoltageAverage => new[] { AcVoltageL12, AcVoltageL23, AcVoltageL31 }.Where(v => v.HasValue).Average();
    
    public double? PowerActiveSum { get; }
    
    public double? Frequency { get; }
    
    public double? PowerApparentSum { get; }
    
    public double? PowerReactiveSum { get; }
    
    public double? PowerFactorTotal { get; }
    
    public double? EnergyTotal{ get; }
    
    public double? DcCurrent { get; }
    
    public double? DcVoltage { get; }
    
    public double? DcPower { get; }
    
    public double? CabinetTemperature { get; }
    public double? HeatSinkTemperature { get; }
    public double? TransformerTemperature { get; }
    public double? OtherTemperature { get; }
    
    public SunSpecInverterState InverterState { get; }
    public SunSpecInverterState VendorInverterState { get; }
    public SunSpecInverterEvents1 Events1 { get; }
    public SunSpecInverterEvents2 Events2 { get; }
    public SunSpecInverterVendorEvents1 VendorEvents1 { get; }
    public SunSpecInverterVendorEvents2 VendorEvents2 { get; }
    public SunSpecInverterVendorEvents3 VendorEvents3 { get; }
    public SunSpecInverterVendorEvents4 VendorEvents4 { get; }
}