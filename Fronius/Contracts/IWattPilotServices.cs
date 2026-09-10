namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     Where the running <see cref="IWattPilotService" /> for a charger is found: the one that owns the connection to
///     it, by the device id its <see cref="WattPilot" /> is published under.
/// </summary>
public interface IWattPilotServices
{
    /// <summary>The service connected to the charger with that id, or <see langword="null" /> where none is.</summary>
    IWattPilotService? Find(string deviceId);
}
