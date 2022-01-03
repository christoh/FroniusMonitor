using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IPowerMeter1P:IHaveDisplayName
    {
        double? PowerWatts { get; }
        double? Voltage { get; }
        double? Current => PowerWatts / Voltage;
        double? Temperature { get; }
        double? Frequency { get; }
        double? EnergyKiloWattHours { get; }
        bool IsPresent { get; }
        bool? IsTurnedOn { get; }
        string? Model { get; }
    }
}
