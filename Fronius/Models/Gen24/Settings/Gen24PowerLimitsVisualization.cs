namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public class Gen24PowerLimitsVisualization : Gen24ParsingBase
{
    [FroniusProprietaryImport("wattPeakReferenceValue", FroniusDataType.Root)]
    public double WattPeakReferenceValue
    {
        get;
        set => Set(ref field, value);
    }

    public static Gen24PowerLimitsVisualization Parse(JToken? token)
    {
        return Gen24JsonService.ReadFroniusData<Gen24PowerLimitsVisualization>(token);
    }

    public override object Clone() => MemberwiseClone();
}