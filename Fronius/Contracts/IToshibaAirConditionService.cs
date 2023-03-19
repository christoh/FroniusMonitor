using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IToshibaAirConditionService
    {
        public ValueTask Start();
        public ValueTask Stop();
        public bool IsRunning { get; }
        public bool IsConnected { get; }
        public ValueTask<string> SendDeviceCommand(ToshibaAcStateData state, params string[] targetIdStrings);
        public BindableCollection<ToshibaAcMapping>? AllDevices { get; }
        public SettingsBase Settings { get; }

        public ValueTask<string> SendDeviceCommand(ToshibaAcStateData state, params Guid[] deviceUniqueIds) => SendDeviceCommand(state, deviceUniqueIds.Select(id => id.ToString("D")).ToArray());

        public ValueTask<string> SendDeviceCommand(ToshibaAcStateData state, params ToshibaAcMappingDevice[] device) => SendDeviceCommand(state, device.Select(d => d.DeviceUniqueId.ToString("D")).ToArray());

        public event EventHandler<ToshibaHvacAzureSmMobileCommand>? LiveDataReceived;
    }
}
