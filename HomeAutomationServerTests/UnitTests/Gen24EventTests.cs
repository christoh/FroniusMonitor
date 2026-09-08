using System.Globalization;
using System.Text.Json;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Models.Gen24;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// One entry of the event log of an inverter: how it is read, and what of it a client is sent.
/// </summary>
/// <remarks>
/// The description of an event is a translated string that has to be downloaded from the inverter, and
/// <see cref="Gen24Event"/> used to fetch it in the getter of <c>Message</c> - through the static injector, and
/// blocking on the async call. That only ever worked in FroniusMonitor, whose injector holds the one service
/// bound to the inverter on screen. The server registers that service per request and hands out one that has no
/// connection, so the download waited for a connection that never came; and because the getter was read while
/// the response was being serialized, <c>GET {id}/events</c> never answered at all. A client, where nothing
/// implements the interface, could not even construct one.
/// </remarks>
public class Gen24EventTests
{
    // ReadFroniusData reaches for IGen24JsonService through the static IoC, the way the models do throughout.
    // That injector is set up once for the whole assembly by TestInjector, because it is one per process.

    private const string OneEvent =
        """
        {
          "timestamp": 1757280000,
          "activeUntil": 1757283600,
          "label": "Grid frequency too high",
          "prefix": "STATE",
          "eventID": 1024,
          "severity": 2
        }
        """;

    private static Gen24Event Read(string json) => IoC.Get<IGen24JsonService>().ReadFroniusData<Gen24Event>(JsonNode.Parse(json));

    [Fact]
    public void An_event_is_read_from_what_the_inverter_writes()
    {
        var froniusEvent = Read(OneEvent);

        Assert.Equal(DateTime.UnixEpoch.AddSeconds(1757280000), froniusEvent.EventTime);
        Assert.Equal(DateTime.UnixEpoch.AddSeconds(1757283600), froniusEvent.ActiveUntil);
        Assert.Equal("Grid frequency too high", froniusEvent.Label);
        Assert.Equal(Severity.Warning, froniusEvent.Severity);
        Assert.Equal("STATE-1024", froniusEvent.Code);
    }

    [Fact]
    public void An_event_that_is_still_going_has_no_end()
    {
        var froniusEvent = Read("""{ "timestamp": 1757280000, "eventID": 1024, "prefix": "STATE", "severity": 1 }""");

        Assert.Null(froniusEvent.ActiveUntil);
        Assert.Equal(Severity.Error, froniusEvent.Severity);
    }

    [Theory]
    [InlineData("STATE", 1024u, "STATE-1024")]
    [InlineData("", 1024u, "1024")]
    [InlineData(null, 1024u, "1024")]
    [InlineData("STATE", null, "STATE-")]
    [InlineData(null, null, "")]
    public void A_code_is_the_prefix_and_the_id(string? prefix, uint? eventId, string expected)
    {
        // The key the description is looked up under, so it has to read the same here as the translation file of
        // the inverter writes it.
        var froniusEvent = new Gen24Event { Prefix = prefix, EventId = eventId };

        Assert.Equal(expected, froniusEvent.Code);
    }

    [Fact]
    public void A_code_is_written_the_same_whatever_the_current_culture_is()
    {
        var previous = CultureInfo.CurrentCulture;

        try
        {
            // A culture with its own digits would otherwise write an event id nothing can look up.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-EG");
            Assert.Equal("STATE-1024", new Gen24Event { Prefix = "STATE", EventId = 1024 }.Code);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void An_event_survives_the_trip_to_a_client()
    {
        // What the events endpoint does. Nothing here needs an IGen24Service, and none of it blocks - which is
        // the whole point: reading a description while serializing is what stopped this from answering.
        var froniusEvent = Read(OneEvent);

        var arrived = JsonSerializer.Deserialize<Gen24Event>(JsonSerializer.Serialize(froniusEvent))!;

        Assert.Equal(froniusEvent.EventTime, arrived.EventTime);
        Assert.Equal(froniusEvent.ActiveUntil, arrived.ActiveUntil);
        Assert.Equal(froniusEvent.Label, arrived.Label);
        Assert.Equal(froniusEvent.Severity, arrived.Severity);
        Assert.Equal(froniusEvent.Code, arrived.Code);
    }

    [Fact]
    public void A_description_is_not_sent_to_a_client()
    {
        // It is in the language of whoever downloaded it, so the server's copy is no use to a client: the client
        // localizes Code itself. Sending it would be sending the wrong language and inviting it to be believed.
        var froniusEvent = Read(OneEvent);
        froniusEvent.Message = "Netzfrequenz zu hoch";

        var json = JsonSerializer.Serialize(froniusEvent);

        Assert.DoesNotContain("Netzfrequenz", json);
        Assert.Null(JsonSerializer.Deserialize<Gen24Event>(json)!.Message);
    }
}
