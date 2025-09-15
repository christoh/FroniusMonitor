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

public abstract partial class Gen24MpptBase : BindableBase, ICloneable
{
    [ObservableProperty]
    public virtual partial MpptOnOff? DynamicPeakManager { get; set; }

    [ObservableProperty]
    public virtual partial MpptPowerMode? PowerMode { get; set; }

    [ObservableProperty]
    public virtual partial uint? WattPeak { get; set; }

    [ObservableProperty]
    public virtual partial double? DcFixedVoltage { get; set; }

    public virtual object Clone() => MemberwiseClone();
}
