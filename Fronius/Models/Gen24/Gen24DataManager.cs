namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24DataManager : Gen24DeviceBase
{
    private double? inverterAcPower;

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_SUM_MEAN_F32")]
    public double? InverterAcPower
    {
        get => inverterAcPower;
        set => Set(ref inverterAcPower, value);
    }

    private double? storagePower;
    [FroniusProprietaryImport("BAT_POWERACTIVE_F64")]
    public double? StoragePower
    {
        get => storagePower;
        set => Set(ref storagePower, value);
    }

    private ushort? energyInfluencingDevices;
    [FroniusProprietaryImport("DEVICE_VALUE_ENERGY_INFLUENCING_DEVICES_U16")]
    public ushort? EnergyInfluencingDevices
    {
        get => energyInfluencingDevices;
        set => Set(ref energyInfluencingDevices, value);
    }

    private ushort? powerInfluencingDevices;
    [FroniusProprietaryImport("DEVICE_VALUE_POWER_INFLUENCING_DEVICES_U16")]
    public ushort? PowerInfluencingDevices
    {
        get => powerInfluencingDevices;
        set => Set(ref powerInfluencingDevices, value);
    }

    private double? solarPower;
    [FroniusProprietaryImport("PV_POWERACTIVE_SUM_F64")]
    public double? SolarPower
    {
        get => solarPower;
        set => Set(ref solarPower, value);
    }

    private double? inverterLifeTimeEnergyProduced;
    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_EVER_SINCE_SUM_F64", Unit.Joule)]
    public double? InverterLifeTimeEnergyProduced
    {
        get => inverterLifeTimeEnergyProduced;
        set => Set(ref inverterLifeTimeEnergyProduced, value);
    }
}