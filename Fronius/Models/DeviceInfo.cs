using De.Hochstaetter.Fronius.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class DeviceInfo : IHaveDisplayName
    {
        public DeviceClass DeviceClass { get; init; }
        public string? SerialNumber { get; init; }
        public int Id { get; init; }
        public int DeviceType { get; init; }

        public virtual string Model => DeviceType.ToDeviceString();
        public virtual string DisplayName => $"{Model} #{Id}";
        public override string ToString() => DisplayName;
    }
}
