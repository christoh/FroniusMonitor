using De.Hochstaetter.Fronius.Models.Events;

namespace De.Hochstaetter.Fronius.Services;

public class DataControlService : IDataControlService
{
    private readonly ILogger<DataControlService> logger;

    public event EventHandler<DeviceUpdateEventArgs>? DeviceUpdate;

    public DataControlService(ILogger<DataControlService> logger)
    {
        this.logger = logger;
    }

    private ConcurrentDictionary<string, object> Entities { get; } = new();

    IReadOnlyDictionary<string, object> IDataControlService.Entities => Entities;

    public void AddOrUpdate(string id, object entity)
    {
        var isNew = !Entities.ContainsKey(id);
        Entities[id] = entity;

        if (isNew)
        {
            logger.LogInformation("Adding new device {Device}", entity is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id);
        }

        DeviceUpdate?.Invoke(this, new DeviceUpdateEventArgs(id, entity, isNew ? DeviceAction.Add : DeviceAction.Change));
    }

    public void AddOrUpdate(IHaveUniqueId entity) => AddOrUpdate(entity.Id, entity);

    public void Remove(string id)
    {
        if (Entities.TryRemove(id, out var entity))
        {
            logger.LogInformation("Removing device {Device}", entity is IHaveDisplayName haveDisplayName ? haveDisplayName.DisplayName : id);
            DeviceUpdate?.Invoke(this, new DeviceUpdateEventArgs(id, entity, DeviceAction.Delete));
        }
    }

    public async ValueTask AddOrUpdateAsync(IEnumerable<IHaveUniqueId> entities, CancellationToken token = default)
    {
        await Task.Run(() => entities.Apply(AddOrUpdate), token).ConfigureAwait(false);
    }
}