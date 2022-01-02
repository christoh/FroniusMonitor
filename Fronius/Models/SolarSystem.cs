using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models;

public class SolarSystem : BindableBase, IHierarchicalCollection
{
    public string DisplayName => Resources.MySolarSystem;

    public ObservableCollection<DeviceGroup> DeviceGroups { get; } = new ObservableCollection<DeviceGroup>();

    public IEnumerable<Storage> Storages => DeviceGroups.SingleOrDefault(g => g.DeviceClass == DeviceClass.Storage)?.Devices.OfType<Storage>() ?? Array.Empty<Storage>();

    public IEnumerable<Inverter> Inverters => DeviceGroups.SingleOrDefault(g => g.DeviceClass == DeviceClass.Inverter)?.Devices.OfType<Inverter>() ?? Array.Empty<Inverter>();

    public IEnumerable<SmartMeter> Meters => DeviceGroups.SingleOrDefault(g => g.DeviceClass == DeviceClass.Meter)?.Devices.OfType<SmartMeter>() ?? Array.Empty<SmartMeter>();

    public SmartMeter? PrimaryMeter => Meters.FirstOrDefault(m => m.Usage == MeterUsage.Inverter);

    public Inverter? PrimaryInverter => Inverters.FirstOrDefault();

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

    IEnumerable IHierarchicalCollection.ItemsEnumerable { get; } = Array.Empty<object>();

    IEnumerable IHierarchicalCollection.ChildrenEnumerable => DeviceGroups;

    public override string ToString() => DisplayName;
}

