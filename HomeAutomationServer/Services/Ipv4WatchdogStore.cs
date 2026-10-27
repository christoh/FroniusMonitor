using Microsoft.Data.Sqlite;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
///     The outage log of the IPv4 watchdog as one SQLite file, <c>history/Ipv4Watchdog.db</c> next to the executable,
///     beside the price and weather history. The folder is what a Docker host mounts to keep the files across
///     containers, and the log with them: an outage that lasts over a restart, and a cycle the server stopped in the
///     middle of, are found again at the next start.
/// </summary>
/// <remarks>
///     <para>
///         Two tables: <c>Outage</c>, one row per outage, and <c>Cycle</c>, one row per cycle. A cycle names the
///         outage it was run for, or none for a test cycle, but that is a plain column and not a foreign key - the
///         watchdog stores a cycle before it knows anything else about the world, and a log should never refuse a row.
///         The rows are written as they happen and rewritten in full when they change, so the row of an outage that is
///         still going on has <c>RecoveredUtc</c> and <c>Recovery</c> empty, and the row of a cycle that is still going on
///         has <c>Result</c> empty.
///     </para>
///     <para>
///         Times are Unix seconds UTC, which drops everything below a second; an enum is stored as its name, so that
///         a value added later cannot silently renumber the rows that are there; a flag is 0 or 1. The schema version
///         is SQLite's <c>user_version</c>, and changes go into <see cref="UpgradeAsync" /> as <c>if (version &lt; n)</c>
///         steps. The plumbing is <see cref="SqliteStoreBase" />'s.
///     </para>
/// </remarks>
public sealed class Ipv4WatchdogStore : SqliteStoreBase, IIpv4WatchdogStore
{
    public const string FileName = "Ipv4Watchdog.db";

    private const string OutageColumns = "Id, LastGoodUtc, DetectedUtc, RecoveredUtc, Recovery";
    private const string CycleColumns = "Id, OutageId, IsTest, StartedUtc, PreviousMode, SwitchedOffUtc, SwitchedOnUtc, FinishedUtc, Result, Detail";

    public Ipv4WatchdogStore(ILogger<Ipv4WatchdogStore> logger) : this(logger, DefaultPath(FileName))
    {
    }

    /// <summary>For a test that wants the file somewhere else.</summary>
    public Ipv4WatchdogStore(ILogger<Ipv4WatchdogStore> logger, string filePath) : base(logger, filePath, 1)
    {
    }

    protected override string Description => "The IPv4 watchdog's outage log";

    protected override async Task UpgradeAsync(SqliteConnection connection, int version, CancellationToken token)
    {
        if (version < 1)
        {
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS Outage (
                    Id INTEGER PRIMARY KEY,
                    LastGoodUtc INTEGER NULL,
                    DetectedUtc INTEGER NOT NULL,
                    RecoveredUtc INTEGER NULL,
                    Recovery TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS Cycle (
                    Id INTEGER PRIMARY KEY,
                    OutageId INTEGER NULL,
                    IsTest INTEGER NOT NULL,
                    StartedUtc INTEGER NOT NULL,
                    PreviousMode TEXT NOT NULL,
                    SwitchedOffUtc INTEGER NULL,
                    SwitchedOnUtc INTEGER NULL,
                    FinishedUtc INTEGER NULL,
                    Result TEXT NULL,
                    Detail TEXT NULL
                );

                PRAGMA user_version = 1;
                """, token).ConfigureAwait(false);
        }
    }

    #region Outages

    public async Task<long> AddOutageAsync(Ipv4Outage outage, CancellationToken token = default)
    {
        var id = 0L;

        await WriteInTransactionAsync(async (connection, transaction, t) =>
        {
            await using var insert = CreateCommand(connection, transaction, """
                INSERT INTO Outage (LastGoodUtc, DetectedUtc, RecoveredUtc, Recovery)
                VALUES ($lastGood, $detected, $recovered, $recovery)
                RETURNING Id
                """);
            BindOutage(insert, outage);
            id = Convert.ToInt64(await insert.ExecuteScalarAsync(t).ConfigureAwait(false), CultureInfo.InvariantCulture);
        }, token).ConfigureAwait(false);

        return id;
    }

    public Task UpdateOutageAsync(Ipv4Outage outage, CancellationToken token = default)
    {
        return WriteAsync("""
            UPDATE Outage SET LastGoodUtc = $lastGood, DetectedUtc = $detected, RecoveredUtc = $recovered, Recovery = $recovery
            WHERE Id = $id
            """, [outage], (command, row) =>
        {
            BindOutage(command, row);
            command.Parameters.AddWithValue("$id", row.Id);
        }, token);
    }

    public async Task<Ipv4Outage?> GetOpenOutageAsync(CancellationToken token = default)
    {
        var outages = await ReadAsync($"SELECT {OutageColumns} FROM Outage WHERE RecoveredUtc IS NULL ORDER BY Id DESC LIMIT 1", _ => { }, ReadOutage, token).ConfigureAwait(false);
        return outages.FirstOrDefault();
    }

    /// <summary>A negative <paramref name="count" /> is none: SQLite would read a negative <c>LIMIT</c> as no limit at all.</summary>
    public Task<IReadOnlyList<Ipv4Outage>> GetOutagesAsync(int count, CancellationToken token = default)
    {
        return ReadAsync($"SELECT {OutageColumns} FROM Outage ORDER BY Id DESC LIMIT $count", command =>
        {
            command.Parameters.AddWithValue("$count", Math.Max(count, 0));
        }, ReadOutage, token);
    }

    private static void BindOutage(SqliteCommand command, Ipv4Outage outage)
    {
        command.Parameters.AddWithValue("$lastGood", ToDb(outage.LastGoodUtc));
        command.Parameters.AddWithValue("$detected", ToUnix(outage.DetectedUtc));
        command.Parameters.AddWithValue("$recovered", ToDb(outage.RecoveredUtc));
        command.Parameters.AddWithValue("$recovery", (object?)outage.Recovery?.ToString() ?? DBNull.Value);
    }

    private static Ipv4Outage ReadOutage(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        LastGoodUtc = ReadTime(reader, 1),
        DetectedUtc = FromUnix(reader.GetInt64(2)),
        RecoveredUtc = ReadTime(reader, 3),
        Recovery = reader.IsDBNull(4) ? null : Enum.Parse<Ipv4Recovery>(reader.GetString(4)),
    };

    #endregion

    #region Cycles

    public async Task<long> AddCycleAsync(Ipv4WatchdogCycle cycle, CancellationToken token = default)
    {
        var id = 0L;

        await WriteInTransactionAsync(async (connection, transaction, t) =>
        {
            await using var insert = CreateCommand(connection, transaction, """
                INSERT INTO Cycle (OutageId, IsTest, StartedUtc, PreviousMode, SwitchedOffUtc, SwitchedOnUtc, FinishedUtc, Result, Detail)
                VALUES ($outage, $test, $started, $previous, $off, $on, $finished, $result, $detail)
                RETURNING Id
                """);
            BindCycle(insert, cycle);
            id = Convert.ToInt64(await insert.ExecuteScalarAsync(t).ConfigureAwait(false), CultureInfo.InvariantCulture);
        }, token).ConfigureAwait(false);

        return id;
    }

    public Task UpdateCycleAsync(Ipv4WatchdogCycle cycle, CancellationToken token = default)
    {
        return WriteAsync("""
            UPDATE Cycle SET OutageId = $outage, IsTest = $test, StartedUtc = $started, PreviousMode = $previous,
                SwitchedOffUtc = $off, SwitchedOnUtc = $on, FinishedUtc = $finished, Result = $result, Detail = $detail
            WHERE Id = $id
            """, [cycle], (command, row) =>
        {
            BindCycle(command, row);
            command.Parameters.AddWithValue("$id", row.Id);
        }, token);
    }

    public async Task<Ipv4WatchdogCycle?> GetUnfinishedCycleAsync(CancellationToken token = default)
    {
        var cycles = await ReadAsync($"SELECT {CycleColumns} FROM Cycle WHERE Result IS NULL ORDER BY Id DESC LIMIT 1", _ => { }, ReadCycle, token).ConfigureAwait(false);
        return cycles.FirstOrDefault();
    }

    public async Task<int> CountCyclesSinceAsync(DateTime sinceUtc, CancellationToken token = default)
    {
        var counts = await ReadAsync("SELECT COUNT(*) FROM Cycle WHERE StartedUtc >= $since", command =>
        {
            command.Parameters.AddWithValue("$since", ToUnix(sinceUtc));
        }, reader => reader.GetInt32(0), token).ConfigureAwait(false);

        return counts.Single();
    }

    /// <summary>A negative <paramref name="count" /> is none: SQLite would read a negative <c>LIMIT</c> as no limit at all.</summary>
    public Task<IReadOnlyList<Ipv4WatchdogCycle>> GetCyclesAsync(int count, CancellationToken token = default)
    {
        return ReadAsync($"SELECT {CycleColumns} FROM Cycle ORDER BY Id DESC LIMIT $count", command =>
        {
            command.Parameters.AddWithValue("$count", Math.Max(count, 0));
        }, ReadCycle, token);
    }

    private static void BindCycle(SqliteCommand command, Ipv4WatchdogCycle cycle)
    {
        command.Parameters.AddWithValue("$outage", (object?)cycle.OutageId ?? DBNull.Value);
        command.Parameters.AddWithValue("$test", cycle.IsTest ? 1 : 0);
        command.Parameters.AddWithValue("$started", ToUnix(cycle.StartedUtc));
        command.Parameters.AddWithValue("$previous", cycle.PreviousMode);
        command.Parameters.AddWithValue("$off", ToDb(cycle.SwitchedOffUtc));
        command.Parameters.AddWithValue("$on", ToDb(cycle.SwitchedOnUtc));
        command.Parameters.AddWithValue("$finished", ToDb(cycle.FinishedUtc));
        command.Parameters.AddWithValue("$result", (object?)cycle.Result?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$detail", (object?)cycle.Detail ?? DBNull.Value);
    }

    private static Ipv4WatchdogCycle ReadCycle(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        OutageId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
        IsTest = reader.GetInt64(2) != 0,
        StartedUtc = FromUnix(reader.GetInt64(3)),
        PreviousMode = reader.GetString(4),
        SwitchedOffUtc = ReadTime(reader, 5),
        SwitchedOnUtc = ReadTime(reader, 6),
        FinishedUtc = ReadTime(reader, 7),
        Result = reader.IsDBNull(8) ? null : Enum.Parse<Ipv4WatchdogCycleResult>(reader.GetString(8)),
        Detail = reader.IsDBNull(9) ? null : reader.GetString(9),
    };

    #endregion

    private static object ToDb(DateTime? utc) => utc is { } time ? ToUnix(time) : DBNull.Value;

    private static DateTime? ReadTime(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : FromUnix(reader.GetInt64(ordinal));
}
