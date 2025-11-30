namespace De.Hochstaetter.Fronius.Models;

public partial class HomeAutomationSystem : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(NetStateOfChange), nameof(NetStateOfChangeIfFull), nameof(MaxStorageNetCapacity))]
    public partial Gen24Sensors? Gen24Sensors { get; set; }

    [ObservableProperty]
    public partial Gen24Sensors? Gen24Sensors2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StorageNetCapacity), nameof(NetStateOfChange), nameof(NetStateOfChangeIfFull), nameof(MaxStorageNetCapacity))]
    public partial Gen24Config? Gen24Config { get; set; }

    [ObservableProperty]
    public partial Gen24Config? Gen24Config2 { get; set; }

    [ObservableProperty]
    public partial FritzBoxDeviceList? FritzBox { get; set; }

    [ObservableProperty]
    public partial Gen24PowerFlow? SitePowerFlow { get; set; }

    [ObservableProperty]
    public partial WattPilot? WattPilot { get; set; }

    public double? StorageNetCapacity => Gen24Sensors?.Storage?.MaxCapacity * NetStateOfChangeIfFull;

    public double? MaxStorageNetCapacity => Gen24Sensors?.Storage?.MaxCapacity * GetNetStateOfChargeIfFull((Gen24Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? 100 : Gen24Config?.BatterySettings?.SocMax ?? 100) / 100d);

    public double? NetStateOfChange => StorageNetCapacity / MaxStorageNetCapacity;

    public double? NetStateOfChangeIfFull => GetNetStateOfChargeIfFull(Gen24Sensors?.Storage?.StateOfCharge);

    private double? GetNetStateOfChargeIfFull(double? stateOfCharge)
    {
        var socMin = Gen24Config?.BatterySettings?.SocMin ?? 0;
        var socMinPreserve = Gen24Config?.BatterySettings?.EnableSystemDeadlockPrevention is true? Gen24Config?.BatterySettings?.SocMinPreserve ?? -1:-1;
        return stateOfCharge - Math.Max(Gen24Config?.BatterySettings?.BackupReserve ?? 0, Math.Max(socMinPreserve, Gen24Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? 5 : socMin)) / 100d;
    }
}

