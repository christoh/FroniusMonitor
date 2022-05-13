namespace De.Hochstaetter.Fronius.Models
{
    public class SmartMeterDevices : BaseResponse, IHierarchicalCollection
    {
        IEnumerable IHierarchicalCollection.ItemsEnumerable => SmartMeters;

        IEnumerable IHierarchicalCollection.ChildrenEnumerable => Array.Empty<object>();

        public ICollection<SmartMeter> SmartMeters = new List<SmartMeter>();

        public override string DisplayName => Resources.Meters;
    }
}
