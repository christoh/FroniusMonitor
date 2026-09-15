namespace De.Hochstaetter.FroniusMonitor.Models;

public enum EnergyDirection : sbyte
{
    None = 0,
    Production = 1,
    Consumption = 2,
    Unknown = -1,
}