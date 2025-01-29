namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public class Gen24PowerLimitDefinition : Gen24ParsingBase
{
    [FroniusProprietaryImport("enabled", FroniusDataType.Root)]
    public bool IsEnabled
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("powerLimit", FroniusDataType.Root)]
    public double PowerLimit
    {
        get;
        set => Set(ref field, value);
    }

    public static Gen24PowerLimitDefinition Parse(JToken? token)
    {
            return Gen24JsonService.ReadFroniusData<Gen24PowerLimitDefinition>(token);
        }

    public override object Clone() => MemberwiseClone();
}