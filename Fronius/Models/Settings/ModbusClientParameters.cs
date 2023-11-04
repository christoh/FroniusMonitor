namespace De.Hochstaetter.Fronius.Models.Settings
{
    public record ModbusClientParameters(ICollection<ModbusConnection> ModbusConnections, TimeSpan RefreshRate);
}
