namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24DataManager : Gen24DeviceBase
{
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_SUM_MEAN_F32")]
    public double? InverterAcPower
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_POWERACTIVE_F64")]
    public double? StoragePower
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DEVICE_VALUE_ENERGY_INFLUENCING_DEVICES_U16")]
    public ushort? EnergyInfluencingDevices
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DEVICE_VALUE_POWER_INFLUENCING_DEVICES_U16")]
    public ushort? PowerInfluencingDevices
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("PV_POWERACTIVE_SUM_F64")]
    public double? SolarPower
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_EVER_SINCE_SUM_F64", Unit.Joule)]
    public double? InverterLifeTimeEnergyProduced
    {
        get;
        set => Set(ref field, value);
    }
}