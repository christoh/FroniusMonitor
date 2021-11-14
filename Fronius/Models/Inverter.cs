using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public class Inverter : DeviceInfo
    {
        public Inverter()
        {
            DeviceClass = DeviceClass.Inverter;
        }

        public string? CustomName { get; init; } = string.Empty;
        public int ErrorCode { get; init; }
        public double MaxPvPowerWatts { get; init; }
        public bool Show { get; init; }
        public InverterStatus Status { get; init; }
        public string InverterStatusString=>Status.ToDisplayName();
        public override string DisplayName=>!string.IsNullOrWhiteSpace(CustomName)?$"{CustomName} ({Model} #{Id})":$"{Model} #{Id}";
    }
}
