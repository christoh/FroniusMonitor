using System.Collections.ObjectModel;

namespace De.Hochstaetter.Fronius.Models;

public class Gen24System : BindableBase
{
    private Gen24Inverter? inverter;

    public Gen24Inverter? Inverter
    {
        get => inverter;
        set => Set(ref inverter, value);
    }

    private Gen24Storage? storage;

    public Gen24Storage? Storage
    {
        get => storage;
        set => Set(ref storage, value, () => NotifyOfPropertyChange(nameof(StorageNetCapacity)));
    }

    private Gen24Restrictions? restrictions;

    public Gen24Restrictions? Restrictions
    {
        get => restrictions;
        set => Set(ref restrictions, value, () => NotifyOfPropertyChange(nameof(StorageNetCapacity)));
    }

    private Gen24DataManager? dataManager;

    public Gen24DataManager? DataManager
    {
        get => dataManager;
        set => Set(ref dataManager, value);
    }

    private Gen24PowerFlow? powerFlow;

    public Gen24PowerFlow? PowerFlow
    {
        get => powerFlow;
        set => Set(ref powerFlow, value);
    }

    private Gen24Cache? cache;

    public Gen24Cache? Cache
    {
        get => cache;
        set => Set(ref cache, value);
    }

    public double? StorageNetCapacity => Storage?.MaxCapacity * (Storage?.StateOfCharge - Restrictions?.MinStateOfCharge);

    public ObservableCollection<Gen24PowerMeter> Meters { get; } = new ObservableCollection<Gen24PowerMeter>();

    public Gen24PowerMeter? PrimaryPowerMeter => Meters.SingleOrDefault(m => m.Usage == MeterUsage.Inverter);
}