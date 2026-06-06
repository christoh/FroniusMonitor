namespace De.Hochstaetter.Fronius.Models;

public partial class HomeAutomationSystem : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(NetStateOfChange), nameof(MaxStorageNetCapacity))]
    public partial Gen24Sensors? Gen24Sensors { get; set; }

    [ObservableProperty]
    public partial Gen24Sensors? Gen24Sensors2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(NetStateOfChange), nameof(MaxStorageNetCapacity))]
    public partial Gen24Config? Gen24Config { get; set; }

    [ObservableProperty]
    public partial Gen24Config? Gen24Config2 { get; set; }

    [ObservableProperty]
    public partial FritzBoxDeviceList? FritzBox { get; set; }

    [ObservableProperty]
    public partial Gen24PowerFlow? SitePowerFlow { get; set; }

    [ObservableProperty]
    public partial WattPilot? WattPilot { get; set; }

    public double? StorageNetCapacity => MaxStorageNetCapacity * NetStateOfChange;

    public double? MaxStorageNetCapacity => (1 - GetSocMin()) * Gen24Sensors?.Storage?.MaxCapacity;

    public double? NetStateOfChange => GetNetStateOfCharge(Gen24Sensors?.Storage?.StateOfCharge);

    private double GetSocMin()
    {
        var socMinConfig = Gen24Config?.BatterySettings?.SocMin ?? 0;
        var socMinPreserve = Gen24Config?.BatterySettings?.EnableSystemDeadlockPrevention is true ? Gen24Config?.BatterySettings?.SocMinPreserve ?? -1 : -1;

        var socMinManufacturer = Gen24Sensors?.Storage?.Manufacturer switch
        {
            "BYD" => 5d,
            _ => 0d,
        };

        return NumberExtensions.Max
        (
            Gen24Config?.BatterySettings?.BackupReserve ?? 0,
            socMinPreserve,
            Gen24Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? socMinManufacturer : socMinConfig
        ) / 100d;
    }

    private double? GetNetStateOfCharge(double? grossStateOfCharge)
    {
        var socMin = GetSocMin();
        return (grossStateOfCharge - socMin) / (1 - socMin);
    }
}

