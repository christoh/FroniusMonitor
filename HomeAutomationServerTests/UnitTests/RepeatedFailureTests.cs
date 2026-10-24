using De.Hochstaetter.HomeAutomationServer.Services.DataCollectors;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What a data collector writes about a device that keeps failing, which used to be one stack trace per round.
/// </summary>
public class RepeatedFailureTests
{
    private static readonly TimeSpan hour = TimeSpan.FromHours(1);

    private static readonly DateTimeOffset noon = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static (RepeatedFailure Failures, SettableTimeProvider Clock) NewFailure()
    {
        var clock = new SettableTimeProvider(noon);
        return (new RepeatedFailure(hour, clock), clock);
    }

    private static Exception Offline(string message = "No route to host") => new HttpRequestException(message);

    [Fact]
    public void The_first_failure_is_worth_the_whole_exception()
    {
        var (failures, _) = NewFailure();

        Assert.Equal(FailureReport.First, failures.Record(Offline()));
        Assert.Equal(1, failures.Attempts);
    }

    [Fact]
    public void The_same_failure_again_says_nothing()
    {
        var (failures, clock) = NewFailure();
        failures.Record(Offline());

        for (var round = 0; round < 100; round++)
        {
            clock.Now += TimeSpan.FromSeconds(5);
            Assert.Equal(FailureReport.Silent, failures.Record(Offline()));
        }

        Assert.Equal(101, failures.Attempts);
    }

    [Fact]
    public void After_an_hour_of_the_same_failure_it_is_mentioned_again()
    {
        var (failures, clock) = NewFailure();
        failures.Record(Offline());

        clock.Now += hour - TimeSpan.FromSeconds(1);
        Assert.Equal(FailureReport.Silent, failures.Record(Offline()));

        clock.Now += TimeSpan.FromSeconds(1);
        Assert.Equal(FailureReport.StillFailing, failures.Record(Offline()));

        // And then quiet again for another hour.
        clock.Now += TimeSpan.FromMinutes(30);
        Assert.Equal(FailureReport.Silent, failures.Record(Offline()));
    }

    [Fact]
    public void Something_else_going_wrong_is_a_failure_of_its_own()
    {
        // Whatever has been failing so far, a different fault is news and gets its line.
        var (failures, clock) = NewFailure();
        failures.Record(Offline());
        clock.Now += TimeSpan.FromSeconds(5);

        Assert.Equal(FailureReport.First, failures.Record(Offline("Connection refused")));
        Assert.Equal(FailureReport.First, failures.Record(new TimeoutException("Timed out")));
        Assert.Equal(1, failures.Attempts);
    }

    [Fact]
    public void Recovering_reports_what_the_outage_amounted_to()
    {
        var (failures, clock) = NewFailure();

        for (var round = 0; round < 12; round++)
        {
            failures.Record(Offline());
            clock.Now += TimeSpan.FromMinutes(5);
        }

        var outage = failures.Recover();

        Assert.NotNull(outage);
        Assert.Equal(12, outage.Value.Attempts);
        Assert.Equal(TimeSpan.FromMinutes(60), outage.Value.Duration);
        Assert.Equal(0, failures.Attempts);
    }

    [Fact]
    public void Recovering_when_nothing_was_wrong_reports_nothing()
    {
        var (failures, _) = NewFailure();

        Assert.Null(failures.Recover());
    }

    [Fact]
    public void A_failure_after_a_recovery_starts_over()
    {
        var (failures, clock) = NewFailure();
        failures.Record(Offline());
        clock.Now += TimeSpan.FromSeconds(5);
        failures.Recover();

        Assert.Equal(FailureReport.First, failures.Record(Offline()));
    }

    private const string What = "read the sensors";
    private const string Device = "GEN24 inverter";
    private const string Address = "http://192.168.1.10";

    [Fact]
    public void Logging_the_first_failure_writes_one_error_with_the_exception()
    {
        var (failures, _) = NewFailure();
        var logger = new CapturingLogger();
        var exception = Offline();

        failures.Log(logger, exception, What, Device, Address);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Equal("Could not read the sensors of GEN24 inverter http://192.168.1.10", entry.Message);
    }

    [Fact]
    public void Logging_the_same_failure_again_within_the_interval_writes_debug_only()
    {
        var (failures, clock) = NewFailure();
        var logger = new CapturingLogger();
        failures.Log(logger, Offline(), What, Device, Address);
        logger.Entries.Clear();

        var again = Offline();
        clock.Now += hour - TimeSpan.FromSeconds(1);
        failures.Log(logger, again, What, Device, Address);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Same(again, entry.Exception);
        Assert.Equal("Could not read the sensors of GEN24 inverter http://192.168.1.10, as before", entry.Message);
    }

    [Fact]
    public void Logging_the_same_failure_round_after_round_never_gets_louder_within_the_interval()
    {
        var (failures, clock) = NewFailure();
        var logger = new CapturingLogger();
        failures.Log(logger, Offline(), What, Device, Address);

        for (var round = 0; round < 100; round++)
        {
            clock.Now += TimeSpan.FromSeconds(5);
            failures.Log(logger, Offline(), What, Device, Address);
        }

        Assert.Equal(LogLevel.Error, logger.Entries[0].Level);
        Assert.Equal(100, logger.Entries.Count(e => e.Level == LogLevel.Debug));
        Assert.Equal(101, logger.Entries.Count);
    }

    [Fact]
    public void Logging_the_same_failure_after_the_interval_writes_one_warning_with_the_attempt_count()
    {
        var (failures, clock) = NewFailure();
        var logger = new CapturingLogger();
        failures.Log(logger, Offline(), What, Device, Address);
        clock.Now += TimeSpan.FromSeconds(5);
        failures.Log(logger, Offline(), What, Device, Address);
        logger.Entries.Clear();

        clock.Now += hour;
        failures.Log(logger, Offline(), What, Device, Address);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal("Still cannot read the sensors of GEN24 inverter http://192.168.1.10 after 3 attempts: No route to host", entry.Message);
    }

    [Fact]
    public void Logging_something_else_going_wrong_writes_an_error_of_its_own()
    {
        var (failures, clock) = NewFailure();
        var logger = new CapturingLogger();
        failures.Log(logger, Offline(), What, Device, Address);
        logger.Entries.Clear();

        clock.Now += TimeSpan.FromSeconds(5);
        var other = Offline("Connection refused");
        failures.Log(logger, other, What, Device, Address);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(other, entry.Exception);
    }

    [Fact]
    public void Logging_a_recovery_after_failures_writes_one_information_with_the_attempts()
    {
        var (failures, clock) = NewFailure();
        var logger = new CapturingLogger();

        for (var round = 0; round < 12; round++)
        {
            failures.Log(logger, Offline(), What, Device, Address);
            clock.Now += TimeSpan.FromMinutes(5);
        }

        logger.Entries.Clear();

        failures.LogRecovery(logger, What, Device, Address);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal("Can read the sensors of GEN24 inverter http://192.168.1.10 again, after 12 attempts over 1:00:00", entry.Message);

        // It has answered; saying so a second time would be noise.
        failures.LogRecovery(logger, What, Device, Address);
        Assert.Single(logger.Entries);
    }

    [Fact]
    public void Logging_a_recovery_when_nothing_failed_writes_nothing()
    {
        var (failures, _) = NewFailure();
        var logger = new CapturingLogger();

        failures.LogRecovery(logger, What, Device, Address);

        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    public void A_logger_with_a_level_disabled_gets_no_call_for_it(LogLevel disabled)
    {
        var (failures, clock) = NewFailure();
        var logger = new CapturingLogger(disabled);

        failures.Log(logger, Offline(), What, Device, Address);
        clock.Now += TimeSpan.FromSeconds(5);
        failures.Log(logger, Offline(), What, Device, Address);
        clock.Now += hour;
        failures.Log(logger, Offline(), What, Device, Address);
        failures.LogRecovery(logger, What, Device, Address);

        LogLevel[] everything = [LogLevel.Error, LogLevel.Debug, LogLevel.Warning, LogLevel.Information];
        Assert.Equal(everything.Where(level => level != disabled), logger.Entries.Select(e => e.Level));
    }

    [Fact]
    public void A_logger_with_every_level_disabled_gets_no_call_and_the_failures_are_counted_all_the_same()
    {
        var (failures, clock) = NewFailure();
        var logger = new CapturingLogger(LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical);

        failures.Log(logger, Offline(), What, Device, Address);
        clock.Now += TimeSpan.FromSeconds(5);
        failures.Log(logger, Offline(), What, Device, Address);
        clock.Now += hour;
        failures.Log(logger, Offline(), What, Device, Address);

        Assert.Equal(3, failures.Attempts);

        failures.LogRecovery(logger, What, Device, Address);

        Assert.Equal(0, failures.Attempts);
        Assert.Empty(logger.Entries);
    }

    /// <summary>
    /// Keeps every call of <see cref="Log{TState}"/> with its exception, which <c>RecordingLogger</c> leaves out, and has
    /// the levels it is told to refuse. A call is recorded whether or not its level is enabled, as the logger of a
    /// framework would filter inside <c>Log</c> and nobody could tell: it is the caller that must not make the call.
    /// </summary>
    private sealed class CapturingLogger(params LogLevel[] disabled) : ILogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && !disabled.Contains(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }
}
