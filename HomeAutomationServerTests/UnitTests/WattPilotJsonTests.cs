using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using De.Hochstaetter.Fronius.Attributes;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Wifi;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The names a WattPilot is sent and read under.
/// </summary>
/// <remarks>
/// Writing a value used to go through Newtonsoft, which took the names off the <c>JsonProperty</c> attributes the
/// charging models carry - a second copy of the names the <see cref="WattPilotAttribute"/>s already state. System.
/// Text.Json does not read those attributes, so writing through it would have sent the charger the C# names
/// instead and it would have ignored the message. The writer now goes off the <see cref="WattPilotAttribute"/>s,
/// and these tests are what says so: what is written comes back through the reader unchanged.
/// </remarks>
public class WattPilotJsonTests
{
    private static WattPilotAttribute AttributeOf(string propertyName) =>
        typeof(WattPilot).GetProperty(propertyName)!.GetCustomAttribute<WattPilotAttribute>()!;

    [Fact]
    public void A_nested_object_is_written_under_the_names_the_charger_uses()
    {
        var currents = new WattPilotLoadBalancingCurrents
        {
            LocalMaximumCurrent = 16,
            DynamicMaximumCurrent = 10,
            MaximumCurrentDnoLine = 32,
            TimeStampEpoch = 1_700_000_000,
        };

        var written = WattPilotExtensions.ToWattPilotJson(currents, AttributeOf(nameof(WattPilot.LoadBalancingCurrents)))!.AsObject();

        Assert.Equal(16, written["amp"].AsInt32());
        Assert.Equal(32, written["dyn"].AsInt32());
        Assert.Equal(10, written["sta"].AsInt32());
        Assert.Equal(1_700_000_000, written["ts"].AsInt32());

        // TimeStamp is the epoch seconds read as a date. Sending it as well would be sending the same thing twice
        // under a name the charger does not know, which is what a serializer that has never heard of the WattPilot
        // attributes does.
        Assert.Equal(4, written.Count);
    }

    [Fact]
    public void What_is_written_reads_back_as_what_it_was()
    {
        // The reader is the other half of the same format, so a round trip through it is the check that the names
        // written are the names the charger would have answered with.
        var original = new WattPilotLoadBalancingCurrents
        {
            LocalMaximumCurrent = 16,
            DynamicMaximumCurrent = 10,
            MaximumCurrentDnoLine = 32,
            TimeStampEpoch = 1_700_000_000,
        };

        var written = WattPilotExtensions.ToWattPilotJson(original, AttributeOf(nameof(WattPilot.LoadBalancingCurrents)))!.AsObject();

        var wattPilot = new WattPilot();
        wattPilot.UpdateFromJson(new JsonObject { ["lot"] = written });

        Assert.Equal(original, wattPilot.LoadBalancingCurrents);
    }

    [Fact]
    public void A_wifi_is_written_under_the_names_the_charger_uses()
    {
        // The WiFi model is the one that would have gone most obviously wrong: the name it is sent under and the
        // name the server sends the client under differ on purpose - "ip" against "ipV4Address", "gw" against
        // "gateway" - so a serializer reading the wrong set of attributes produces a message the charger drops.
        var wifi = new WattPilotWifiInfo
        {
            Ssid = "somewhere",
            IpV4AddressString = "192.168.178.22",
            GatewayString = "192.168.178.1",
            NetMaskString = "255.255.255.0",
            MacAddressString = "00:11:22:33:44:55",
            Channel = 11,
            WifiSignal = -55,
            IsG = true,
            CountryCode = "AT",
        };

        var written = WattPilotExtensions.ToWattPilotJson(wifi, AttributeOf(nameof(WattPilot.CurrentWifi)))!.AsObject();

        Assert.Equal("somewhere", written["ssid"].AsString());
        Assert.Equal("192.168.178.22", written["ip"].AsString());
        Assert.Equal("192.168.178.1", written["gw"].AsString());
        Assert.Equal("255.255.255.0", written["netmask"].AsString());
        Assert.Equal("00:11:22:33:44:55", written["bssid"].AsString());
        Assert.Equal(11, written["channel"].AsInt32());
        Assert.Equal(-55, written["rssi"].AsInt32());

        // A property that has a name of its own goes out under it, even where it also arrives as one slot of the
        // "f" array the charger packs the capabilities into.
        Assert.True(written["g"].AsBoolean());

        // And what the charger cannot be told is left out entirely: the "f" array, which is its answer rather than
        // something to send back; a property that only ever arrives in it; a caption; a parsed address.
        Assert.False(written.ContainsKey("f"));
        Assert.False(written.ContainsKey("CountryCode"));
        Assert.False(written.ContainsKey("Type"));
        Assert.False(written.ContainsKey("DisplayName"));
        Assert.False(written.ContainsKey("IpV4Address"));
    }

    [Fact]
    public void An_enum_is_written_as_the_number_the_charger_answers_with()
    {
        var wifi = new WattPilotWifiInfo { Encryption = WifiEncryption.WpaPsk };

        var written = WattPilotExtensions.ToWattPilotJson(wifi, AttributeOf(nameof(WattPilot.CurrentWifi)))!.AsObject();

        Assert.Equal((int)WifiEncryption.WpaPsk, written["encryptionType"].AsInt32());
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(16, "16")]
    [InlineData("text", "\"text\"")]
    [InlineData(1.5, "1.5")]
    [InlineData(null, "null")]
    public void A_scalar_is_written_as_itself(object? value, string expected)
    {
        Assert.Equal(expected, WattPilotExtensions.ToWattPilotJson(value, AttributeOf(nameof(WattPilot.CurrentWifi)))?.ToJsonString() ?? "null");
    }

    [Fact]
    public void A_byte_array_is_written_as_an_array_of_numbers()
    {
        // The card map: the charger wants numbers, and a byte array serializes to base 64 if left to a serializer.
        var written = WattPilotExtensions.ToWattPilotJson(new byte[] { 1, 2, 255 }, AttributeOf(nameof(WattPilot.Map)));

        Assert.Equal("[1,2,255]", written?.ToJsonString());
    }

    [Fact]
    public void A_nested_object_is_read_through_its_own_names()
    {
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson("""{ "ccw": { "ssid": "somewhere", "ip": "192.168.178.22", "gw": "192.168.178.1", "channel": 11 } }""");

        Assert.Equal("somewhere", wattPilot.CurrentWifi?.Ssid);
        Assert.Equal("192.168.178.22", wattPilot.CurrentWifi?.IpV4AddressString);
        Assert.Equal("192.168.178.1", wattPilot.CurrentWifi?.GatewayString);
        Assert.Equal(11, wattPilot.CurrentWifi?.Channel);

        // The parsed form comes off the string, so the string is the only thing the charger has to send.
        Assert.Equal(IPAddress.Parse("192.168.178.22"), wattPilot.CurrentWifi?.IpV4Address);
    }

    [Fact]
    public void A_list_of_nested_objects_is_read_through_their_own_names()
    {
        // This is the one that would have gone wrong in silence: the serializer takes a list of objects happily
        // and answers entries with every property left at its default, so the cards would have been there but
        // empty, and nobody would appear to have charged anything.
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson("""{ "cards": [ { "name": "Christoph", "energy": 12.5, "cardId": true }, { "name": "guest", "energy": 0, "cardId": false } ] }""");

        Assert.Equal(2, wattPilot.Cards?.Count);
        Assert.Equal("Christoph", wattPilot.Cards?[0].Name);
        Assert.Equal(12.5, wattPilot.Cards?[0].Energy);
        Assert.True(wattPilot.Cards?[0].HaveCardId);
        Assert.Equal("guest", wattPilot.Cards?[1].Name);
        Assert.False(wattPilot.Cards?[1].HaveCardId);
    }

    [Fact]
    public void A_card_is_also_read_from_one_key_per_property()
    {
        // Newer firmware no longer sends the cards as one array but every property of every card as a key of
        // its own - the prefix, the index of the card, one letter for the property. The list has to come into
        // being from those keys alone, including a card in the middle nothing has been said about yet.
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson("""{ "c0n": "BMW 530e CH", "c0e": 69154, "c0i": true, "c2n": "EQC 400 CH", "c2i": false }""");

        Assert.Equal(3, wattPilot.Cards?.Count);
        Assert.Equal("BMW 530e CH", wattPilot.Cards?[0].Name);
        Assert.Equal(69154, wattPilot.Cards?[0].Energy);
        Assert.True(wattPilot.Cards?[0].HaveCardId);
        Assert.Null(wattPilot.Cards?[1].Name);
        Assert.Null(wattPilot.Cards?[1].Energy);
        Assert.Equal("EQC 400 CH", wattPilot.Cards?[2].Name);
        Assert.False(wattPilot.Cards?[2].HaveCardId);
    }

    [Fact]
    public void One_key_of_a_card_changes_that_card_in_place()
    {
        // A card charging is one "c1e" after another. The card must change under the bindings that show it,
        // not be replaced along with the whole list every time.
        var wattPilot = new WattPilot();
        wattPilot.UpdateFromJson("""{ "cards": [ { "name": "Christoph", "energy": 12.5, "cardId": true }, { "name": "guest", "energy": 0, "cardId": false } ] }""");
        var cards = wattPilot.Cards;

        wattPilot.UpdateFromJson("""{ "c1e": 99 }""");

        Assert.Same(cards, wattPilot.Cards);
        Assert.Equal(2, wattPilot.Cards?.Count);
        Assert.Equal(99, wattPilot.Cards?[1].Energy);
        Assert.Equal("guest", wattPilot.Cards?[1].Name);
        Assert.Equal(12.5, wattPilot.Cards?[0].Energy);
    }

    [Theory]
    [InlineData("""{ "c0p": "{}" }""")]
    [InlineData("""{ "cae": true, "cco": 30, "c": 1, "c0": 1 }""")]
    public void A_key_that_only_looks_like_a_card_key_is_ignored(string json)
    {
        // "c0p" is a card key for a property the card has no use for, the others start with the prefix and are
        // not card keys at all. None of them may conjure up a card.
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson(json);

        Assert.Null(wattPilot.Cards);
    }

    [Fact]
    public void A_scanned_wifi_list_is_read_through_its_own_names()
    {
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson("""{ "scan": [ { "ssid": "one", "rssi": -42 }, { "ssid": "two", "rssi": -80 } ] }""");

        Assert.Equal(2, wattPilot.ScannedWifis?.Count);
        Assert.Equal("one", wattPilot.ScannedWifis?[0].Ssid);
        Assert.Equal(-42, wattPilot.ScannedWifis?[0].WifiSignal);
        Assert.Equal("two", wattPilot.ScannedWifis?[1].Ssid);
    }

    [Fact]
    public void An_array_of_numbers_is_still_read_by_the_serializer()
    {
        // The phase map is a byte array, which has no WattPilot attributes of its own to be read through.
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson("""{ "map": [ 0, 1, 2, 3 ] }""");

        Assert.Equal<byte[]>([0, 1, 2, 3], wattPilot.Map);
    }

    [Fact]
    public void A_number_the_charger_wrote_as_text_is_still_read()
    {
        // System.Text.Json refuses a JSON string where a number is wanted, where Newtonsoft coerced it. The
        // charger is not consistent about which it writes, so the text fallback of the reader has to stay.
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson("""{ "amp": "16", "fna": "somewhere" }""");

        Assert.Equal<byte?>(16, wattPilot.MaximumChargingCurrent);
        Assert.Equal("somewhere", wattPilot.DeviceName);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\"1\"", true)]
    [InlineData("\"true\"", true)]
    public void A_flag_is_read_however_the_charger_writes_it(string json, bool expected)
    {
        // System.Text.Json will not read a number, or a string, into a bool, and Convert.ToBoolean refuses "1" as
        // well - so a charger writing a flag as 1 would have left the property unset.
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson($$"""{ "fup": {{json}} }""");

        Assert.Equal(expected, wattPilot.PvSurplusEnabled);
    }

    [Fact]
    public void One_array_the_charger_packs_several_flags_into_is_read_slot_by_slot()
    {
        // The WiFi capabilities arrive as one "f" array, and each property that comes out of it names its slot
        // with an index rather than a name of its own.
        var wattPilot = new WattPilot();

        wattPilot.UpdateFromJson("""{ "ccw": { "f": [ 4, 3, 1, 0, 1, 0, 1, 0, 0, "AT" ] } }""");

        Assert.Equal(WifiCipher.Ccmp, wattPilot.CurrentWifi?.PairwiseCipher);
        Assert.Equal(WifiCipher.Tkip, wattPilot.CurrentWifi?.GroupCipher);
        Assert.True(wattPilot.CurrentWifi?.IsB);
        Assert.False(wattPilot.CurrentWifi?.IsG);
        Assert.True(wattPilot.CurrentWifi?.IsN);
        Assert.False(wattPilot.CurrentWifi?.SupportsLowRate);
        Assert.True(wattPilot.CurrentWifi?.AllowWps);
        Assert.Equal("AT", wattPilot.CurrentWifi?.CountryCode);
        Assert.Equal("802.11 b/n", wattPilot.CurrentWifi?.Type);
    }

    [Fact]
    public void Something_the_charger_has_no_name_for_is_refused_rather_than_sent()
    {
        Assert.Throws<NotSupportedException>(() => WattPilotExtensions.ToWattPilotJson(new object(), AttributeOf(nameof(WattPilot.CurrentWifi))));
    }
}
