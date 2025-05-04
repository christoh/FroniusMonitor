using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.Fronius.Models;

#pragma warning disable CS0618 // Type or member is obsolete
public class HomeAutomationSystem : BindableBase
{
    public Gen24Sensors? Gen24Sensors
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(StorageNetCapacity));
            NotifyOfPropertyChange(nameof(NetStateOfChange));
            NotifyOfPropertyChange(nameof(NetStateOfChangeIfFull));
            NotifyOfPropertyChange(nameof(MaxStorageNetCapacity));
        });
    }

    public Gen24Sensors? Gen24Sensors2
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24Config? Gen24Config
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(StorageNetCapacity));
            NotifyOfPropertyChange(nameof(NetStateOfChange));
            NotifyOfPropertyChange(nameof(NetStateOfChangeIfFull));
            NotifyOfPropertyChange(nameof(MaxStorageNetCapacity));
        });
    }

    public Gen24Config? Gen24Config2
    {
        get;
        set => Set(ref field, value);
    }

    public FritzBoxDeviceList? FritzBox
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24PowerFlow? SitePowerFlow
    {
        get;
        set => Set(ref field, value);
    }

    public WattPilot? WattPilot
    {
        get;
        set => Set(ref field, value);
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
#pragma warning restore CS0618 // Type or member is obsolete

