namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public partial class Gen24PowerLimitsVisualization : Gen24ParsingBase
{
    [ObservableProperty]
    [FroniusProprietaryImport("wattPeakReferenceValue", FroniusDataType.Root)]
    public partial double WattPeakReferenceValue { get; set; }

    public static Gen24PowerLimitsVisualization Parse(JToken? token)
    {
        return Gen24JsonService.ReadFroniusData<Gen24PowerLimitsVisualization>(token);
    }

    public override object Clone() => MemberwiseClone();
}