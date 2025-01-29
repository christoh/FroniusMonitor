namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum StringCombination : byte
{
    [EnumParse(ParseNumeric = true)] CombineStrings_12 = 1,
    [EnumParse(ParseNumeric = true)] CombineStrings_123 = 4,
    [EnumParse(ParseNumeric = true)] CombineStrings_1234 = 10,
    [EnumParse(ParseNumeric = true)] CombineStrings_12and34 = 11,
    [EnumParse(ParseNumeric = true)] CombineStrings_13 = 2,
    [EnumParse(ParseNumeric = true)] CombineStrings_134 = 8,
    [EnumParse(ParseNumeric = true)] CombineStrings_13and24 = 12,
    [EnumParse(ParseNumeric = true)] CombineStrings_14 = 5,
    [EnumParse(ParseNumeric = true)] CombineStrings_14and23 = 13,
    [EnumParse(ParseNumeric = true)] CombineStrings_23 = 3,
    [EnumParse(ParseNumeric = true)] CombineStrings_234 = 9,
    [EnumParse(ParseNumeric = true)] CombineStrings_24 = 6,
    [EnumParse(ParseNumeric = true)] CombineStrings_34 = 7,
    [EnumParse(ParseNumeric = true)] NotCombined = 0,
}

public class Gen24Mppt : BindableBase, ICloneable
{
    private static readonly IGen24JsonService gen24JsonService = IoC.TryGet<IGen24JsonService>()!;

    [FroniusProprietaryImport("PV_MODE_COMBINE_U16", FroniusDataType.Root)]
    public StringCombination? StringCombination
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Mppt1? Mppt1
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Mppt2? Mppt2
    {
        get;
        set => Set(ref field, value);
    }

    public double? WattPeakTotal => Mppt1?.WattPeak + Mppt2?.WattPeak;

    public object Clone()
    {
        return new Gen24Mppt
        {
            StringCombination = StringCombination,
            Mppt1 = Mppt1?.Clone() as Gen24Mppt1,
            Mppt2 = Mppt2?.Clone() as Gen24Mppt2,
        };
    }

    public static Gen24Mppt Parse(JToken? token)
    {
        var result = gen24JsonService.ReadFroniusData<Gen24Mppt>(token);
        result.Mppt1 = gen24JsonService.ReadFroniusData<Gen24Mppt1>(token?["mppt1"]);
        result.Mppt2 = gen24JsonService.ReadFroniusData<Gen24Mppt2>(token?["mppt2"]);
        return result;
    }
}
