namespace De.Hochstaetter.Fronius.Services;

public class DataControlService(ILogger<DataControlService> logger) : IDataControlService
{
    public event EventHandler<DeviceUpdateEventArgs>? DeviceUpdate;

    private readonly Lock updateLock = new();

    // Do not use ConcurrentDictionary here to preserve the order
    private Dictionary<string, ManagedDevice> Entities { get; } = new();

    IReadOnlyDictionary<string, ManagedDevice> IDataControlService.Entities => Entities;

    public void AddOrUpdate(string id, ManagedDevice entity)
    {
        var isNew = !Entities.ContainsKey(id);

        lock (updateLock)
        {
            Entities[id] = entity;
        }

        var displayName = entity.Device is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id;

        if (isNew)
        {
            logger.LogInformation("Adding new device {Device}", displayName);
        }
        else
        {
            logger.LogTrace("Updating device {Device}", displayName);
        }

        DeviceUpdate?.Invoke(this, new DeviceUpdateEventArgs(id, entity, isNew ? DeviceAction.Add : DeviceAction.Change));
    }

    public void AddOrUpdate(ManagedDevice entity) => AddOrUpdate(entity.Device.Id, entity);

    public async ValueTask RemoveAsync(IEnumerable<string> ids, CancellationToken token = default)
    {
        await Task.Run(() => ids.Apply(Remove), token).ConfigureAwait(false);
    }

    public void Remove(string id)
    {
        bool success;
        ManagedDevice? entity;

        lock (updateLock)
        {
            success = Entities.Remove(id, out entity);
        }
        
        if (success && entity is not null)
        {
            logger.LogInformation("Removing device {Device}", entity.Device is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id);
            DeviceUpdate?.Invoke(this, new DeviceUpdateEventArgs(id, entity, DeviceAction.Delete));
        }
        else
        {
            logger.LogTrace("Could not remove device {Device}", entity?.Device is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id);
        }
    }

    public async ValueTask AddOrUpdateAsync(IEnumerable<ManagedDevice> entities, CancellationToken token = default)
    {
        await Task.Run(() => entities.Apply(AddOrUpdate), token).ConfigureAwait(false);
    }
}