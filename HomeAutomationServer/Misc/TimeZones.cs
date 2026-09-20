namespace De.Hochstaetter.HomeAutomationServer.Misc;

/// <summary>
///     The one place a time zone id from <c>Settings.xml</c> is turned into a <see cref="TimeZoneInfo" />. Every
///     section that names one - the price chart's polling windows, SolarWeb's day boundaries - resolves it here.
/// </summary>
internal static class TimeZones
{
    /// <summary>
    ///     The time zone <paramref name="timeZoneId" /> names, as an IANA or Windows id, or the local one where it is
    ///     empty or unknown. Never throws: a misspelt zone must not stop the server, and the log says what happened.
    /// </summary>
    public static TimeZoneInfo Resolve(string timeZoneId, ILogger? logger = null)
    {
        if (timeZoneId.Length == 0)
        {
            return TimeZoneInfo.Local;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone))
        {
            return zone;
        }

        if (logger?.IsEnabled(LogLevel.Warning) == true)
        {
            logger.LogWarning("The time zone '{TimeZoneId}' is unknown, using {Local} instead", timeZoneId, TimeZoneInfo.Local.Id);
        }

        return TimeZoneInfo.Local;
    }
}
