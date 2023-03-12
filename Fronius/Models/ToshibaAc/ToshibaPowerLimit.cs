using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public enum ToshibaPowerLimit : byte
    {
        Low = 0x32,
        Medium = 0x4b,
        Unlimited = 0x64,
    }
}
