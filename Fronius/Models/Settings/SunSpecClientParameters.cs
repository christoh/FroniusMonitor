namespace De.Hochstaetter.Fronius.Models.Settings;

public record SunSpecClientParameters(ICollection<ModbusConnection> ModbusConnections, TimeSpan RefreshRate) : ModbusClientParameters(ModbusConnections, RefreshRate);
