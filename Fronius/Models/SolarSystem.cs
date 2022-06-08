namespace De.Hochstaetter.Fronius.Models;

public class SolarSystem : BindableBase, IHierarchicalCollection
{
    public string DisplayName => Resources.MySolarSystem;

    public ObservableCollection<DeviceGroup> DeviceGroups { get; } = new();

    public IEnumerable<Storage> Storages => DeviceGroups.SingleOrDefault(g => g.DeviceClass == DeviceClass.Storage)?.Devices.OfType<Storage>() ?? Array.Empty<Storage>();

    public IEnumerable<Inverter> Inverters => DeviceGroups.SingleOrDefault(g => g.DeviceClass == DeviceClass.Inverter)?.Devices.OfType<Inverter>() ?? Array.Empty<Inverter>();

    public IEnumerable<SmartMeter> Meters => DeviceGroups.SingleOrDefault(g => g.DeviceClass == DeviceClass.Meter)?.Devices.OfType<SmartMeter>() ?? Array.Empty<SmartMeter>();

    public SmartMeter? PrimaryMeter => Meters.FirstOrDefault(m => m.Usage == MeterUsage.Inverter);

    public Inverter? PrimaryInverter => Inverters.FirstOrDefault();

    private Gen24System? gen24System;

    public Gen24System? Gen24System
    {
        get => gen24System;
        set => Set(ref gen24System, value);
    }

    private Gen24Components? components;

    public Gen24Components? Components
    {
        get => components;
        set => Set(ref components, value);
    }

    private Gen24Versions? versions;

    public Gen24Versions? Versions
    {
        get => versions;
        set => Set(ref versions, value);
    }

    private FritzBoxDeviceList? fritzBox;

    public FritzBoxDeviceList? FritzBox
    {
        get => fritzBox;
        set => Set(ref fritzBox, value);
    }

    private PowerFlow? powerFlow;

    public PowerFlow? PowerFlow
    {
        get => powerFlow;
        set => Set(ref powerFlow, value);
    }

    private WattPilot? wattPilot;

    public WattPilot? WattPilot
    {
        get => wattPilot;
        set => Set(ref wattPilot, value);
    }

    IEnumerable IHierarchicalCollection.ItemsEnumerable { get; } = Array.Empty<object>();

    IEnumerable IHierarchicalCollection.ChildrenEnumerable => DeviceGroups;

    public override string ToString() => DisplayName;
}

