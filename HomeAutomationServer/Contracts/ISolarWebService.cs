namespace De.Hochstaetter.HomeAutomationServer.Contracts;

/// <summary>
///     What a controller asks for: one Solar.web chart, from the cache where the cache is good enough and from
///     Solar.web otherwise. The pacing towards Solar.web - the refresh interval, the request spacing, the 429
///     back-off - is the service's and is not visible here beyond the exceptions.
/// </summary>
public interface ISolarWebService
{
    /// <summary>False where the settings have no usable <c>SolarWeb</c> element, so a client can be told there is nothing to show.</summary>
    bool IsEnabled { get; }

    /// <summary>Today in the PV system's time zone.</summary>
    DateOnly Today { get; }

    /// <summary>Until when Solar.web has asked to be left alone after a 429, or <see langword="null" /> where it has not.</summary>
    DateTimeOffset? RateLimitedUntil { get; }

    /// <summary>
    ///     The chart of <paramref name="view" /> over the <paramref name="interval" /> that contains
    ///     <paramref name="date" />. A period that is over comes from the cache once it is there; a running period
    ///     is read again when the cached chart is older than the refresh interval.
    /// </summary>
    /// <exception cref="ArgumentException">A Premium view was asked for a day, which Solar.web does not have.</exception>
    /// <exception cref="SolarWebRateLimitException">Solar.web asked to be left alone and there is no cached chart to fall back on.</exception>
    /// <exception cref="SolarWebLoginException">The login failed and there is no cached chart to fall back on.</exception>
    /// <exception cref="HttpRequestException">Solar.web refused and there is no cached chart to fall back on.</exception>
    Task<SolarWebChart> GetChartAsync(SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default);
}
