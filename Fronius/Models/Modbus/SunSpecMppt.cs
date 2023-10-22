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

        [Modbus(10)]
        public ushort DcVoltageI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(11)]
        public ushort DcEnergyI
        {
            get => Get<ushort>();
            set => Set(value);
        }

        [Modbus(12)]
        public uint DcLifeTimeEnergyI
        {
            get => Get<uint>();
            set => Set(value);
        }

        [Modbus(14)]
        public uint TimeStamp
        {
            get => Get<uint>();
            set => Set(value);
        }

        [Modbus(16)]
        public short TemperatureI
        {
            get => Get<short>();
            set => Set(value);
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
