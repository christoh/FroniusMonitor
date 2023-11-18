using De.Hochstaetter.Fronius.Models.Events;

namespace De.Hochstaetter.Fronius.Services;

public class DataControlService(ILogger<DataControlService> logger) : IDataControlService
{
    public event EventHandler<DeviceUpdateEventArgs>? DeviceUpdate;

    private ConcurrentDictionary<string, object> Entities { get; } = new();

    IReadOnlyDictionary<string, object> IDataControlService.Entities => Entities;

    public void AddOrUpdate(string id, object entity)
    {
        var isNew = !Entities.ContainsKey(id);
        Entities[id] = entity;
        var displayName = entity is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id;

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

    public void AddOrUpdate(IHaveUniqueId entity) => AddOrUpdate(entity.Id, entity);

    public async ValueTask RemoveAsync(IEnumerable<string> entities, CancellationToken token = default)
    {
        await Task.Run(() => entities.Apply(Remove), token).ConfigureAwait(false);
    }

    public void Remove(string id)
    {
        if (Entities.TryRemove(id, out var entity))
        {
            logger.LogInformation("Removing device {Device}", entity is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id);
            DeviceUpdate?.Invoke(this, new DeviceUpdateEventArgs(id, entity, DeviceAction.Delete));
        }
        else
        {
            logger.LogTrace("Could not remove device {Device}", entity is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id);
        }
    }

    public async ValueTask AddOrUpdateAsync(IEnumerable<IHaveUniqueId> entities, CancellationToken token = default)
    {
        await Task.Run(() => entities.Apply(AddOrUpdate), token).ConfigureAwait(false);
    }
}