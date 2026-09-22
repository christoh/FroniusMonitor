using Microsoft.Data.Sqlite;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
///     What the server's SQLite history files have in common: one file in the <c>history</c> folder next to the
///     executable, a schema version in SQLite's <c>user_version</c> that <see cref="InitializeAsync" /> brings up to
///     date step by step, one writer at a time, and the plumbing that opens, reads and writes.
/// </summary>
/// <remarks>
///     <para>
///         Times are Unix seconds UTC unless a store says otherwise, a day is <c>yyyy-MM-dd</c>, and a decimal is
///         stored as text so that a price of 12.345 comes back as exactly that. <see cref="EnergyHistoryStore" /> and
///         <see cref="SolarWebHistoryStore" /> are the two files; each has its own schema and its own version.
///     </para>
///     <para>
///         The <c>history</c> folder is what a Docker host mounts to keep the files across containers; the
///         Dockerfile creates it and <c>docker-compose.yml</c> shows the mount.
///     </para>
/// </remarks>
public abstract class SqliteStoreBase
{
    private readonly string connectionString;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    protected SqliteStoreBase(ILogger logger, string filePath, int currentSchemaVersion)
    {
        Logger = logger;
        FilePath = filePath;
        CurrentSchemaVersion = currentSchemaVersion;
        connectionString = new SqliteConnectionStringBuilder { DataSource = filePath, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true }.ToString();
    }

    public string FilePath { get; }

    protected ILogger Logger { get; }

    protected int CurrentSchemaVersion { get; }

    /// <summary>What the file holds, for the log line at start.</summary>
    protected abstract string Description { get; }

    /// <summary>The default place of a history file: <c>history/&lt;fileName&gt;</c> next to the executable.</summary>
    protected static string DefaultPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "history", fileName);

    /// <summary>Creates the folder, the file and the tables where they do not exist, and upgrades an older schema.</summary>
    public async Task InitializeAsync(CancellationToken token = default)
    {
        if (Path.GetDirectoryName(FilePath) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        var version = Convert.ToInt32(await ScalarAsync(connection, "PRAGMA user_version", token).ConfigureAwait(false), CultureInfo.InvariantCulture);
        await UpgradeAsync(connection, version, token).ConfigureAwait(false);

        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("{Description} is {File} (schema {Version})", Description, FilePath, CurrentSchemaVersion);
        }
    }

    /// <summary>
    ///     Brings a file of schema <paramref name="version" /> up to <see cref="CurrentSchemaVersion" />: one
    ///     <c>if (version &lt; n)</c> step per version, each ending in <c>PRAGMA user_version = n</c>. A new file
    ///     has version 0 and runs through every step.
    /// </summary>
    protected abstract Task UpgradeAsync(SqliteConnection connection, int version, CancellationToken token);

    protected async Task<SqliteConnection> OpenAsync(CancellationToken token)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);
        return connection;
    }

    /// <summary>One statement per row inside one transaction; the command is prepared once and its parameters rebound.</summary>
    protected async Task WriteAsync<T>(string sql, IEnumerable<T> rows, Action<SqliteCommand, T> bind, CancellationToken token)
    {
        var list = rows as IReadOnlyCollection<T> ?? rows.ToList();

        if (list.Count == 0)
        {
            return;
        }

        await WriteInTransactionAsync(async (connection, transaction, t) =>
        {
            await using var command = CreateCommand(connection, transaction, sql);

            foreach (var row in list)
            {
                command.Parameters.Clear();
                bind(command, row);
                await command.ExecuteNonQueryAsync(t).ConfigureAwait(false);
            }
        }, token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Runs <paramref name="work" /> as the only writer, inside one transaction that is committed when the work
    ///     returns. For the writes that are more than one statement per row - a delete and a rewrite, say.
    /// </summary>
    protected async Task WriteInTransactionAsync(Func<SqliteConnection, SqliteTransaction, CancellationToken, Task> work, CancellationToken token)
    {
        await writeLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            await using var connection = await OpenAsync(token).ConfigureAwait(false);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token).ConfigureAwait(false);
            await work(connection, transaction, token).ConfigureAwait(false);
            await transaction.CommitAsync(token).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    protected async Task<IReadOnlyList<T>> ReadAsync<T>(string sql, Action<SqliteCommand> bind, Func<SqliteDataReader, T> map, CancellationToken token)
    {
        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        return await ReadAsync(connection, sql, bind, map, token).ConfigureAwait(false);
    }

    protected static Task<IReadOnlyList<T>> ReadAsync<T>(SqliteConnection connection, string sql, Action<SqliteCommand> bind, Func<SqliteDataReader, T> map, CancellationToken token) =>
        ReadAsync(connection, transaction: null, sql, bind, map, token);

    /// <summary>
    ///     Reads one statement on a connection, optionally as part of a transaction that gives several statements the
    ///     same snapshot. A chart is three queries - its row, series and points - and must not see a replacement
    ///     between them.
    /// </summary>
    protected static async Task<IReadOnlyList<T>> ReadAsync<T>(SqliteConnection connection, SqliteTransaction? transaction, string sql, Action<SqliteCommand> bind, Func<SqliteDataReader, T> map, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        bind(command);
        var result = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

        while (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            result.Add(map(reader));
        }

        return result;
    }

    protected static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    protected static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
    }

    protected static async Task<object?> ScalarAsync(SqliteConnection connection, string sql, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(token).ConfigureAwait(false);
    }

    protected static long ToUnix(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();

    protected static DateTime FromUnix(long seconds) => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

    protected static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    protected static DateOnly ParseDay(string day) => DateOnly.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
