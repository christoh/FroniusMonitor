namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
/// Implemented by whatever polls the configuration of the inverters, so that code which has just changed a setting
/// can have it read back at once instead of waiting out the interval.
/// </summary>
public interface IGen24ConfigRefresher
{
    /// <summary>
    /// Reads the configuration of one inverter now rather than at the end of the current interval, and does
    /// nothing at all if that inverter is not being polled.
    /// </summary>
    /// <param name="deviceId">The <see cref="IHaveUniqueId.Id"/> of the inverter.</param>
    void ReadConfigNow(string deviceId);
}
