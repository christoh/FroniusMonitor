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

    [FroniusProprietaryImport("ACBRIDGE_MODE_POWER_OUTPUT_U16", FroniusDataType.Root)]
    public AcBridgePowerOutputMode? PowerOutputMode
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DEVICE_POWERACTIVE_NOMINAL_F32", FroniusDataType.Root)]
    public double? PowerActiveNominal
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DEVICE_POWERAPPARENT_NOMINAL_F32", FroniusDataType.Root)]
    public double? PowerApparentNominal
    {
        get;
        set => Set(ref field, value);
    }

    public static Gen24AcSystemSettings Parse(JToken? token) => Gen24JsonService.ReadFroniusData<Gen24AcSystemSettings>(token);
}