using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public enum ToshibaAcOperatingMode : byte
    {
        Recirculation = 0x41,
        Cooling = 0x42,
        Dehumidification = 0x43,
        Heating = 0x44,
        FanOnly = 0x45,
    }
}
