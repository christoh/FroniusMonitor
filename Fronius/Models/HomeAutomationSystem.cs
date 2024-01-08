namespace De.Hochstaetter.Fronius.Models;

public class HomeAutomationSystem : BindableBase
{
    private Gen24Sensors? gen24Sensors;

    public Gen24Sensors? Gen24Sensors
    {
        get => gen24Sensors;
        set => Set(ref gen24Sensors, value, () => NotifyOfPropertyChange(nameof(StorageNetCapacity)));
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
        set => Set(ref gen24Config, value, () => NotifyOfPropertyChange(nameof(StorageNetCapacity)));
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

    public double? StorageNetCapacity => Gen24Sensors?.Storage?.MaxCapacity * (Gen24Sensors?.Storage?.StateOfCharge - (Gen24Sensors?.Restrictions?.MinStateOfCharge ?? (Math.Max(Gen24Config?.BatterySettings?.BackupReserve ?? 0, Gen24Config?.BatterySettings?.SocMin ?? 0) / 100d)));
}

