namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public class Gen24PowerLimitDefinition : Gen24ParsingBase
{
    private bool isEnabled;

    [FroniusProprietaryImport("enabled", FroniusDataType.Root)]
    public bool IsEnabled
    {
        get => isEnabled;
        set => Set(ref isEnabled, value);
    }

    private double powerLimit;

    [FroniusProprietaryImport("powerLimit", FroniusDataType.Root)]
    public double PowerLimit
    {
        get => powerLimit;
        set => Set(ref powerLimit, value);
    }

    public static Gen24PowerLimitDefinition Parse(JToken? token)
    {
            return Gen24JsonService.ReadFroniusData<Gen24PowerLimitDefinition>(token);
        }

    public override object Clone() => MemberwiseClone();
}