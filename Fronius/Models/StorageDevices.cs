using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class StorageDevices : BaseResponse, IHierarchicalCollection
    {
        IEnumerable IHierarchicalCollection.ItemsEnumerable => Storages;

        IEnumerable IHierarchicalCollection.ChildrenEnumerable => Array.Empty<object>();

        public ICollection<Storage> Storages = new List<Storage>();

        public override string DisplayName => Resources.Storages;
    }
}
