using Microsoft.Data.Sqlite;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
///     Solar.web's charts as one SQLite file, <c>history/SolarWebHistory.db</c> next to the executable, beside the
///     price and weather history. The folder is what a Docker host mounts to keep the files across containers.
/// </summary>
/// <remarks>
///     <para>
///         Three tables: a chart row with the key and the captions, its series in order, and the points of each
///         series. The series and the points hang off the chart's rowid, so that replacing a chart is three deletes
///         and three inserts in one transaction. The times of the points are Solar.web's milliseconds since the
///         epoch as they came, because a day chart has a point every five minutes and the battery states carry
///         seconds.
///     </para>
///     <para>
///         The schema version is SQLite's <c>user_version</c>, and changes go into <see cref="UpgradeAsync" /> as
///         <c>if (version &lt; n)</c> steps.
///     </para>
/// </remarks>
public sealed class SolarWebHistoryStore : SqliteStoreBase, ISolarWebHistoryStore
{
    public const string FileName = "SolarWebHistory.db";

    public SolarWebHistoryStore(ILogger<SolarWebHistoryStore> logger) : this(logger, DefaultPath(FileName))
    {
    }

    /// <summary>For a test that wants the file somewhere else.</summary>
    public SolarWebHistoryStore(ILogger<SolarWebHistoryStore> logger, string filePath) : base(logger, filePath, 1)
    {
    }

    protected override string Description => "Solar.web history";

    protected override async Task UpgradeAsync(SqliteConnection connection, int version, CancellationToken token)
    {
        if (version < 1)
        {
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS SolarWebChart (
                    Id INTEGER PRIMARY KEY,
                    PvSystemId TEXT NOT NULL,
                    ChartInterval TEXT NOT NULL,
                    ChartView TEXT NOT NULL,
                    Period TEXT NOT NULL,
                    FetchedUtc INTEGER NOT NULL,
                    IsPremiumFeature INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    SumValue TEXT NOT NULL,
                    UNIQUE (PvSystemId, ChartInterval, ChartView, Period)
                );

                CREATE TABLE IF NOT EXISTS SolarWebSeries (
                    ChartId INTEGER NOT NULL,
                    Ordinal INTEGER NOT NULL,
                    SeriesId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Unit TEXT NOT NULL,
                    ChartType TEXT NOT NULL,
                    PRIMARY KEY (ChartId, Ordinal)
                ) WITHOUT ROWID;

                CREATE TABLE IF NOT EXISTS SolarWebPoint (
                    ChartId INTEGER NOT NULL,
                    Ordinal INTEGER NOT NULL,
                    TimeMs INTEGER NOT NULL,
                    Value REAL NULL,
                    Text TEXT NULL,
                    PRIMARY KEY (ChartId, Ordinal, TimeMs)
                ) WITHOUT ROWID;

                PRAGMA user_version = 1;
                """, token).ConfigureAwait(false);
        }
    }

    public async Task<SolarWebChart?> GetChartAsync(string pvSystemId, SolarWebInterval interval, SolarWebView view, DateOnly period, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token).ConfigureAwait(false);

        var charts = await ReadAsync(connection, "SELECT Id, FetchedUtc, IsPremiumFeature, Title, SumValue FROM SolarWebChart WHERE PvSystemId = $system AND ChartInterval = $interval AND ChartView = $view AND Period = $period", command =>
        {
            BindKey(command, pvSystemId, interval, view, period);
        }, reader => (Id: reader.GetInt64(0), Chart: new SolarWebChart
        {
            PvSystemId = pvSystemId,
            Interval = interval,
            View = view,
            Period = period,
            FetchedUtc = FromUnix(reader.GetInt64(1)),
            IsPremiumFeature = reader.GetInt64(2) != 0,
            Title = reader.GetString(3),
            SumValue = reader.GetString(4),
        }), token).ConfigureAwait(false);

        if (charts.Count == 0)
        {
            return null;
        }

        var (id, chart) = charts[0];

        var series = await ReadAsync(connection, "SELECT Ordinal, SeriesId, Name, Unit, ChartType FROM SolarWebSeries WHERE ChartId = $id ORDER BY Ordinal", command =>
        {
            command.Parameters.AddWithValue("$id", id);
        }, reader => (Ordinal: reader.GetInt32(0), Series: new SolarWebSeries
        {
            Id = reader.GetString(1),
            Name = reader.GetString(2),
            Unit = reader.GetString(3),
            ChartType = reader.GetString(4),
        }), token).ConfigureAwait(false);

        var byOrdinal = series.ToDictionary(s => s.Ordinal, s => s.Series);

        var points = await ReadAsync(connection, "SELECT Ordinal, TimeMs, Value, Text FROM SolarWebPoint WHERE ChartId = $id ORDER BY Ordinal, TimeMs", command =>
        {
            command.Parameters.AddWithValue("$id", id);
        }, reader => (Ordinal: reader.GetInt32(0), Point: new SolarWebPoint
        {
            TimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)).UtcDateTime,
            Value = reader.IsDBNull(2) ? null : reader.GetDouble(2),
            Text = reader.IsDBNull(3) ? null : reader.GetString(3),
        }), token).ConfigureAwait(false);

        foreach (var (ordinal, point) in points)
        {
            byOrdinal[ordinal].Points.Add(point);
        }

        chart.Series = [.. series.Select(s => s.Series)];
        return chart;
    }

    public Task UpsertChartAsync(SolarWebChart chart, CancellationToken token = default)
    {
        return WriteInTransactionAsync(async (connection, transaction, t) =>
        {
            await DeleteChartsAsync(connection, transaction, "PvSystemId = $system AND ChartInterval = $interval AND ChartView = $view AND Period = $period", command =>
            {
                BindKey(command, chart.PvSystemId, chart.Interval, chart.View, chart.Period);
            }, t).ConfigureAwait(false);

            long id;

            await using (var insert = CreateCommand(connection, transaction, """
                INSERT INTO SolarWebChart (PvSystemId, ChartInterval, ChartView, Period, FetchedUtc, IsPremiumFeature, Title, SumValue)
                VALUES ($system, $interval, $view, $period, $fetched, $premium, $title, $sum)
                RETURNING Id
                """))
            {
                BindKey(insert, chart.PvSystemId, chart.Interval, chart.View, chart.Period);
                insert.Parameters.AddWithValue("$fetched", ToUnix(chart.FetchedUtc));
                insert.Parameters.AddWithValue("$premium", chart.IsPremiumFeature ? 1 : 0);
                insert.Parameters.AddWithValue("$title", chart.Title);
                insert.Parameters.AddWithValue("$sum", chart.SumValue);
                id = Convert.ToInt64(await insert.ExecuteScalarAsync(t).ConfigureAwait(false), CultureInfo.InvariantCulture);
            }

            await using var seriesInsert = CreateCommand(connection, transaction, "INSERT INTO SolarWebSeries (ChartId, Ordinal, SeriesId, Name, Unit, ChartType) VALUES ($id, $ordinal, $series, $name, $unit, $type)");
            // INSERT OR REPLACE: Solar.web has been seen to send the same time twice in one series; the later one wins.
            await using var pointInsert = CreateCommand(connection, transaction, "INSERT OR REPLACE INTO SolarWebPoint (ChartId, Ordinal, TimeMs, Value, Text) VALUES ($id, $ordinal, $time, $value, $text)");

            foreach (var (series, ordinal) in chart.Series.Select((s, i) => (s, i)))
            {
                seriesInsert.Parameters.Clear();
                seriesInsert.Parameters.AddWithValue("$id", id);
                seriesInsert.Parameters.AddWithValue("$ordinal", ordinal);
                seriesInsert.Parameters.AddWithValue("$series", series.Id);
                seriesInsert.Parameters.AddWithValue("$name", series.Name);
                seriesInsert.Parameters.AddWithValue("$unit", series.Unit);
                seriesInsert.Parameters.AddWithValue("$type", series.ChartType);
                await seriesInsert.ExecuteNonQueryAsync(t).ConfigureAwait(false);

                foreach (var point in series.Points)
                {
                    pointInsert.Parameters.Clear();
                    pointInsert.Parameters.AddWithValue("$id", id);
                    pointInsert.Parameters.AddWithValue("$ordinal", ordinal);
                    pointInsert.Parameters.AddWithValue("$time", new DateTimeOffset(DateTime.SpecifyKind(point.TimeUtc, DateTimeKind.Utc)).ToUnixTimeMilliseconds());
                    pointInsert.Parameters.AddWithValue("$value", (object?)point.Value ?? DBNull.Value);
                    pointInsert.Parameters.AddWithValue("$text", (object?)point.Text ?? DBNull.Value);
                    await pointInsert.ExecuteNonQueryAsync(t).ConfigureAwait(false);
                }
            }
        }, token);
    }

    public async Task<int> DeleteDayChartsBeforeAsync(string pvSystemId, DateOnly cutoff, CancellationToken token = default)
    {
        var deleted = 0;

        await WriteInTransactionAsync(async (connection, transaction, t) =>
        {
            deleted = await DeleteChartsAsync(connection, transaction, "PvSystemId = $system AND ChartInterval = $interval AND Period < $cutoff", command =>
            {
                command.Parameters.AddWithValue("$system", pvSystemId);
                command.Parameters.AddWithValue("$interval", SolarWebInterval.Day.ToString());
                command.Parameters.AddWithValue("$cutoff", Day(cutoff));
            }, t).ConfigureAwait(false);
        }, token).ConfigureAwait(false);

        return deleted;
    }

    /// <summary>Removes the charts <paramref name="where" /> selects, with their series and points. Returns how many charts went.</summary>
    private static async Task<int> DeleteChartsAsync(SqliteConnection connection, SqliteTransaction transaction, string where, Action<SqliteCommand> bind, CancellationToken token)
    {
        var selected = $"SELECT Id FROM SolarWebChart WHERE {where}";

        foreach (var sql in new[]
                 {
                     $"DELETE FROM SolarWebPoint WHERE ChartId IN ({selected})",
                     $"DELETE FROM SolarWebSeries WHERE ChartId IN ({selected})",
                 })
        {
            await using var command = CreateCommand(connection, transaction, sql);
            bind(command);
            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        await using var delete = CreateCommand(connection, transaction, $"DELETE FROM SolarWebChart WHERE {where}");
        bind(delete);
        return await delete.ExecuteNonQueryAsync(token).ConfigureAwait(false);
    }

    private static void BindKey(SqliteCommand command, string pvSystemId, SolarWebInterval interval, SolarWebView view, DateOnly period)
    {
        command.Parameters.AddWithValue("$system", pvSystemId);
        command.Parameters.AddWithValue("$interval", interval.ToString());
        command.Parameters.AddWithValue("$view", view.ToString());
        command.Parameters.AddWithValue("$period", Day(period));
    }
}
