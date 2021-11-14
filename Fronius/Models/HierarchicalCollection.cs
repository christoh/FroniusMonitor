using De.Hochstaetter.Fronius.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class HierarchicalCollection : BindableBase, IHierarchicalCollection
    {
        private string displayName=string.Empty;

        public string DisplayName
        {
            get => displayName;
            set => Set(ref displayName, value);
        }

        public ObservableCollection<HierarchicalCollection> Children { get; } = new ObservableCollection<HierarchicalCollection>();

        public ObservableCollection<object> Items { get; } = new ObservableCollection<object>();

        IEnumerable IHierarchicalCollection.ChildrenEnumerable => Children;

        IEnumerable IHierarchicalCollection.ItemsEnumerable => Items;

        public IEnumerable AllItems
        {
            get
            {
                foreach (var item in Items)
                {
                    yield return item;
                }

                foreach (var child in Children)
                {
                    foreach (var childItem in child.AllItems)
                    {
                        yield return childItem;
                    }
                }
            }
        }
    }
}
