namespace De.Hochstaetter.Fronius.Models;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public enum DeviceType
{
    [EnumParse(ParseAs = "inverter")] Inverter,
    [EnumParse(ParseAs = "powermeter")] PowerMeter,
    [EnumParse(IsDefault = true)] Unknown,
}