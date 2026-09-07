namespace De.Hochstaetter.Fronius.Services.DataCollectors;

/// <summary>
/// The wait between two reads of a device, which ends either when the interval has passed or when somebody asks
/// for the next read at once.
/// </summary>
/// <remarks>
/// A collector that polls on a timer cannot be told anything: whatever is written to a device while it waits is
/// only seen at the end of the interval, which for the configuration of an inverter is minutes. Waiting on this
/// instead of on <see cref="Task.Delay(TimeSpan)"/> lets whoever did the writing say so.
/// </remarks>
public sealed class ReadNowRequest : IDisposable
{
    private readonly SemaphoreSlim signal = new(0, 1);

    /// <summary>
    /// Waits for the interval to pass or for <see cref="Request"/>, whichever comes first.
    /// </summary>
    /// <returns><see langword="true"/> if a read was asked for, <see langword="false"/> if the interval passed.</returns>
    public Task<bool> WaitAsync(TimeSpan interval, CancellationToken token = default) => signal.WaitAsync(interval, token);

    /// <summary>
    /// Asks for the next read to happen now. Asking twice before it happens is the same as asking once - the read
    /// that follows sees everything written before it either way.
    /// </summary>
    public void Request()
    {
        try
        {
            signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already asked for. One read answers both requests.
        }
    }

    public void Dispose() => signal.Dispose();
}
