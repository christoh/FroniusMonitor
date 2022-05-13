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
