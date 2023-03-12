using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public enum ToshibaAcPowerSetting : byte
    {
        Normal = 0x0,
        Eco = 0x3,
        HiPower = 0x1,
        Quiet1 = 0xa,
        Quiet2 = 0x2,
    }
}
