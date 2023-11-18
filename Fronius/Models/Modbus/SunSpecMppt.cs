namespace De.Hochstaetter.Fronius.Models.Modbus
{
    public class SunSpecMppt : SunSpecModelBase, IHaveDisplayName
    {
        public SunSpecMppt
        (
            ReadOnlyMemory<byte> data,
            ushort modelNumber,
            ushort absoluteRegister,
            short dcVoltageSf,
            short dcCurrentSf,
            short dcEnergySf,
            short dcPowerSf
        ) : base(data, modelNumber, absoluteRegister)
        {
            DcCurrentSf = dcCurrentSf;
            DcPowerSf = dcPowerSf;
            DcVoltageSf = dcVoltageSf;
            DcEnergySf = dcEnergySf;
        }

        public short DcVoltageSf { get; }
        public short DcPowerSf { get; }
        public short DcEnergySf { get; }
        public short DcCurrentSf { get; }

        public override IReadOnlyList<ushort> SupportedModels { get; } = new[] { (ushort)0xffff };
        
        public override ushort MinimumDataLength => 20;

        [Modbus(0)]
        public ushort Id
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(1, 8)]
        public string DisplayName
        {
            get => GetString() ?? string.Empty;
            set => SetString(value);
        }

        [Modbus(9)]
        public ushort DcCurrentI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        public double? DcCurrent
        {
            get => ToDouble(DcCurrentI, DcCurrentSf);
            set => DcCurrentI = FromDouble<ushort>(value, DcCurrentSf);
        }

        [Modbus(10)]
        public ushort DcVoltageI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        public double? DcVoltage
        {
            get => ToDouble(DcVoltageI, DcVoltageSf);
            set => DcVoltageI = FromDouble<ushort>(value, DcVoltageSf);
        }

        [Modbus(11)]
        public ushort DcPowerI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        public double? DcPower
        {
            get => ToDouble(DcPowerI, DcPowerSf);
            set => DcPowerI = FromDouble<ushort>(value, DcPowerSf);
        }

        [Modbus(12)]
        public uint DcLifeTimeEnergyI
        {
            get => Get<uint>();
            set => Set(value);
        }

        public double? DcLifeTimeEnergy
        {
            get => ToDouble(DcLifeTimeEnergyI, DcEnergySf, true);
            set => DcLifeTimeEnergyI = FromDouble<uint>(value, DcEnergySf);
        }

        [Modbus(14)]
        public uint TimeStampI
        {
            get => Get<uint>();
            set => Set(value);
        }

        public DateTime? TimeStamp
        {
            get => ToDateTime(TimeStampI);
            set => TimeStampI = ToSunSpecTime(value);
        }

        [Modbus(16)]
        public short TemperatureI
        {
            get => Get<short>();
            set => Set(value);
        }

        public double? Temperature
        {
            get => ToDouble(TemperatureI);
            set => TemperatureI = FromDouble<short>(value);
        }

        [Modbus(17)]
        public SunSpecMpptState State
        {
            get => Get<SunSpecMpptState>();
            set => Set(value);
        }

        [Modbus(18)]
        public SunSpecMpptEvent Events
        {
            get => Get<SunSpecMpptEvent>();
            set => Set(value);
        }

        public override string ToString() => DisplayName;
    }
}
