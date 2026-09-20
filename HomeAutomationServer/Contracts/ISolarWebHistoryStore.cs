namespace De.Hochstaetter.HomeAutomationServer.Contracts;

/// <summary>
///     The cache of Solar.web's charts, kept by the server so that a period that is over is never asked of Solar.web
///     again. The server's implementation is a SQLite file; the contract lives here so that the service and the
///     tests do not depend on it.
/// </summary>
/// <remarks>
///     A chart is one row per (system, interval, view, period) and is replaced as a whole when written again -
///     Solar.web's answer for a running period changes with every request, and a series that vanished would
///     otherwise stay.
/// </remarks>
public interface ISolarWebHistoryStore
{
    /// <summary>Creates the file and the tables where they do not exist. Called once by the service before anything else.</summary>
    Task InitializeAsync(CancellationToken token = default);

    /// <summary>The chart as last written, or <see langword="null" /> where none was. <paramref name="period" /> is a value <see cref="SolarWebPeriod.Normalize" /> returned.</summary>
    Task<SolarWebChart?> GetChartAsync(string pvSystemId, SolarWebInterval interval, SolarWebView view, DateOnly period, CancellationToken token = default);

    /// <summary>Writes the chart, replacing the one of the same system, interval, view and period.</summary>
    Task UpsertChartAsync(SolarWebChart chart, CancellationToken token = default);

    /// <summary>
    ///     Removes the <see cref="SolarWebInterval.Day" /> charts of every day before <paramref name="cutoff" />, of
    ///     every view. Months, years and the whole history are kept. Returns how many charts went.
    /// </summary>
    Task<int> DeleteDayChartsBeforeAsync(string pvSystemId, DateOnly cutoff, CancellationToken token = default);
}
