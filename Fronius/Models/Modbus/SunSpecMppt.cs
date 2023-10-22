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

        public override string ToString() => DisplayName;
    }
}
