using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Localization;
using System.Collections;
using System.Collections.ObjectModel;

namespace De.Hochstaetter.Fronius.Models
{
    public class InverterDevices : BaseResponse, IHierarchicalCollection
    {
        public ObservableCollection<Inverter> Inverters = new ObservableCollection<Inverter>();
        public override string DisplayName => Resources.Inverters;
        IEnumerable IHierarchicalCollection.ItemsEnumerable => Inverters;
        IEnumerable IHierarchicalCollection.ChildrenEnumerable { get; } = Array.Empty<object>();
    }
}
