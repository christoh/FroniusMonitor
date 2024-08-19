namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum AcBridgePowerOutputMode : ushort
{
    [EnumParse(ParseNumeric = true)] Symmetric = 0,
    [EnumParse(ParseNumeric = true)] Asymmetric = 1,
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24AcSystemSettings : Gen24ParsingBase
{
    public override object Clone() => MemberwiseClone();

    private AcBridgePowerOutputMode? powerOutputMode;
    [FroniusProprietaryImport("ACBRIDGE_MODE_POWER_OUTPUT_U16", FroniusDataType.Root)]
    public AcBridgePowerOutputMode? PowerOutputMode
    {
        get => powerOutputMode;
        set => Set(ref powerOutputMode, value);
    }

    private double? powerActiveNominal;
    [FroniusProprietaryImport("DEVICE_POWERACTIVE_NOMINAL_F32", FroniusDataType.Root)]
    public double? PowerActiveNominal
    {
        get => powerActiveNominal;
        set => Set(ref powerActiveNominal, value);
    }

    private double? powerApparentNominal;
    [FroniusProprietaryImport("DEVICE_POWERAPPARENT_NOMINAL_F32", FroniusDataType.Root)]
    public double? PowerApparentNominal
    {
        get => powerApparentNominal;
        set => Set(ref powerApparentNominal, value);
    }

    public static Gen24AcSystemSettings Parse(JToken? token) => Gen24JsonService.ReadFroniusData<Gen24AcSystemSettings>(token);
}