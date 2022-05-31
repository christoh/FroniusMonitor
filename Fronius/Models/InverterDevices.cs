namespace De.Hochstaetter.Fronius.Models
{
    public class InverterDevices : BaseResponse, IHierarchicalCollection
    {
        public ObservableCollection<Inverter> Inverters = new();
        public override string DisplayName => Resources.Inverters;
        IEnumerable IHierarchicalCollection.ItemsEnumerable => Inverters;
        IEnumerable IHierarchicalCollection.ChildrenEnumerable { get; } = Array.Empty<object>();
    }
}
