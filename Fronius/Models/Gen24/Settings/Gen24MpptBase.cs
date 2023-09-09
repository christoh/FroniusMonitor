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
    private MpptOnOff? dynamicPeakManager;

    public virtual MpptOnOff? DynamicPeakManager
    {
        get => dynamicPeakManager;
        set => Set(ref dynamicPeakManager, value);
    }

    private MpptPowerMode? powerMode;

    public virtual MpptPowerMode? PowerMode
    {
        get => powerMode;
        set => Set(ref powerMode, value);
    }

    private uint? wattPeak;

    public virtual uint? WattPeak
    {
        get => wattPeak;
        set => Set(ref wattPeak, value);
    }

    private double? dcFixedVoltage;

    public virtual double? DcFixedVoltage
    {
        get => dcFixedVoltage;
        set => Set(ref dcFixedVoltage, value);
    }

    public virtual object Clone() => MemberwiseClone();
}
