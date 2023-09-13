﻿namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public class Gen24PowerLimitsVisualization : Gen24ParsingBase
{

    private uint wattPeakReferenceValue;
    [FroniusProprietaryImport("wattPeakReferenceValue", FroniusDataType.Root)]
    public uint WattPeakReferenceValue
    {
        get => wattPeakReferenceValue;
        set => Set(ref wattPeakReferenceValue, value);
    }

    public static Gen24PowerLimitsVisualization Parse(JToken? token)
    {
        return Gen24JsonService.ReadFroniusData<Gen24PowerLimitsVisualization>(token);
    }

    public override object Clone() => MemberwiseClone();
}