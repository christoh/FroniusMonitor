namespace De.Hochstaetter.Fronius.Models.Charging;

public record WattPilotAcknowledge(uint RequestId, PropertyInfo PropertyInfo, object? Value)
{
    public ManualResetEventSlim Event { get; } = new();

    /// <summary>
    /// <c>true</c> once the WattPilot acknowledged this write with success. Writes that are never
    /// confirmed (explicit failure or timeout) remain <c>false</c> and are reported as unsuccessful.
    /// </summary>
    public bool IsConfirmed { get; set; }

    /// <summary>
    /// Guards <see cref="Event"/> against use-after-dispose. Only ever mutated while holding the
    /// lock on the owning acknowledge list.
    /// </summary>
    public bool IsDisposed { get; set; }

    public override string ToString() => $"{PropertyInfo.Name} = '{Value}'";
}