namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
///     What <see cref="Services.DataCollectors.SolarWebService" /> is configured with: the account and the system from
///     <c>Settings.xml</c>, and how the cache and the requests are paced.
/// </summary>
/// <remarks>
///     Solar.web answers <c>429 Too Many Requests</c> when it is asked too often, and it has been seen to answer
///     <c>500</c> and <c>503</c> for no reason of the caller's. Everything here is about asking as seldom as the data
///     allows: what is cached is served as it is, the running periods are read again every <see cref="RefreshRate" />
///     by the service itself, requests are at least <see cref="MinimumRequestInterval" /> apart, and a 429 stops
///     every request for the time the answer names, or <see cref="DefaultRateLimitBackoff" /> where it names none.
/// </remarks>
public class SolarWebParameters
{
    /// <summary>The account and the system; <see langword="null" /> or an unconfigured one means Solar.web is not read.</summary>
    public SolarWebSettings? Settings { get; set; }

    /// <summary>How often the running periods and the firmware status are read again. <see cref="SolarWebSettings.RefreshMinutes" />.</summary>
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How long the charts of single days are kept. The charts of months, years and the whole history are kept for good.</summary>
    public TimeSpan DayRetention { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    ///     How long after its end a period is still read again while its chart is not complete. Solar.web can be
    ///     hours behind the inverter, but whatever has not arrived this long after the period's end never will
    ///     (the developer's rule, 2026-09-20), so the chart is then left as it is.
    /// </summary>
    public TimeSpan FinalAfter { get; set; } = TimeSpan.FromDays(2);

    /// <summary>
    ///     How often a Premium view the account is locked out of is tried again. Solar.web answers such a view with
    ///     a placeholder, which does not change until the account changes, so once a day is often enough to notice.
    /// </summary>
    public TimeSpan LockedViewRetry { get; set; } = TimeSpan.FromDays(1);

    /// <summary>The least time between two requests to Solar.web.</summary>
    public TimeSpan MinimumRequestInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>How long a 429 without a <c>Retry-After</c> header stops the requests.</summary>
    public TimeSpan DefaultRateLimitBackoff { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>How long a 503 or the maintenance page without a <c>Retry-After</c> stops the requests. Maintenance lasts a while; a stray 503 does not.</summary>
    public TimeSpan UnavailableBackoff { get; set; } = TimeSpan.FromMinutes(5);
}
