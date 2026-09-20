namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
///     What <see cref="Services.SolarWebService" /> is configured with: the account and the system from
///     <c>Settings.xml</c>, and how the cache and the requests are paced.
/// </summary>
/// <remarks>
///     Solar.web answers <c>429 Too Many Requests</c> when it is asked too often, and it has been seen to answer
///     <c>500</c> and <c>503</c> for no reason of the caller's. Everything here is about asking as seldom as the data
///     allows: a period that is over is read once and kept, a running period is read again only after
///     <see cref="RefreshRate" />, requests are at least <see cref="MinimumRequestInterval" /> apart, and a 429 stops
///     every request for the time the answer names, or <see cref="DefaultRateLimitBackoff" /> where it names none.
/// </remarks>
public class SolarWebParameters
{
    /// <summary>The account and the system; <see langword="null" /> or an unconfigured one means Solar.web is not read.</summary>
    public SolarWebSettings? Settings { get; set; }

    /// <summary>How old the chart of a running period may be before it is read again. <see cref="SolarWebSettings.RefreshMinutes" />.</summary>
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How long the charts of single days are kept. The charts of months, years and the whole history are kept for good.</summary>
    public TimeSpan DayRetention { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    ///     How long after its end a period counts as final. An inverter uploads its log with some delay, and
    ///     Solar.web may still change yesterday's numbers in the morning; a day later they are what they are.
    /// </summary>
    public TimeSpan FinalAfter { get; set; } = TimeSpan.FromDays(1);

    /// <summary>The least time between two requests to Solar.web.</summary>
    public TimeSpan MinimumRequestInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>How long a 429 without a <c>Retry-After</c> header stops the requests.</summary>
    public TimeSpan DefaultRateLimitBackoff { get; set; } = TimeSpan.FromMinutes(15);
}
