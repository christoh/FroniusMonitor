using System.Text.Json;
using De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;
using De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// Solar.web's firmware answer as recorded on 2026-09-20 - a Gen24 twice (two data sources), and a battery Solar.web
/// does not manage - and the version format with the dash.
/// </summary>
public sealed class SolarWebFirmwareTests
{
    private const string PvSystemId = "318e321a-883b-4a82-868a-0fdc784476ad";

    private static readonly DateTime fetched = new(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

    internal const string Answer = """
        {"data":{"UpdateInfos":[
        {"DataSourceId":"pilot-0.5e-653991530307939023_1603873422","ComponentId":16580609,"UpdateFamily":"Battery","AvailableUpdate":{"LastUpdate":null,"InstalledVersion":null,"UpdateVersion":null,"UpdateVersionPrefix":"","ChangelogUrl":null,"NewerVersionAvailable":false,"IsUpdateAllowed":true},"InfoDescription":{"Text":"","State":"None","Url":null},"IsOnline":true,"UpdateRecommendationInfo":"Empty","UpdateStatus":"None"},
        {"DataSourceId":"pilot-0.6e-796954429721009271_1663595310","ComponentId":262144,"UpdateFamily":"Gen24Main","AvailableUpdate":{"LastUpdate":"2026-08-30T06:12:00Z","InstalledVersion":"1.41.11-1","UpdateVersion":"1.41.11-1","UpdateVersionPrefix":"","ChangelogUrl":"https://firmware-download.fronius.com/releaseGroup/Gen24-imxs6/common/1.41.11-1/changelog.pdf","NewerVersionAvailable":false,"IsUpdateAllowed":true},"InfoDescription":{"Text":"","State":"None","Url":null},"IsOnline":true,"UpdateRecommendationInfo":"LatestVersionInstalled","UpdateStatus":"Success"},
        {"DataSourceId":"pilot-0.5e-653991530307939023_1603873422","ComponentId":262144,"UpdateFamily":"Gen24Main","AvailableUpdate":{"LastUpdate":null,"InstalledVersion":"1.41.11-1","UpdateVersion":"1.42.3-2","UpdateVersionPrefix":"","ChangelogUrl":"https://firmware-download.fronius.com/releaseGroup/Gen24-imxs6/common/1.42.3-2/changelog.pdf","NewerVersionAvailable":true,"IsUpdateAllowed":true},"InfoDescription":{"Text":"Update recommended","State":"Info","Url":"https://www.fronius.com/"},"IsOnline":false,"UpdateRecommendationInfo":"NewerVersionAvailable","UpdateStatus":"None"}
        ]}}
        """;

    [Fact]
    public void The_components_come_with_their_versions_the_changelog_and_what_solar_web_manages()
    {
        var status = SolarWebFirmwareParser.Parse(Answer, PvSystemId, fetched);

        Assert.Equal(PvSystemId, status.PvSystemId);
        Assert.Equal(fetched, status.Timestamp);
        Assert.Equal(3, status.Components.Count);
        Assert.True(status.HasOutdatedFirmware);

        var battery = status.Components[0];
        Assert.Equal("Battery", battery.UpdateFamily);
        Assert.Equal(16580609, battery.ComponentId);
        Assert.Equal("pilot-0.5e-653991530307939023_1603873422", battery.DataSourceId);
        Assert.Null(battery.InstalledVersion);
        Assert.Null(battery.UpdateVersion);
        Assert.Null(battery.ChangelogUrl);
        Assert.Null(battery.LastUpdate);
        Assert.False(battery.IsManagedBySolarWeb);
        Assert.False(battery.IsOutdated);
        Assert.Equal("Empty", battery.UpdateRecommendationInfo);
        Assert.True(battery.IsOnline);

        var current = status.Components[1];
        Assert.Equal(new Version(1, 41, 11, 1), current.InstalledVersion);
        Assert.Equal(new Version(1, 41, 11, 1), current.UpdateVersion);
        Assert.Equal(new DateTime(2026, 8, 30, 6, 12, 0, DateTimeKind.Utc), current.LastUpdate);
        Assert.Equal("https://firmware-download.fronius.com/releaseGroup/Gen24-imxs6/common/1.41.11-1/changelog.pdf", current.ChangelogUrl);
        Assert.True(current.IsManagedBySolarWeb);
        Assert.False(current.IsOutdated);
        Assert.Equal("Success", current.UpdateStatus);
        Assert.Equal("LatestVersionInstalled", current.UpdateRecommendationInfo);

        var outdated = status.Components[2];
        Assert.Equal(new Version(1, 41, 11, 1), outdated.InstalledVersion);
        Assert.Equal(new Version(1, 42, 3, 2), outdated.UpdateVersion);
        Assert.True(outdated.NewerVersionAvailable);
        Assert.True(outdated.IsOutdated);
        Assert.False(outdated.IsOnline);
        Assert.Equal("Update recommended", outdated.InfoText);
        Assert.Equal("Info", outdated.InfoState);
        Assert.Equal("https://www.fronius.com/", outdated.InfoUrl);
        Assert.EndsWith("1.42.3-2/changelog.pdf", outdated.ChangelogUrl);
    }

    [Fact]
    public void A_version_with_a_dash_is_a_dotnet_version_with_a_revision_and_writes_back_with_the_dash()
    {
        Assert.Equal(new Version(1, 41, 11, 1), SolarWebVersion.Parse("1.41.11-1"));
        Assert.Equal(new Version(2, 0, 5), SolarWebVersion.Parse("2.0.5"));
        Assert.Null(SolarWebVersion.Parse(null));
        Assert.Null(SolarWebVersion.Parse(""));
        Assert.Throws<InvalidDataException>(() => SolarWebVersion.Parse("beta"));

        Assert.Equal("1.41.11-1", new Version(1, 41, 11, 1).ToSolarWebString());
        Assert.Equal("2.0.5", new Version(2, 0, 5).ToSolarWebString());
    }

    [Fact]
    public void Two_answers_are_the_same_when_their_components_are_and_the_time_stamp_does_not_count()
    {
        var first = SolarWebFirmwareParser.Parse(Answer, PvSystemId, fetched);
        var second = SolarWebFirmwareParser.Parse(Answer, PvSystemId, fetched.AddHours(1));
        var third = SolarWebFirmwareParser.Parse(Answer.Replace("\"NewerVersionAvailable\":true", "\"NewerVersionAvailable\":false"), PvSystemId, fetched);

        Assert.True(first.SameAs(second));
        Assert.False(first.SameAs(third));
        Assert.False(first.SameAs(null));
        Assert.False(first.SameAs(new SolarWebFirmwareStatus { PvSystemId = "other", Components = first.Components }));
    }

    [Fact]
    public void The_status_serializes_with_dotted_versions_and_reads_back()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var status = SolarWebFirmwareParser.Parse(Answer, PvSystemId, fetched);

        var json = JsonSerializer.Serialize(status, options);
        var back = JsonSerializer.Deserialize<SolarWebFirmwareStatus>(json, options);

        Assert.Contains("\"installedVersion\":\"1.41.11.1\"", json);
        Assert.Contains("\"updateVersion\":\"1.42.3.2\"", json);
        Assert.Contains("\"hasOutdatedFirmware\":true", json);
        Assert.NotNull(back);
        Assert.True(status.SameAs(back));
        Assert.Equal(new Version(1, 42, 3, 2), back.Components[2].UpdateVersion);
    }
}
