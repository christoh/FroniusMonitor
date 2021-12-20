using De.Hochstaetter.Fronius.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class SystemDevices : BaseResponse
    {
        public ICollection<DeviceInfo> Devices { get; } = new List<DeviceInfo>();
        public override string DisplayName => Resources.Devices;
    }
}
