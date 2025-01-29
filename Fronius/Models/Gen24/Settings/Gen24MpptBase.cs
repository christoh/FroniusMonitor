// ReSharper disable StringLiteralTypo
namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "IdentifierTypo")]
public enum MpptOnOff : byte
{
    [EnumParse(ParseNumeric = true, ParseAs = "off")] Off = 0,
    [EnumParse(ParseNumeric = true, ParseAs = "on", IsDefault = true)] On = 1,
    [EnumParse(ParseNumeric = true, ParseAs = "on_mlsd")] OnMlsd = 2,
}

public enum MpptPowerMode : byte
{
    [EnumParse(ParseNumeric = true, ParseAs = "off", IsDefault = true)] Off = 0,
    [EnumParse(ParseNumeric = true, ParseAs = "auto")] Auto = 1,
    [EnumParse(ParseNumeric = true, ParseAs = "fix")] Fix = 2,
}

public abstract class Gen24MpptBase : BindableBase, ICloneable
{
    public virtual MpptOnOff? DynamicPeakManager
    {
        get;
        set => Set(ref field, value);
    }

    public virtual MpptPowerMode? PowerMode
    {
        get;
        set => Set(ref field, value);
    }

    public virtual uint? WattPeak
    {
        get;
        set => Set(ref field, value);
    }

    public virtual double? DcFixedVoltage
    {
        get;
        set => Set(ref field, value);
    }

    public virtual object Clone() => MemberwiseClone();
}
