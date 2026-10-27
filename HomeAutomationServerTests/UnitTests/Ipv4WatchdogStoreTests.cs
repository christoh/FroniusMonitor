using System.Globalization;
using De.Hochstaetter.HomeAutomationServer.Models.Ipv4Watchdog;
using De.Hochstaetter.HomeAutomationServer.Services;
using Microsoft.Data.Sqlite;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The IPv4 watchdog's outage log against a SQLite file of its own: a new file has schema 1 and nothing in it, every
/// field of an outage and of a cycle comes back as it went in and as it was updated, the open outage and the
/// unfinished cycle are the newest ones without an end, the cycles are counted from a given time on, the lists
/// come newest first, and all of it survives a restart.
/// </summary>
public sealed class Ipv4WatchdogStoreTests : IAsyncLifetime
{
    // Whole seconds on purpose: the file keeps Unix seconds, so a round trip is exact only for those.
    private static readonly DateTime Start = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private readonly string directory = Path.Combine(Path.GetTempPath(), "HomeAutomationServerTests", Guid.NewGuid().ToString("N"));
    private readonly ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddTestOutput());
    private Ipv4WatchdogStore store = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        store = CreateStore();
        await store.InitializeAsync(Token);
    }

    public ValueTask DisposeAsync()
    {
        // The pool holds the file open for a moment; the temp folder is not worth a retry loop.
        SqliteConnection.ClearAllPools();
        loggers.Dispose();

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task A_new_file_has_schema_1_both_tables_and_nothing_in_them()
    {
        Assert.True(File.Exists(store.FilePath));
        Assert.Equal(1, await ScalarAsync("PRAGMA user_version"));
        Assert.Equal(2, await ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('Outage', 'Cycle')"));

        Assert.Null(await store.GetOpenOutageAsync(Token));
        Assert.Null(await store.GetUnfinishedCycleAsync(Token));
        Assert.Equal(0, await store.CountCyclesSinceAsync(Start.AddYears(-100), Token));
        Assert.Empty(await store.GetOutagesAsync(10, Token));
        Assert.Empty(await store.GetCyclesAsync(10, Token));
    }

    [Fact]
    public async Task Initializing_again_keeps_the_schema_and_the_rows()
    {
        var outageId = await store.AddOutageAsync(OpenOutage(0), Token);
        var cycleId = await store.AddCycleAsync(RunningCycle(1), Token);

        await store.InitializeAsync(Token);

        Assert.Equal(1, await ScalarAsync("PRAGMA user_version"));
        Assert.Equal(outageId, (await store.GetOpenOutageAsync(Token))?.Id);
        Assert.Equal(cycleId, (await store.GetUnfinishedCycleAsync(Token))?.Id);
    }

    [Fact]
    public async Task An_outage_comes_back_with_every_field_as_added_and_as_updated()
    {
        // The Id of the record that is passed in is ignored: the store assigns its own.
        var added = new Ipv4Outage { Id = 999, LastGoodUtc = Start.AddSeconds(-30), DetectedUtc = Start };
        var id = await store.AddOutageAsync(added, Token);

        Assert.True(id > 0);
        Assert.NotEqual(999, id);

        var stored = (await store.GetOutagesAsync(10, Token)).Single();
        Assert.Equal(added with { Id = id }, stored);
        Assert.Null(stored.RecoveredUtc);
        Assert.Null(stored.Recovery);
        AssertUtc(stored.LastGoodUtc, stored.DetectedUtc);

        // Every column is written, so a time that was there can go away again and a missing one can appear.
        var recovered = stored with { LastGoodUtc = null, RecoveredUtc = Start.AddMinutes(7), Recovery = Ipv4Recovery.AfterCycle };
        await store.UpdateOutageAsync(recovered, Token);

        var updated = (await store.GetOutagesAsync(10, Token)).Single();
        Assert.Equal(recovered, updated);
        Assert.Null(updated.LastGoodUtc);
        AssertUtc(updated.DetectedUtc, updated.RecoveredUtc);
    }

    [Theory]
    [InlineData(Ipv4Recovery.ByItself)]
    [InlineData(Ipv4Recovery.AfterCycle)]
    public async Task Every_way_an_outage_can_end_is_kept(Ipv4Recovery recovery)
    {
        var id = await store.AddOutageAsync(OpenOutage(0), Token);
        await store.UpdateOutageAsync(OpenOutage(0) with { Id = id, RecoveredUtc = Start.AddMinutes(3), Recovery = recovery }, Token);

        Assert.Equal(recovery, (await store.GetOutagesAsync(10, Token)).Single().Recovery);
    }

    [Fact]
    public async Task Updating_an_outage_leaves_the_others_alone_and_an_unknown_id_writes_nothing()
    {
        var first = await store.AddOutageAsync(OpenOutage(0), Token);
        var second = await store.AddOutageAsync(OpenOutage(10), Token);
        Assert.True(second > first);

        await store.UpdateOutageAsync(OpenOutage(0) with { Id = first, RecoveredUtc = Start.AddMinutes(2), Recovery = Ipv4Recovery.ByItself }, Token);
        await store.UpdateOutageAsync(OpenOutage(20) with { Id = second + 100, RecoveredUtc = Start.AddMinutes(22), Recovery = Ipv4Recovery.ByItself }, Token);

        var outages = await store.GetOutagesAsync(10, Token);
        Assert.Equal([second, first], outages.Select(o => o.Id));
        Assert.Equal(OpenOutage(10) with { Id = second }, outages[0]);
        Assert.Equal(Ipv4Recovery.ByItself, outages[1].Recovery);
    }

    [Fact]
    public async Task The_open_outage_is_the_newest_one_that_has_not_recovered()
    {
        var first = await store.AddOutageAsync(OpenOutage(0), Token);
        var second = await store.AddOutageAsync(OpenOutage(10), Token);
        var third = await store.AddOutageAsync(OpenOutage(20), Token);

        // A recovered outage is never the open one, however new it is.
        await store.UpdateOutageAsync(OpenOutage(20) with { Id = third, RecoveredUtc = Start.AddMinutes(25), Recovery = Ipv4Recovery.ByItself }, Token);
        Assert.Equal(OpenOutage(10) with { Id = second }, await store.GetOpenOutageAsync(Token));

        await store.UpdateOutageAsync(OpenOutage(10) with { Id = second, RecoveredUtc = Start.AddMinutes(15), Recovery = Ipv4Recovery.AfterCycle }, Token);
        Assert.Equal(OpenOutage(0) with { Id = first }, await store.GetOpenOutageAsync(Token));

        await store.UpdateOutageAsync(OpenOutage(0) with { Id = first, RecoveredUtc = Start.AddMinutes(5), Recovery = Ipv4Recovery.ByItself }, Token);
        Assert.Null(await store.GetOpenOutageAsync(Token));
    }

    [Fact]
    public async Task A_cycle_comes_back_with_every_field_as_added_and_as_updated()
    {
        var outageId = await store.AddOutageAsync(OpenOutage(0), Token);

        // A cycle of an outage that has everything but its end, as the watchdog stores it before IPv6 is switched off.
        var added = new Ipv4WatchdogCycle
        {
            Id = 999,
            OutageId = outageId,
            IsTest = false,
            StartedUtc = Start.AddMinutes(1),
            PreviousMode = "Automatic",
        };

        var id = await store.AddCycleAsync(added, Token);
        Assert.True(id > 0);
        Assert.NotEqual(999, id);

        var stored = (await store.GetCyclesAsync(10, Token)).Single();
        Assert.Equal(added with { Id = id }, stored);
        Assert.Equal(outageId, stored.OutageId);
        Assert.False(stored.IsTest);
        Assert.Null(stored.SwitchedOffUtc);
        Assert.Null(stored.SwitchedOnUtc);
        Assert.Null(stored.FinishedUtc);
        Assert.Null(stored.Result);
        Assert.Null(stored.Detail);

        // The whole cycle, a test cycle without an outage this time: every column is written, the outage's key too.
        var finished = stored with
        {
            OutageId = null,
            IsTest = true,
            StartedUtc = Start.AddMinutes(2),
            PreviousMode = "Disabled",
            SwitchedOffUtc = Start.AddMinutes(2).AddSeconds(5),
            SwitchedOnUtc = Start.AddMinutes(2).AddSeconds(35),
            FinishedUtc = Start.AddMinutes(3),
            Result = Ipv4WatchdogCycleResult.Ipv4StillDown,
            Detail = "The box was unreachable for 94 s; 'IPv6' is back as \"Disabled\".",
        };

        await store.UpdateCycleAsync(finished, Token);

        var updated = (await store.GetCyclesAsync(10, Token)).Single();
        Assert.Equal(finished, updated);
        Assert.Null(updated.OutageId);
        Assert.True(updated.IsTest);
        AssertUtc(updated.StartedUtc, updated.SwitchedOffUtc, updated.SwitchedOnUtc, updated.FinishedUtc);

        // And back to a cycle that is still running, to be sure that nothing sticks.
        var running = updated with { OutageId = outageId, IsTest = false, SwitchedOffUtc = null, SwitchedOnUtc = null, FinishedUtc = null, Result = null, Detail = null };
        await store.UpdateCycleAsync(running, Token);
        Assert.Equal(running, (await store.GetCyclesAsync(10, Token)).Single());
    }

    [Theory]
    [InlineData(Ipv4WatchdogCycleResult.Ipv4Back)]
    [InlineData(Ipv4WatchdogCycleResult.Ipv4StillDown)]
    [InlineData(Ipv4WatchdogCycleResult.Failed)]
    public async Task Every_result_a_cycle_can_have_is_kept(Ipv4WatchdogCycleResult result)
    {
        var id = await store.AddCycleAsync(RunningCycle(0), Token);
        await store.UpdateCycleAsync(RunningCycle(0) with { Id = id, FinishedUtc = Start.AddMinutes(1), Result = result }, Token);

        Assert.Equal(result, (await store.GetCyclesAsync(10, Token)).Single().Result);
    }

    [Fact]
    public async Task Updating_a_cycle_leaves_the_others_alone_and_an_unknown_id_writes_nothing()
    {
        var first = await store.AddCycleAsync(RunningCycle(0), Token);
        var second = await store.AddCycleAsync(RunningCycle(10, isTest: true), Token);
        Assert.True(second > first);

        await store.UpdateCycleAsync(RunningCycle(0) with { Id = first, FinishedUtc = Start.AddMinutes(1), Result = Ipv4WatchdogCycleResult.Failed, Detail = "No answer." }, Token);
        await store.UpdateCycleAsync(RunningCycle(20) with { Id = second + 100, FinishedUtc = Start.AddMinutes(21), Result = Ipv4WatchdogCycleResult.Failed }, Token);

        var cycles = await store.GetCyclesAsync(10, Token);
        Assert.Equal([second, first], cycles.Select(c => c.Id));
        Assert.Equal(RunningCycle(10, isTest: true) with { Id = second }, cycles[0]);
        Assert.Equal(Ipv4WatchdogCycleResult.Failed, cycles[1].Result);
        Assert.Equal("No answer.", cycles[1].Detail);
    }

    [Fact]
    public async Task The_unfinished_cycle_is_the_newest_one_without_a_result()
    {
        Assert.Null(await store.GetUnfinishedCycleAsync(Token));

        var first = await store.AddCycleAsync(RunningCycle(0), Token);
        var second = await store.AddCycleAsync(RunningCycle(10), Token);
        var third = await store.AddCycleAsync(RunningCycle(20), Token);

        Assert.Equal(RunningCycle(20) with { Id = third }, await store.GetUnfinishedCycleAsync(Token));

        // A finished cycle is not returned, however new it is; the older one that was left running is.
        await store.UpdateCycleAsync(RunningCycle(20) with { Id = third, FinishedUtc = Start.AddMinutes(21), Result = Ipv4WatchdogCycleResult.Ipv4Back }, Token);
        Assert.Equal(RunningCycle(10) with { Id = second }, await store.GetUnfinishedCycleAsync(Token));

        await store.UpdateCycleAsync(RunningCycle(10) with { Id = second, FinishedUtc = Start.AddMinutes(11), Result = Ipv4WatchdogCycleResult.Failed }, Token);
        Assert.Equal(RunningCycle(0) with { Id = first }, await store.GetUnfinishedCycleAsync(Token));

        await store.UpdateCycleAsync(RunningCycle(0) with { Id = first, FinishedUtc = Start.AddMinutes(1), Result = Ipv4WatchdogCycleResult.Ipv4StillDown }, Token);
        Assert.Null(await store.GetUnfinishedCycleAsync(Token));
    }

    [Fact]
    public async Task Cycles_are_counted_from_a_time_on_inclusive_and_test_cycles_count_too()
    {
        await store.AddCycleAsync(RunningCycle(0), Token);
        await store.AddCycleAsync(RunningCycle(10, isTest: true), Token);
        await store.AddCycleAsync(RunningCycle(20), Token);
        await store.AddCycleAsync(RunningCycle(30, isTest: true), Token);

        // The boundary is inclusive: a cycle that started exactly at the given time is counted.
        Assert.Equal(4, await store.CountCyclesSinceAsync(Start.AddMinutes(-1), Token));
        Assert.Equal(4, await store.CountCyclesSinceAsync(Start, Token));
        Assert.Equal(3, await store.CountCyclesSinceAsync(Start.AddSeconds(1), Token));
        Assert.Equal(3, await store.CountCyclesSinceAsync(Start.AddMinutes(10), Token));
        Assert.Equal(2, await store.CountCyclesSinceAsync(Start.AddMinutes(10).AddSeconds(1), Token));
        Assert.Equal(1, await store.CountCyclesSinceAsync(Start.AddMinutes(30), Token));
        Assert.Equal(0, await store.CountCyclesSinceAsync(Start.AddMinutes(30).AddSeconds(1), Token));
    }

    [Fact]
    public async Task Outages_come_newest_first_and_the_count_is_respected()
    {
        var ids = new List<long>();

        for (var i = 0; i < 5; i++)
        {
            ids.Add(await store.AddOutageAsync(OpenOutage(i * 10), Token));
        }

        var newest = ids.AsEnumerable().Reverse().ToList();

        Assert.Equal(newest, (await store.GetOutagesAsync(100, Token)).Select(o => o.Id));
        Assert.Equal(newest.Take(3), (await store.GetOutagesAsync(3, Token)).Select(o => o.Id));
        Assert.Equal(newest.Take(1), (await store.GetOutagesAsync(1, Token)).Select(o => o.Id));
        Assert.Equal(OpenOutage(40) with { Id = ids[4] }, (await store.GetOutagesAsync(1, Token)).Single());
        Assert.Empty(await store.GetOutagesAsync(0, Token));

        // A negative count is none, not SQLite's "no limit".
        Assert.Empty(await store.GetOutagesAsync(-1, Token));
    }

    [Fact]
    public async Task Cycles_come_newest_first_and_the_count_is_respected()
    {
        var ids = new List<long>();

        for (var i = 0; i < 5; i++)
        {
            ids.Add(await store.AddCycleAsync(RunningCycle(i * 10, isTest: i % 2 == 1), Token));
        }

        var newest = ids.AsEnumerable().Reverse().ToList();

        Assert.Equal(newest, (await store.GetCyclesAsync(100, Token)).Select(c => c.Id));
        Assert.Equal(newest.Take(3), (await store.GetCyclesAsync(3, Token)).Select(c => c.Id));
        Assert.Equal(newest.Take(1), (await store.GetCyclesAsync(1, Token)).Select(c => c.Id));
        Assert.Equal(RunningCycle(40) with { Id = ids[4] }, (await store.GetCyclesAsync(1, Token)).Single());
        Assert.Empty(await store.GetCyclesAsync(0, Token));

        // A negative count is none, not SQLite's "no limit".
        Assert.Empty(await store.GetCyclesAsync(-1, Token));
    }

    [Fact]
    public async Task Rows_added_at_the_same_time_all_get_an_id_of_their_own()
    {
        var outageIds = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => store.AddOutageAsync(OpenOutage(i), Token)));
        var cycleIds = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => store.AddCycleAsync(RunningCycle(i), Token)));

        Assert.Equal(20, outageIds.Distinct().Count());
        Assert.Equal(20, cycleIds.Distinct().Count());
        Assert.Equal(20, (await store.GetOutagesAsync(100, Token)).Count);
        Assert.Equal(20, (await store.GetCyclesAsync(100, Token)).Count);
    }

    [Fact]
    public async Task Everything_survives_a_second_store_on_the_same_file_as_after_a_restart()
    {
        var finishedOutage = await store.AddOutageAsync(OpenOutage(0), Token);
        await store.UpdateOutageAsync(OpenOutage(0) with { Id = finishedOutage, RecoveredUtc = Start.AddMinutes(4), Recovery = Ipv4Recovery.AfterCycle }, Token);
        var openOutage = await store.AddOutageAsync(OpenOutage(10), Token);

        var finishedCycle = await store.AddCycleAsync(RunningCycle(1, finishedOutage), Token);
        await store.UpdateCycleAsync(RunningCycle(1, finishedOutage) with { Id = finishedCycle, FinishedUtc = Start.AddMinutes(2), Result = Ipv4WatchdogCycleResult.Ipv4Back, Detail = "Back." }, Token);
        var runningCycle = await store.AddCycleAsync(RunningCycle(11, openOutage), Token);

        var outagesBefore = await store.GetOutagesAsync(10, Token);
        var cyclesBefore = await store.GetCyclesAsync(10, Token);

        // The pool would hand the new store the connection of the old one; clearing it makes the file really be opened again.
        SqliteConnection.ClearAllPools();
        var restarted = CreateStore();
        await restarted.InitializeAsync(Token);

        Assert.Equal(outagesBefore, await restarted.GetOutagesAsync(10, Token));
        Assert.Equal(cyclesBefore, await restarted.GetCyclesAsync(10, Token));
        Assert.Equal(OpenOutage(10) with { Id = openOutage }, await restarted.GetOpenOutageAsync(Token));
        Assert.Equal(RunningCycle(11, openOutage) with { Id = runningCycle }, await restarted.GetUnfinishedCycleAsync(Token));
        Assert.Equal(1, await restarted.CountCyclesSinceAsync(Start.AddMinutes(11), Token));
        Assert.Equal(2, await restarted.CountCyclesSinceAsync(Start, Token));

        // The new store carries on where the old one stopped.
        Assert.True(await restarted.AddOutageAsync(OpenOutage(20), Token) > openOutage);
        Assert.True(await restarted.AddCycleAsync(RunningCycle(21), Token) > runningCycle);
    }

    private Ipv4WatchdogStore CreateStore() => new(loggers.CreateLogger<Ipv4WatchdogStore>(), Path.Combine(directory, Ipv4WatchdogStore.FileName));

    private static Ipv4Outage OpenOutage(int minutes) => new() { LastGoodUtc = Start.AddMinutes(minutes).AddSeconds(-30), DetectedUtc = Start.AddMinutes(minutes) };

    private static Ipv4WatchdogCycle RunningCycle(int minutes, long? outageId = null, bool isTest = false) => new()
    {
        OutageId = outageId,
        IsTest = isTest,
        StartedUtc = Start.AddMinutes(minutes),
        PreviousMode = "Automatic",
    };

    private static void AssertUtc(params DateTime?[] times)
    {
        Assert.All(times.OfType<DateTime>(), time => Assert.Equal(DateTimeKind.Utc, time.Kind));
    }

    /// <summary>Asks the file itself, behind the store's back, so that the schema is checked and not what the store believes of it.</summary>
    private async Task<long> ScalarAsync(string sql)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = store.FilePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(Token), CultureInfo.InvariantCulture);
    }
}
