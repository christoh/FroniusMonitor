using De.Hochstaetter.Fronius.Models.Charging;
using Microsoft.Data.Sqlite;

namespace De.Hochstaetter.HomeAutomationServer.Services;

/// <summary>
///     The history of prices, tariffs, productions and weather as one SQLite file,
///     <c>history/PriceAndWeatherHistory.db</c> next to the executable. The folder is what a Docker host mounts to
///     keep the history across containers.
/// </summary>
/// <remarks>
///     <para>
///         Every table carries the source of a row as part of its key - the price source, the price region, the
///         weather service and station - so a second provider of any of these later goes into the same tables
///         without a migration. Times are Unix seconds UTC; a decimal is stored as text so that a price of 12.345
///         comes back as exactly that.
///     </para>
///     <para>
///         The schema version is SQLite's <c>user_version</c>. <see cref="InitializeAsync" /> brings an older file up
///         to date step by step; a new file gets the current schema at once.
///     </para>
/// </remarks>
public sealed class EnergyHistoryStore : IEnergyHistoryStore
{
    public const string FileName = "PriceAndWeatherHistory.db";

    private const int CurrentSchemaVersion = 1;

    private readonly ILogger<EnergyHistoryStore> logger;
    private readonly string filePath;
    private readonly string connectionString;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public EnergyHistoryStore(ILogger<EnergyHistoryStore> logger) : this(logger, Path.Combine(AppContext.BaseDirectory, "history", FileName))
    {
    }

    /// <summary>For a test that wants the file somewhere else.</summary>
    public EnergyHistoryStore(ILogger<EnergyHistoryStore> logger, string filePath)
    {
        this.logger = logger;
        this.filePath = filePath;
        connectionString = new SqliteConnectionStringBuilder { DataSource = filePath, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true }.ToString();
    }

    public string FilePath => filePath;

    public async Task InitializeAsync(CancellationToken token = default)
    {
        if (Path.GetDirectoryName(filePath) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        var version = Convert.ToInt32(await Scalar(connection, "PRAGMA user_version", token).ConfigureAwait(false), CultureInfo.InvariantCulture);

        if (version < 1)
        {
            await Execute(connection, """
                CREATE TABLE IF NOT EXISTS MarketPrice (
                    Source TEXT NOT NULL,
                    StartUtc INTEGER NOT NULL,
                    EndUtc INTEGER NOT NULL,
                    CentsPerKwh TEXT NOT NULL,
                    PRIMARY KEY (Source, StartUtc)
                ) WITHOUT ROWID;

                CREATE TABLE IF NOT EXISTS PriceComponent (
                    Day TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Ordinal INTEGER NOT NULL,
                    Description TEXT NOT NULL,
                    NetPrice TEXT NOT NULL,
                    TaxRate TEXT NOT NULL,
                    Unit TEXT NOT NULL,
                    PRIMARY KEY (Day, Name)
                ) WITHOUT ROWID;

                CREATE TABLE IF NOT EXISTS GridProduction (
                    Region TEXT NOT NULL,
                    StartUtc INTEGER NOT NULL,
                    EndUtc INTEGER NOT NULL,
                    SolarMegaWatt REAL NOT NULL,
                    WindMegaWatt REAL NOT NULL,
                    PRIMARY KEY (Region, StartUtc)
                ) WITHOUT ROWID;

                CREATE TABLE IF NOT EXISTS Weather (
                    Source TEXT NOT NULL,
                    StationId TEXT NOT NULL,
                    TimeUtc INTEGER NOT NULL,
                    IsForecast INTEGER NOT NULL,
                    GlobalRadiation REAL NULL,
                    WindSpeed REAL NULL,
                    Temperature REAL NULL,
                    CloudCover REAL NULL,
                    PRIMARY KEY (Source, StationId, TimeUtc, IsForecast)
                ) WITHOUT ROWID;

                PRAGMA user_version = 1;
                """, token).ConfigureAwait(false);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Energy history is {File} (schema {Version})", filePath, CurrentSchemaVersion);
        }
    }

    #region Prices

    public Task UpsertPricesAsync(IEnumerable<EnergyPricePoint> prices, CancellationToken token = default)
    {
        return WriteAsync("""
            INSERT INTO MarketPrice (Source, StartUtc, EndUtc, CentsPerKwh) VALUES ($source, $start, $end, $cents)
            ON CONFLICT (Source, StartUtc) DO UPDATE SET EndUtc = excluded.EndUtc, CentsPerKwh = excluded.CentsPerKwh
            """, prices, (command, price) =>
        {
            command.Parameters.AddWithValue("$source", price.Source.ToString());
            command.Parameters.AddWithValue("$start", ToUnix(price.StartTime));
            command.Parameters.AddWithValue("$end", ToUnix(price.EndTime));
            command.Parameters.AddWithValue("$cents", price.CentsPerKiloWattHour.ToString(CultureInfo.InvariantCulture));
        }, token);
    }

    public Task<IReadOnlyList<EnergyPricePoint>> GetPricesAsync(EnergyPriceSource source, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        return ReadAsync("SELECT StartUtc, EndUtc, CentsPerKwh FROM MarketPrice WHERE Source = $source AND StartUtc >= $from AND StartUtc < $to ORDER BY StartUtc", command =>
        {
            command.Parameters.AddWithValue("$source", source.ToString());
            command.Parameters.AddWithValue("$from", ToUnix(fromUtc));
            command.Parameters.AddWithValue("$to", ToUnix(toUtc));
        }, reader => new EnergyPricePoint
        {
            StartTime = FromUnix(reader.GetInt64(0)),
            EndTime = FromUnix(reader.GetInt64(1)),
            CentsPerKiloWattHour = decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
            Source = source,
        }, token);
    }

    #endregion

    #region Components

    public async Task UpsertPriceComponentsAsync(DateOnly day, IEnumerable<EnergyPriceComponent> components, CancellationToken token = default)
    {
        var list = components.ToList();

        if (list.Count == 0)
        {
            return;
        }

        // The tariff of a day is replaced as a whole: a component that vanished from the answer would otherwise
        // stay in the sum for good.
        await writeLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            await using var connection = await OpenAsync(token).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);

            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = (SqliteTransaction)transaction;
                delete.CommandText = "DELETE FROM PriceComponent WHERE Day = $day";
                delete.Parameters.AddWithValue("$day", Day(day));
                await delete.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            await using (var insert = connection.CreateCommand())
            {
                insert.Transaction = (SqliteTransaction)transaction;
                // Ordinal keeps the order Awattar answers in, which is the order the invoice lists the components
                // in; a table without a rowid has no order of its own.
                insert.CommandText = "INSERT OR REPLACE INTO PriceComponent (Day, Name, Ordinal, Description, NetPrice, TaxRate, Unit) VALUES ($day, $name, $ordinal, $description, $net, $tax, $unit)";

                foreach (var (component, ordinal) in list.Select((c, i) => (c, i)))
                {
                    insert.Parameters.Clear();
                    insert.Parameters.AddWithValue("$day", Day(day));
                    insert.Parameters.AddWithValue("$name", component.Name);
                    insert.Parameters.AddWithValue("$ordinal", ordinal);
                    insert.Parameters.AddWithValue("$description", component.Description);
                    insert.Parameters.AddWithValue("$net", component.NetPrice.ToString(CultureInfo.InvariantCulture));
                    insert.Parameters.AddWithValue("$tax", component.TaxRate.ToString(CultureInfo.InvariantCulture));
                    insert.Parameters.AddWithValue("$unit", component.Unit);
                    await insert.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }
            }

            await transaction.CommitAsync(token).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public Task<IReadOnlyList<EnergyPriceComponent>> GetPriceComponentsAsync(DateOnly day, CancellationToken token = default)
    {
        return ReadAsync("SELECT Name, Description, NetPrice, TaxRate, Unit FROM PriceComponent WHERE Day = $day ORDER BY Ordinal", command =>
        {
            command.Parameters.AddWithValue("$day", Day(day));
        }, reader => new EnergyPriceComponent
        {
            Name = reader.GetString(0),
            Description = reader.GetString(1),
            NetPrice = decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
            TaxRate = decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
            Unit = reader.GetString(4),
        }, token);
    }

    #endregion

    #region Productions

    public Task UpsertProductionsAsync(AwattarCountry region, IEnumerable<GridProductionPoint> productions, CancellationToken token = default)
    {
        return WriteAsync("""
            INSERT INTO GridProduction (Region, StartUtc, EndUtc, SolarMegaWatt, WindMegaWatt) VALUES ($region, $start, $end, $solar, $wind)
            ON CONFLICT (Region, StartUtc) DO UPDATE SET EndUtc = excluded.EndUtc, SolarMegaWatt = excluded.SolarMegaWatt, WindMegaWatt = excluded.WindMegaWatt
            """, productions, (command, production) =>
        {
            command.Parameters.AddWithValue("$region", region.ToString());
            command.Parameters.AddWithValue("$start", ToUnix(production.StartTime));
            command.Parameters.AddWithValue("$end", ToUnix(production.EndTime));
            command.Parameters.AddWithValue("$solar", production.SolarMegaWatt);
            command.Parameters.AddWithValue("$wind", production.WindMegaWatt);
        }, token);
    }

    public Task<IReadOnlyList<GridProductionPoint>> GetProductionsAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        return ReadAsync("SELECT StartUtc, EndUtc, SolarMegaWatt, WindMegaWatt FROM GridProduction WHERE Region = $region AND StartUtc >= $from AND StartUtc < $to ORDER BY StartUtc", command =>
        {
            command.Parameters.AddWithValue("$region", region.ToString());
            command.Parameters.AddWithValue("$from", ToUnix(fromUtc));
            command.Parameters.AddWithValue("$to", ToUnix(toUtc));
        }, reader => new GridProductionPoint
        {
            StartTime = FromUnix(reader.GetInt64(0)),
            EndTime = FromUnix(reader.GetInt64(1)),
            SolarMegaWatt = reader.GetDouble(2),
            WindMegaWatt = reader.GetDouble(3),
        }, token);
    }

    #endregion

    #region Weather

    private const string WeatherSource = "DWD";

    public Task UpsertWeatherAsync(string stationId, IEnumerable<WeatherPoint> points, CancellationToken token = default)
    {
        return WriteAsync("""
            INSERT INTO Weather (Source, StationId, TimeUtc, IsForecast, GlobalRadiation, WindSpeed, Temperature, CloudCover)
            VALUES ($source, $station, $time, $forecast, $radiation, $wind, $temperature, $cloud)
            ON CONFLICT (Source, StationId, TimeUtc, IsForecast) DO UPDATE SET
                GlobalRadiation = COALESCE(excluded.GlobalRadiation, GlobalRadiation),
                WindSpeed = COALESCE(excluded.WindSpeed, WindSpeed),
                Temperature = COALESCE(excluded.Temperature, Temperature),
                CloudCover = COALESCE(excluded.CloudCover, CloudCover)
            """, points, (command, point) =>
        {
            command.Parameters.AddWithValue("$source", WeatherSource);
            command.Parameters.AddWithValue("$station", stationId);
            command.Parameters.AddWithValue("$time", ToUnix(point.Time));
            command.Parameters.AddWithValue("$forecast", point.IsForecast ? 1 : 0);
            command.Parameters.AddWithValue("$radiation", (object?)point.GlobalRadiationWattsPerSquareMeter ?? DBNull.Value);
            command.Parameters.AddWithValue("$wind", (object?)point.WindSpeedMetersPerSecond ?? DBNull.Value);
            command.Parameters.AddWithValue("$temperature", (object?)point.TemperatureCelsius ?? DBNull.Value);
            command.Parameters.AddWithValue("$cloud", (object?)point.CloudCoverPercent ?? DBNull.Value);
        }, token);
    }

    public Task<IReadOnlyList<WeatherPoint>> GetWeatherAsync(string stationId, DateTime fromUtc, DateTime toUtc, CancellationToken token = default)
    {
        return ReadAsync("SELECT TimeUtc, IsForecast, GlobalRadiation, WindSpeed, Temperature, CloudCover FROM Weather WHERE Source = $source AND StationId = $station AND TimeUtc >= $from AND TimeUtc < $to ORDER BY TimeUtc, IsForecast", command =>
        {
            command.Parameters.AddWithValue("$source", WeatherSource);
            command.Parameters.AddWithValue("$station", stationId);
            command.Parameters.AddWithValue("$from", ToUnix(fromUtc));
            command.Parameters.AddWithValue("$to", ToUnix(toUtc));
        }, reader => new WeatherPoint
        {
            Time = FromUnix(reader.GetInt64(0)),
            IsForecast = reader.GetInt64(1) != 0,
            GlobalRadiationWattsPerSquareMeter = reader.IsDBNull(2) ? null : reader.GetDouble(2),
            WindSpeedMetersPerSecond = reader.IsDBNull(3) ? null : reader.GetDouble(3),
            TemperatureCelsius = reader.IsDBNull(4) ? null : reader.GetDouble(4),
            CloudCoverPercent = reader.IsDBNull(5) ? null : reader.GetDouble(5),
        }, token);
    }

    #endregion

    #region Plumbing

    private async Task<SqliteConnection> OpenAsync(CancellationToken token)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);
        return connection;
    }

    /// <summary>One statement per row inside one transaction; the command is prepared once and its parameters rebound.</summary>
    private async Task WriteAsync<T>(string sql, IEnumerable<T> rows, Action<SqliteCommand, T> bind, CancellationToken token)
    {
        var list = rows as IReadOnlyCollection<T> ?? rows.ToList();

        if (list.Count == 0)
        {
            return;
        }

        await writeLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            await using var connection = await OpenAsync(token).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = sql;

            foreach (var row in list)
            {
                command.Parameters.Clear();
                bind(command, row);
                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }

            await transaction.CommitAsync(token).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private async Task<IReadOnlyList<T>> ReadAsync<T>(string sql, Action<SqliteCommand> bind, Func<SqliteDataReader, T> map, CancellationToken token)
    {
        await using var connection = await OpenAsync(token).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
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

    private static async Task Execute(SqliteConnection connection, string sql, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
    }

    private static async Task<object?> Scalar(SqliteConnection connection, string sql, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(token).ConfigureAwait(false);
    }

    private static long ToUnix(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();

    private static DateTime FromUnix(long seconds) => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

    private static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    #endregion
}
