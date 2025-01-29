namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public class Gen24SolarWebSettings : BindableBase, ICloneable
{
    [FroniusProprietaryImport("customerRemoteControlProfileActive", FroniusDataType.Root)]
    public bool CustomerRemoteControlProfileActive
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("enableRemoteControl", FroniusDataType.Root)]
    public bool EnableRemoteControl
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("sendDataToSolarWeb", FroniusDataType.Root)]
    public bool SendDataToSolarWeb
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("technicianRemoteControlProfileActive", FroniusDataType.Root)]
    public bool TechnicianRemoteControlProfileActive
    {
        get;
        set => Set(ref field, value);
    }

    public object Clone() => MemberwiseClone();
}