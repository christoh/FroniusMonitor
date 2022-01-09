using De.Hochstaetter.Fronius.Contracts;
using System.Collections;

namespace De.Hochstaetter.Fronius.Models
{
    public class DeviceGroup : IHaveDisplayName, IHierarchicalCollection
    {
        public string DisplayName => DeviceClass.ToPluralName();

        public DeviceClass DeviceClass { get; init; }

        public ICollection<object> Devices { get; } = new List<object>();

        IEnumerable IHierarchicalCollection.ItemsEnumerable => Devices;

        IEnumerable IHierarchicalCollection.ChildrenEnumerable => Array.Empty<object>();

        public override string ToString() => DisplayName;
    }
}
