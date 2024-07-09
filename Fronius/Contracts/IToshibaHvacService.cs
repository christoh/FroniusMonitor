namespace De.Hochstaetter.Fronius.Contracts;

public interface IToshibaHvacService
{
    public ValueTask Start(AzureConnection? azureConnection, string azureDeviceId);
    public ValueTask Stop();
    public bool IsRunning { get; }
    public bool IsConnected { get; }
    public ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings);
    public BindableCollection<ToshibaHvacMapping>? AllDevices { get; }

    public ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params Guid[] deviceUniqueIds) => SendDeviceCommand(state, deviceUniqueIds.Select(id => id.ToString("D")).ToArray());

    public ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params ToshibaHvacMappingDevice[] device) => SendDeviceCommand(state, device.Select(d => d.DeviceUniqueId.ToString("D")).ToArray());

    public event EventHandler<ToshibaHvacAzureSmMobileCommand>? LiveDataReceived;
}