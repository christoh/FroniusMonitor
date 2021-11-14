using De.Hochstaetter.Fronius.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public enum InverterStatus : byte
    {
        StartUp0 = 0,
        StartUp1 = 1,
        StartUp2 = 2,
        StartUp3 = 3,
        StartUp4 = 4,
        StartUp5 = 5,
        StartUp6 = 6,
        Running = 7,
        Standby = 8,
        Booting = 9,
        Error = 10,
        Idle = 11,
        Ready = 12,
        Sleeping = 13,
        Unknown = 255,
    }

    public static partial class Extensions
    {
        public static string ToDisplayName(this InverterStatus status)
        {
            return status switch
            {
                InverterStatus.StartUp0 => Resources.StartUp0,
                InverterStatus.StartUp1 => Resources.StartUp1,
                InverterStatus.StartUp2 => Resources.StartUp2,
                InverterStatus.StartUp3 => Resources.StartUp3,
                InverterStatus.StartUp4 => Resources.StartUp4,
                InverterStatus.StartUp5 => Resources.StartUp5,
                InverterStatus.StartUp6 => Resources.StartUp6,
                InverterStatus.Running => Resources.Running,
                InverterStatus.Standby => Resources.Standby,
                InverterStatus.Booting => Resources.Booting,
                InverterStatus.Error => Resources.Error,
                InverterStatus.Idle => Resources.Idle,
                InverterStatus.Ready => Resources.Ready,
                InverterStatus.Sleeping => Resources.Sleeping,
                InverterStatus.Unknown => Resources.Unknown,
                _ => Resources.Invalid
            };
        }
    }
}
