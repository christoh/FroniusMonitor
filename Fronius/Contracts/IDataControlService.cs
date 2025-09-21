namespace De.Hochstaetter.Fronius.Contracts;

public interface IDataControlService
{
    public event EventHandler<DeviceUpdateEventArgs>? DeviceUpdate;
    public IReadOnlyDictionary<string, ManagedDevice> Entities { get; }
    public void AddOrUpdate(string id, ManagedDevice entity);
    public ValueTask AddOrUpdateAsync(IEnumerable<ManagedDevice> entities, CancellationToken token = default);
    public void AddOrUpdate(ManagedDevice entity);
    public ValueTask RemoveAsync(IEnumerable<string> ids, CancellationToken token = default);
    public void Remove(string id);
}