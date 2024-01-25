using De.Hochstaetter.Fronius.Models.Events;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IDataControlService
    {
        public event EventHandler<Models.Events.DeviceUpdateEventArgs>? DeviceUpdate;
        public IReadOnlyDictionary<string, object> Entities { get; }
        public void AddOrUpdate(string id, object entity);
        public ValueTask AddOrUpdateAsync(IEnumerable<IHaveUniqueId> entities, CancellationToken token = default);
        public void AddOrUpdate(IHaveUniqueId entity);
        public ValueTask RemoveAsync(IEnumerable<string> entities, CancellationToken token = default);
        public void Remove(string id);
    }
}
