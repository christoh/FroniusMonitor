namespace De.Hochstaetter.Fronius.Models.Events;

public enum DeviceAction : byte
{
    Add, Change, Delete
}

public record DeviceUpdateEventArgs(string Id, ManagedDevice Device, DeviceAction DeviceAction);

public record SettingsChangedEventArgs(object? Parameters);

public record NewWattPilotFirmwareEventArgs(Version CurrentFirmware, Version NewFirmware, string Name, string SerialNumber);

public record WattPilotServiceStoppedEventArgs(WattPilot? WattPilot, WebConnection? WebConnection);

public record WattPilotUpdateEventArgs(WattPilot WattPilot, JObject JObject);