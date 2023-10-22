namespace De.Hochstaetter.Fronius.Models.Modbus
{
    public class SunSpecInverterInt : SunSpecModelBase
    {
        public SunSpecInverterInt(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister) { }

        public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 101, 102, 103 };

        [Modbus(0)]
        public ushort AcCurrentSumI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(1)]
        public ushort AcCurrentL1I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(2)]
        public ushort AcCurrentL2I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(3)]
        public ushort AcCurrentL3I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(4)]
        public short CurrentSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(5)]
        public ushort AcVoltageL12I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(6)]
        public ushort AcVoltageL23I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(7)]
        public ushort AcVoltageL31I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(8)]
        public ushort AcVoltageL1I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(9)]
        public ushort AcVoltageL2I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(10)]
        public ushort AcVoltageL3I
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(11)]
        public short AcVoltageSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(12)]
        public short PowerActiveSumI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(13)]
        public short PowerActiveSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(14)]
        public ushort FrequencyI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(15)]
        public short FrequencySf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(16)]
        public short PowerApparentSumI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(17)]
        public short PowerApparentSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(18)]
        public short PowerReactiveSumI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(19)]
        public short PowerReactiveSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(20)]
        public short PowerFactorTotalI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(21)]
        public short PowerFactorSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(22)]
        public uint EnergyTotalI
        {
            get => Get<uint>();
            set => Set(value);
        }

        [Modbus(24)]
        public short EnergySf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(25)]
        public ushort DcCurrentI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(26)]
        public short DcCurrentSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(27)]
        public ushort DcVoltageI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(28)]
        public short DcVoltageSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(29)]
        public short DcPowerI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(30)]
        public short DcPowerSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(31)]
        public short CabinetTemperatureI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(32)]
        public short HeatSinkTemperatureI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(33)]
        public short TransformerTemperatureI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(34)]
        public short OtherTemperatureI
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(35)]
        public short TemperatureSf
        {
            get => Get<short>();
            set => Set(value);
        }

        [Modbus(36)]
        public SunSpecInverterState InverterState
        {
            get => Get<SunSpecInverterState>();
            set => Set(value);
        }

        [Modbus(37)]
        public SunSpecInverterState VendorInverterState
        {
            get => Get<SunSpecInverterState>();
            set => Set(value);
        }

        [Modbus(38)]
        public SunSpecInverterEvents1 Events1
        {
            get => Get<SunSpecInverterEvents1>();
            set => Set(value);
        }

        [Modbus(40)]
        public SunSpecInverterEvents2 Events2
        {
            get => Get<SunSpecInverterEvents2>();
            set => Set(value);
        }

        [Modbus(42)]
        public SunSpecInverterVendorEvents1 VendorEvents1
        {
            get => Get<SunSpecInverterVendorEvents1>();
            set => Set(value);
        }

        [Modbus(44)]
        public SunSpecInverterVendorEvents2 VendorEvents2
        {
            get => Get<SunSpecInverterVendorEvents2>();
            set => Set(value);
        }

        [Modbus(46)]
        public SunSpecInverterVendorEvents3 VendorEvents3
        {
            get => Get<SunSpecInverterVendorEvents3>();
            set => Set(value);
        }

        [Modbus(48)]
        public SunSpecInverterVendorEvents4 VendorEvents4
        {
            get => Get<SunSpecInverterVendorEvents4>();
            set => Set(value);
        }

        public double? AcCurrentSum
        {
            get => ToDouble(AcCurrentSumI, CurrentSf);
            set => AcCurrentSumI = FromDouble<ushort>(value, CurrentSf);
        }

        public double? AcCurrentL1
        {
            get => ToDouble(AcCurrentL1I, CurrentSf);
            set => AcCurrentL1I = FromDouble<ushort>(value, CurrentSf);
        }

        public double? AcCurrentL2
        {
            get => ToDouble(AcCurrentL2I, CurrentSf);
            set => AcCurrentL2I = FromDouble<ushort>(value, CurrentSf);
        }

        public double? AcCurrentL3
        {
            get => ToDouble(AcCurrentL3I, CurrentSf);
            set => AcCurrentL3I = FromDouble<ushort>(value, CurrentSf);
        }
    }
}
