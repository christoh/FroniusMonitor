namespace De.Hochstaetter.Fronius.Models.Charging;

/// <summary>
///     What became of a batch of settings sent to a Wattpilot: the writes that could not be sent at all, and the ones
///     the charger never acknowledged. Empty on both counts means everything was taken.
/// </summary>
public class WattPilotWriteResult
{
    /// <summary>Writes that failed on the way out, one line per property, with the exception's own words.</summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>Writes the charger did not confirm within the timeout, as "Property = 'value'".</summary>
    public List<string> Unconfirmed { get; set; } = [];

    public bool IsSuccess => Errors.Count == 0 && Unconfirmed.Count == 0;
}
