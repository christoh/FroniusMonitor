namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecInverterFloat : SunSpecModelBase
{
    public SunSpecInverterFloat(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }
    
    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 111, 112, 113 };
    public override ushort MinimumDataLength => 60;

    [Modbus(0)]
    public float AcCurrentSumF
    {
        get => Get<float>();
        set => Set(value);
    }
    
    [Modbus(2)]
    public float AcCurrentL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(4)]
    public float AcCurrentL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(6)]
    public float AcCurrentL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(8)]
    public float AcVoltageL12F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(10)]
    public float AcVoltageL23F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(12)]
    public float AcVoltageL31F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(14)]
    public float AcVoltageL1F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(16)]
    public float AcVoltageL2F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(18)]
    public float AcVoltageL3F
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(20)]
    public float PowerActiveSumF
    {
        get => Get<float>();
        set => Set(value);
    }
    
    [Modbus(22)]
    public float FrequencyF
    {
        get => Get<float>();
        set => Set(value);
    }
    
    [Modbus(24)]
    public float PowerApparentSumF
    {
        get => Get<float>();
        set => Set(value);
    }
    
    [Modbus(26)]
    public float PowerReactiveSumF
    {
        get => Get<float>();
        set => Set(value);
    }
    
    [Modbus(28)]
    public float PowerFactorTotalF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(30)]
    public float EnergyTotalF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(32)]
    public float DcCurrentF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(34)]
    public float DcVoltageF
    {
        get => Get<float>();
        set => Set(value);
    }
    
    [Modbus(36)]
    public float DcPowerF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(38)]
    public float CabinetTemperatureF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(40)]
    public float HeatSinkTemperatureF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(42)]
    public float TransformerTemperatureF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(44)]
    public float OtherTemperatureF
    {
        get => Get<float>();
        set => Set(value);
    }

    [Modbus(46)]
    public SunSpecInverterState InverterState
    {
        get => Get<SunSpecInverterState>();
        set => Set(value);
    }

    [Modbus(47)]
    public SunSpecInverterState VendorInverterState
    {
        get => Get<SunSpecInverterState>();
        set => Set(value);
    }

    [Modbus(48)]
    public SunSpecInverterEvents1 Events1
    {
        get => Get<SunSpecInverterEvents1>();
        set => Set(value);
    }

    [Modbus(50)]
    public SunSpecInverterEvents2 Events2
    {
        get => Get<SunSpecInverterEvents2>();
        set => Set(value);
    }

    [Modbus(52)]
    public SunSpecInverterVendorEvents1 VendorEvents1
    {
        get => Get<SunSpecInverterVendorEvents1>();
        set => Set(value);
    }

    [Modbus(54)]
    public SunSpecInverterVendorEvents2 VendorEvents2
    {
        get => Get<SunSpecInverterVendorEvents2>();
        set => Set(value);
    }

    [Modbus(56)]
    public SunSpecInverterVendorEvents3 VendorEvents3
    {
        get => Get<SunSpecInverterVendorEvents3>();
        set => Set(value);
    }

    [Modbus(58)]
    public SunSpecInverterVendorEvents4 VendorEvents4
    {
        get => Get<SunSpecInverterVendorEvents4>();
        set => Set(value);
    }
}
