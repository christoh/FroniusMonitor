using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public enum ToshibaAcFanSpeed:byte
    {
        Quiet = 0x31,
        Auto = 0x41,
        Manual1 = 0x32,
        Manual2 = 0x33,
        Manual3 = 0x34,
        Manual4 = 0x35,
        Manual5 = 0x36,
    }
}
