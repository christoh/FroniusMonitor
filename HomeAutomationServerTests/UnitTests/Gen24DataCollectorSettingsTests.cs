using System.Text.RegularExpressions;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The Gen24DataCollector element of Settings.xml as the server writes and reads it: attributes, defaults that are
/// left out, and Connections staying out of the element entirely (it comes from Gen24Connections instead).
/// </summary>
public sealed class Gen24DataCollectorSettingsTests
{
    [Fact]
    public void The_section_round_trips_as_attributes()
    {
        var settings = new Settings
        {
            Gen24DataCollector = new Gen24DataCollectorParameters
            {
                RefreshRate = TimeSpan.FromSeconds(30),
                ConfigRefreshRate = TimeSpan.FromMinutes(2),
                LogDirectory = "/data/log",
            },
        };

        var xml = SettingsXml.Serialize(settings);
        var back = SettingsXml.Deserialize(xml);

        Assert.Contains("<Gen24DataCollector ", xml);
        Assert.Contains("RefreshRate=\"PT30S\"", xml);
        Assert.Contains("ConfigRefreshRate=\"PT2M\"", xml);
        Assert.Contains("LogDirectory=\"/data/log\"", xml);
        Assert.Equal(TimeSpan.FromSeconds(30), back.Gen24DataCollector.RefreshRate);
        Assert.Equal(TimeSpan.FromMinutes(2), back.Gen24DataCollector.ConfigRefreshRate);
        Assert.Equal("/data/log", back.Gen24DataCollector.LogDirectory);
    }

    [Fact]
    public void Defaults_are_left_out_and_come_back_as_defaults()
    {
        var xml = SettingsXml.Serialize(new Settings());
        var back = SettingsXml.Deserialize(xml);

        Assert.Contains("<Gen24DataCollector", xml);
        Assert.DoesNotContain("RefreshRate", xml);
        Assert.DoesNotContain("ConfigRefreshRate", xml);
        Assert.DoesNotContain("LogDirectory", xml);
        Assert.Equal(TimeSpan.FromSeconds(5), back.Gen24DataCollector.RefreshRate);
        Assert.Equal(TimeSpan.FromMinutes(5), back.Gen24DataCollector.ConfigRefreshRate);
        Assert.Equal("/var/log", back.Gen24DataCollector.LogDirectory);
    }

    [Fact]
    public void Connections_are_not_part_of_the_element()
    {
        var xml = SettingsXml.Serialize(new Settings { Gen24Connections = [new WebConnection { BaseUrl = "http://192.168.1.1" }] });
        var element = Regex.Match(xml, "<Gen24DataCollector[^>]*/?>").Value;

        Assert.DoesNotContain("Connections", element);
    }
}
