using De.Hochstaetter.Fronius.Localization;

namespace De.Hochstaetter.Fronius.Models
{
    public enum DeviceClass : byte
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
        public static string ToDisplayName(this DeviceClass deviceClass) => deviceClass switch
        {
            DeviceClass.Inverter => Resources.Inverter,
            DeviceClass.StringControl => Resources.StringControl,
            DeviceClass.Meter => Resources.Meter,
            DeviceClass.Ohmpilot => Resources.Ohmpilot,
            DeviceClass.SensorCard => Resources.SensorCard,
            DeviceClass.Storage => Resources.Storage,
            _ => Resources.Unknown
        };

        public static string ToPluralName(this DeviceClass deviceClass) => deviceClass switch
        {
            DeviceClass.Inverter => Resources.Inverters,
            DeviceClass.StringControl => Resources.StringControls,
            DeviceClass.Meter => Resources.Meters,
            DeviceClass.Ohmpilot => Resources.Ohmpilots,
            DeviceClass.SensorCard => Resources.SensorCards,
            DeviceClass.Storage => Resources.Storages,
            _ => Resources.Unknown
        };
    }
}
