namespace De.Hochstaetter.HomeAutomationServer.Contracts;

/// <summary>
///     Talks to Fronius Solar.web the way a browser does: it holds the cookies of one account, logs in at
///     <c>login.fronius.com</c> when Solar.web asks it to, and reads the charts of the chart page.
/// </summary>
/// <remarks>
///     Nothing here caches or paces; that is <see cref="ISolarWebService" />'s job. A <c>429</c> is thrown as a
///     <see cref="SolarWebRateLimitException" />, a login that does not end in a session as a
///     <see cref="SolarWebLoginException" />, and any other refusal as an <see cref="HttpRequestException" />.
/// </remarks>
public interface ISolarWebClient
{
    /// <summary>
    ///     The chart the chart page shows for <paramref name="view" /> and <paramref name="interval" /> at
    ///     <paramref name="date" />: the day itself, or the month, the year or the whole history that contains it.
    /// </summary>
    Task<SolarWebChart> GetChartAsync(SolarWebSettings settings, SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default);
}
