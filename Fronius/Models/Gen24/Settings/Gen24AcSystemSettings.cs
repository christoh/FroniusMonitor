namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum AcBridgePowerOutputMode : ushort
{
    [EnumParse(ParseNumeric = true)] Symmetric = 0,
    [EnumParse(ParseNumeric = true)] Asymmetric = 1,
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class Gen24AcSystemSettings : Gen24ParsingBase
{
    public override object Clone() => MemberwiseClone();

    [ObservableProperty]
    [FroniusProprietaryImport("ACBRIDGE_MODE_POWER_OUTPUT_U16", FroniusDataType.Root)]
    public partial AcBridgePowerOutputMode? PowerOutputMode { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DEVICE_POWERACTIVE_NOMINAL_F32", FroniusDataType.Root)]
    public partial double? PowerActiveNominal { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DEVICE_POWERAPPARENT_NOMINAL_F32", FroniusDataType.Root)]
    public partial double? PowerApparentNominal { get; set; }

    public static Gen24AcSystemSettings Parse(JToken? token) => Gen24JsonService.ReadFroniusData<Gen24AcSystemSettings>(token);
}