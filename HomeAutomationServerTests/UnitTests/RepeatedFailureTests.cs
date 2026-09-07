using De.Hochstaetter.Fronius.Services.DataCollectors;
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
}
