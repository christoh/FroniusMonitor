using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class SystemDevices : BindableBase
    {
        private DateTime timeStamp;
        public DateTime TimeStamp
        {
            get => timeStamp;
            set => Set(ref timeStamp, value);
        }

        public ICollection<DeviceInfo> Devices { get; } = new List<DeviceInfo>();
    }
}
