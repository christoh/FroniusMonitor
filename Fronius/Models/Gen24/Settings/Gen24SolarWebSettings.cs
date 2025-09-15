namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public partial class Gen24SolarWebSettings : BindableBase, ICloneable
{
    [ObservableProperty]
    [FroniusProprietaryImport("customerRemoteControlProfileActive", FroniusDataType.Root)]
    public partial bool CustomerRemoteControlProfileActive { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("enableRemoteControl", FroniusDataType.Root)]
    public partial bool EnableRemoteControl { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("sendDataToSolarWeb", FroniusDataType.Root)]
    public partial bool SendDataToSolarWeb { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("technicianRemoteControlProfileActive", FroniusDataType.Root)]
    public partial bool TechnicianRemoteControlProfileActive { get; set; }

    public object Clone() => MemberwiseClone();
}