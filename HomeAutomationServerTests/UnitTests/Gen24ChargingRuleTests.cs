using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.Fronius.Services;


namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// Reading the time of use rules of an inverter on a thread that has no <see cref="SynchronizationContext"/> -
/// which is every request thread of the server.
/// </summary>
/// <remarks>
/// <see cref="Gen24ChargingRule.Parse"/> hands the rules to a <c>BindableCollection</c>, and that one throws a
/// <see cref="ThreadStateException"/> when it is given no context and finds none on the current thread. The server
/// therefore reads through <see cref="Gen24ChargingRule.ParseList"/>, and these tests are here because the
/// difference is invisible until something actually runs off a UI thread.
/// </remarks>
public class Gen24ChargingRuleTests
{
    private const string TwoRules =
        """
        {
          "timeofuse": [
            {
              "Active": true,
              "Power": 5000,
              "ScheduleType": "CHARGE_MAX",
              "TimeTable": { "Start": "01:30", "End": "05:45" },
              "Weekdays": { "Mon": true, "Tue": true, "Wed": true, "Thu": true, "Fri": true, "Sat": false, "Sun": false }
            },
            {
              "Active": false,
              "Power": 2500,
              "ScheduleType": "DISCHARGE_MAX",
              "TimeTable": { "Start": "18:00", "End": "21:15" },
              "Weekdays": { "Mon": false, "Tue": false, "Wed": false, "Thu": false, "Fri": false, "Sat": true, "Sun": true }
            }
          ]
        }
        """;

    // ParseList reaches for IGen24JsonService through the static IoC, the way the models do throughout. That
    // injector is set up once for the whole assembly by TestInjector, because it is one per process.

    [Fact]
    public void The_rules_are_read_without_a_synchronization_context()
    {
        Assert.Null(SynchronizationContext.Current);

        var rules = Gen24ChargingRule.ParseList(JsonNode.Parse(TwoRules));

        Assert.Equal(2, rules.Count);
        Assert.Equal("01:30", rules[0].StartTime);
        Assert.Equal("05:45", rules[0].EndTime);
        Assert.True(rules[0].IsActive);
        Assert.Equal(5000, rules[0].Power);
        Assert.True(rules[0].Monday);
        Assert.False(rules[0].Sunday);
        Assert.False(rules[1].IsActive);
        Assert.True(rules[1].Sunday);
    }

    [Theory]
    [InlineData("""{ "timeofuse": [] }""")]
    [InlineData("""{ "somethingElse": [] }""")]
    [InlineData("""{ }""")]
    public void A_config_without_rules_reads_as_an_empty_list(string json)
    {
        Assert.Empty(Gen24ChargingRule.ParseList(JsonNode.Parse(json)));
    }

    [Fact]
    public void No_token_at_all_reads_as_an_empty_list()
    {
        Assert.Empty(Gen24ChargingRule.ParseList(null));
    }
}
