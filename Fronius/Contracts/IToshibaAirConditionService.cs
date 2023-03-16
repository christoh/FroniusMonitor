using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IToshibaAirConditionService
    {
        public ValueTask Start();
        public void Stop();
        public bool IsRunning { get; }
        public bool IsConnected { get; }
        public ValueTask SendDeviceCommand(ToshibaAcStateData state, params string[] targetIdStrings);
        public ObservableCollection<ToshibaAcMapping>? AllDevices { get; }
        public SettingsBase Settings { get; }

        public ValueTask SendDeviceCommand(ToshibaAcStateData state, params Guid[] deviceUniqueIds) => SendDeviceCommand(state, deviceUniqueIds.Select(id => id.ToString("D")).ToArray());

        public ValueTask SendDeviceCommand(ToshibaAcStateData state, params ToshibaAcMappingDevice[] device) => SendDeviceCommand(state, device.Select(d => d.DeviceUniqueId.ToString("D")).ToArray());

    }
}
