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
    public class SolarSystem : IHierarchicalCollection
    {
        public string DisplayName => Resources.MySolarSystem;

        public ObservableCollection<DeviceGroup> DeviceGroups { get; } = new ObservableCollection<DeviceGroup>();

        IEnumerable IHierarchicalCollection.ItemsEnumerable { get; } = Array.Empty<object>();

        IEnumerable IHierarchicalCollection.ChildrenEnumerable => DeviceGroups;

        public override string ToString() => DisplayName;
    }
}
