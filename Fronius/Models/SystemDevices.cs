using De.Hochstaetter.Fronius.Localization;

namespace De.Hochstaetter.Fronius.Models
{
    public class SystemDevices : BaseResponse
    {
        public ICollection<DeviceInfo> Devices { get; } = new List<DeviceInfo>();
        public override string DisplayName => Resources.Devices;
    }
}
