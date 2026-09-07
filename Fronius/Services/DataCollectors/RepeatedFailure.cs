namespace De.Hochstaetter.Fronius.Services.DataCollectors;

/// <summary>What a failure that has just happened is worth in the log.</summary>
public enum FailureReport
{
    /// <summary>The first of its kind: worth the whole exception.</summary>
    First,

    /// <summary>The same thing again, and long enough since the last word about it to say so once more.</summary>
    StillFailing,

    /// <summary>The same thing again. Nothing new to say.</summary>
    Silent,
}

/// <summary>
/// Keeps something that fails over and over out of the log without hiding it.
/// </summary>
/// <remarks>
/// A collector that polls a device writes a line every time it fails, and a device that is simply switched off
/// fails on every round - so an inverter off overnight left a few hundred stack traces behind, all the same one.
/// This says the first one in full, then nothing until the reminder interval has passed, and tells the caller when
/// the device answers again and what it missed in the meantime.
///
/// "The same thing" is the type and the message of the exception. Something else going wrong is a new failure and
/// gets a line of its own, whatever has been failing so far.
/// </remarks>
public sealed class RepeatedFailure(TimeSpan reminderInterval, TimeProvider? clock = null)
{
    private readonly TimeProvider clock = clock ?? TimeProvider.System;

    private string? failure;
    private DateTimeOffset firstFailedAt;
    private DateTimeOffset lastReportedAt;

    /// <summary>How many times in a row it has failed, and 0 once it has answered again.</summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// Records a failure and says what to write about it.
    /// </summary>
    public FailureReport Record(Exception exception)
    {
        var now = clock.GetUtcNow();
        var signature = $"{exception.GetType().FullName}: {exception.Message}";
        Attempts++;

        if (signature != failure)
        {
            failure = signature;
            firstFailedAt = now;
            lastReportedAt = now;
            Attempts = 1;
            return FailureReport.First;
        }

        if (now - lastReportedAt < reminderInterval)
        {
            return FailureReport.Silent;
        }

        lastReportedAt = now;
        return FailureReport.StillFailing;
    }

    /// <summary>
    /// Records a success, and returns what the outage that has just ended amounted to - or
    /// <see langword="null"/> where there was nothing wrong in the first place.
    /// </summary>
    public (int Attempts, TimeSpan Duration)? Recover()
    {
        if (failure is null)
        {
            return null;
        }

        var outage = (Attempts, clock.GetUtcNow() - firstFailedAt);
        failure = null;
        Attempts = 0;
        return outage;
    }
}
