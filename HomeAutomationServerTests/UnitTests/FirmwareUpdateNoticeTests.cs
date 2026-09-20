using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// When the client tells the user about outdated firmware: once per component and offered version, whatever how
/// often the same status arrives, and again after a logout.
/// </summary>
public sealed class FirmwareUpdateNoticeTests
{
    [Fact]
    public void Outdated_firmware_is_announced_once_per_offered_version_and_unmanaged_components_never()
    {
        var notice = new FirmwareUpdateNotice();
        var status = Status(Gen24("1.41.11-1", "1.42.3-2", newer: true), Battery());

        var first = notice.Take(status);
        Assert.Equal("Gen24Main", Assert.Single(first).UpdateFamily);
        Assert.Equal("Gen24Main: 1.41.11-1 → 1.42.3-2", FirmwareUpdateNotice.Describe(first[0]));

        // The same status again, as a reconnect or a push of something else brings it: nothing to say.
        Assert.Empty(notice.Take(status));
        Assert.Empty(notice.Take(Status(Gen24("1.41.11-1", "1.42.3-2", newer: true))));

        // Installed: nothing to say either. A newer version later: told again.
        Assert.Empty(notice.Take(Status(Gen24("1.42.3-2", "1.42.3-2", newer: false))));
        Assert.Single(notice.Take(Status(Gen24("1.42.3-2", "1.43.0-1", newer: true))));

        // After a logout the next session is told again.
        notice.Reset();
        Assert.Single(notice.Take(Status(Gen24("1.42.3-2", "1.43.0-1", newer: true))));
    }

    [Fact]
    public void Nothing_is_announced_where_the_update_is_not_allowed_or_no_version_is_offered()
    {
        var notice = new FirmwareUpdateNotice();

        Assert.Empty(notice.Take(Status(Gen24("1.41.11-1", "1.42.3-2", newer: true) with { IsUpdateAllowed = false })));
        Assert.Empty(notice.Take(Status(Gen24("1.41.11-1", null, newer: true))));
        Assert.Empty(notice.Take(Status()));
    }

    private static SolarWebFirmwareStatus Status(params SolarWebFirmwareComponent[] components) => new() { PvSystemId = "sys", Components = [.. components] };

    private static SolarWebFirmwareComponent Gen24(string installed, string? offered, bool newer) => new()
    {
        DataSourceId = "pilot-1",
        ComponentId = 262144,
        UpdateFamily = "Gen24Main",
        InstalledVersion = SolarWebVersion.Parse(installed),
        UpdateVersion = SolarWebVersion.Parse(offered),
        NewerVersionAvailable = newer,
        IsUpdateAllowed = true,
        ChangelogUrl = offered == null ? null : $"https://firmware-download.fronius.com/{offered}/changelog.pdf",
    };

    private static SolarWebFirmwareComponent Battery() => new() { DataSourceId = "pilot-1", ComponentId = 16580609, UpdateFamily = "Battery", NewerVersionAvailable = false, IsUpdateAllowed = true };
}
