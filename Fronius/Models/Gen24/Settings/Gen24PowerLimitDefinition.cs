namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public partial class Gen24PowerLimitDefinition : Gen24ParsingBase
{
    [ObservableProperty]
    [FroniusProprietaryImport("enabled", FroniusDataType.Root)]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("powerLimit", FroniusDataType.Root)]
    public partial double PowerLimit { get; set; }

    public static Gen24PowerLimitDefinition Parse(JToken? token)
    {
        return Gen24JsonService.ReadFroniusData<Gen24PowerLimitDefinition>(token);
    }

    public override object Clone() => MemberwiseClone();
}