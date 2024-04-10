using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.Fronius.Models;

public class HomeAutomationSystem : BindableBase
{
    private Gen24Sensors? gen24Sensors;

    public Gen24Sensors? Gen24Sensors
    {
        get => gen24Sensors;
        set => Set(ref gen24Sensors, value, () =>
        {
            NotifyOfPropertyChange(nameof(StorageNetCapacity));
            NotifyOfPropertyChange(nameof(NetStateOfChange));
            NotifyOfPropertyChange(nameof(NetStateOfChangeIfFull));
            NotifyOfPropertyChange(nameof(MaxStorageNetCapacity));
        });
    }

    private Gen24Sensors? gen24Sensors2;

    public Gen24Sensors? Gen24Sensors2
    {
        get => gen24Sensors2;
        set => Set(ref gen24Sensors2, value);
    }

    private Gen24Config? gen24Config;

    public Gen24Config? Gen24Config
    {
        get => gen24Config;
        set => Set(ref gen24Config, value, () =>
        {
            NotifyOfPropertyChange(nameof(StorageNetCapacity));
            NotifyOfPropertyChange(nameof(NetStateOfChange));
            NotifyOfPropertyChange(nameof(NetStateOfChangeIfFull));
            NotifyOfPropertyChange(nameof(MaxStorageNetCapacity));
        });
    }

    private Gen24Config? gen24Config2;

    public Gen24Config? Gen24Config2
    {
        get => gen24Config2;
        set => Set(ref gen24Config2, value);
    }

    private FritzBoxDeviceList? fritzBox;

    public FritzBoxDeviceList? FritzBox
    {
        get => fritzBox;
        set => Set(ref fritzBox, value);
    }

    private Gen24PowerFlow? sitePowerFlow;

    public Gen24PowerFlow? SitePowerFlow
    {
        get => sitePowerFlow;
        set => Set(ref sitePowerFlow, value);
    }

    private WattPilot? wattPilot;

    public WattPilot? WattPilot
    {
        get => wattPilot;
        set => Set(ref wattPilot, value);
    }

    public double? StorageNetCapacity => Gen24Sensors?.Storage?.MaxCapacity * NetStateOfChangeIfFull;

    public double? MaxStorageNetCapacity => Gen24Sensors?.Storage?.MaxCapacity * GetNetStateOfChargeIfFull((Gen24Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? 100 : Gen24Config?.BatterySettings?.SocMax ?? 100) / 100d);

    public double? NetStateOfChange => StorageNetCapacity / MaxStorageNetCapacity;

    public double? NetStateOfChangeIfFull => GetNetStateOfChargeIfFull(Gen24Sensors?.Storage?.StateOfCharge);

    private double? GetNetStateOfChargeIfFull(double? stateOfCharge)
    {
        return stateOfCharge - Math.Max(Gen24Config?.BatterySettings?.BackupReserve ?? 0, Gen24Config?.BatterySettings?.Limits is SocLimits.UseManufacturerDefault or null ? 5 : Gen24Config?.BatterySettings?.SocMin ?? 0) / 100d;
    }
}

