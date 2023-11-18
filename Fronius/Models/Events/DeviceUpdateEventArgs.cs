namespace De.Hochstaetter.Fronius.Models.Events
{
    public enum DeviceAction : byte
    {
        Add, Change, Delete
    }

    public record DeviceUpdateEventArgs(string Id, object Device, DeviceAction DeviceAction);

    public record SettingsChangedEventArgs(object? Parameters);
}
