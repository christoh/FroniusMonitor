using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class InverterDevices : ResponseBase, IHierarchicalCollection
    {
        public ObservableCollection<Inverter> Inverters = new ObservableCollection<Inverter>();
        public override string DisplayName => Resources.Inverters;
        IEnumerable IHierarchicalCollection.ItemsEnumerable => Inverters;
        IEnumerable IHierarchicalCollection.ChildrenEnumerable { get; } = Array.Empty<object>();
    }
}
