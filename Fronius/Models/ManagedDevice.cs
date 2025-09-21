namespace De.Hochstaetter.Fronius.Models;

public record ManagedDevice(IHaveUniqueId Device, object? Credentials = null, Type? ServiceType = null, bool SupportsPushMessages = false)
{
    public DateTime LastUpdated { get; } = DateTime.UtcNow;
}
