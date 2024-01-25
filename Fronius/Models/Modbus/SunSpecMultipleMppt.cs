namespace De.Hochstaetter.Fronius.Models.Modbus;

public class SunSpecMultipleMppt : SunSpecModelBase
{
    public SunSpecMultipleMppt(ReadOnlyMemory<byte> data, ushort modelNumber, ushort absoluteRegister) : base(data, modelNumber, absoluteRegister)
    {
        var mppts = new SunSpecMppt[NumberOfTrackers];

        for (var i = 0; i < NumberOfTrackers; i++)
        {
            var registerOffset = 8 + i * 20;

            mppts[i] = new SunSpecMppt
            (
                Data.Slice(registerOffset << 1, 40),
                0xffff,
                (ushort)(absoluteRegister + registerOffset),
                DcVoltageSf,
                DcCurrentSf,
                DcEnergySf,
                DcPowerSf
            );
        }

        Mppts = mppts;
    }

    public override IReadOnlyList<ushort> SupportedModels { get; } = new ushort[] { 160, };
        
    public override ushort MinimumDataLength => throw new NotSupportedException();

    public IReadOnlyList<SunSpecMppt> Mppts { get; }

    [Modbus(0)]
    public short DcCurrentSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(1)]
    public short DcVoltageSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(2)]
    public short DcPowerSf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(3)]
    public short DcEnergySf
    {
        get => Get<short>();
        set => Set(value);
    }

    [Modbus(4)]
    public SunSpecMpptEvent Events
    {
        get => Get<SunSpecMpptEvent>();
        set => Set(value);
    }

    [Modbus(6)]
    public ushort NumberOfTrackers
    {
        get => Get<ushort>();
        set => Set(value);
    }

    [Modbus(7)]
    public ushort TimeStampPeriod
    {
        get => Get<ushort>();
        set => Set(value);
    }
}