namespace De.Hochstaetter.Fronius.Models.Events;

public enum DeviceAction : byte
{
    Add, Change, Delete
}

public record DeviceUpdateEventArgs(string Id, ManagedDevice Device, DeviceAction DeviceAction);

public record SettingsChangedEventArgs(object? Parameters);

public record NewWattPilotFirmwareEventArgs(string CurrentFirmware, string NewFirmware, string Name, string SerialNumber);

public record WattPilotServiceStoppedEventArgs(WattPilot? WattPilot, WebConnection? WebConnection);

public record WattPilotUpdateEventArgs(WattPilot WattPilot, JsonObject JsonObject);
/// <summary>One air conditioner has new state - a live update or a heartbeat - and here is the message that brought it.</summary>
public record ToshibaHvacDeviceUpdatedEventArgs(ToshibaHvacMappingDevice Device, ToshibaHvacAzureSmMobileCommand Command);
