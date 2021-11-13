using De.Hochstaetter.Fronius.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Models
{
    public enum DeviceClass:byte
    {
        Unknown,
        Inverter,
        Meter,
        Storage,
        Ohmpilot,
        SensorCard,
        StringControl,
    }

    public static partial class Extensions
    {
        public static string ToDisplayName(this DeviceClass deviceClass)
        {
            return deviceClass switch
            {
                DeviceClass.Inverter=>Resources.Inverter,
                DeviceClass.StringControl=>Resources.StringControl,
                DeviceClass.Meter=>Resources.Meter,
                DeviceClass.Ohmpilot=>Resources.Ohmpilot,
                DeviceClass.SensorCard=>Resources.SensorCard,
                DeviceClass.Storage=>Resources.Storage,
                _=>Resources.Unknown
            };
        }
    }
}
