using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.Fronius.Models.Gen24;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The warning the inverter tile shows when the firmware reports API versions this software was not made for.
/// </summary>
/// <remarks>
/// Firmware 1.42.9-1 raised configApi to 11.0.0 and the warning said "breaking changes" although everything read
/// from the config was still where it was. Since then each API has a highest major version of its own instead of
/// the major version of its minimum.
/// </remarks>
public class Gen24ConfigTests
{
    private const string Firmware = "1.42.9-1";

    // What 1.42.9-1 reports on a Symo GEN24 10.0, from api/status/version on 2026-10-15.
    private static Gen24Config Reporting(string configApi) => new()
    {
        Versions = new Gen24Versions
        {
            CommandApi = new Version(10, 2, 0),
            ConfigApi = Version.Parse(configApi),
            ComponentsApi = new Version(1, 1, 0),
            StatusApi = new Version(6, 2, 0),
            SwVersions = new Dictionary<string, Version> { ["GEN24"] = new(1, 42, 9, 1) },
        },
    };

    [Theory]
    [InlineData("11.0.0")] // firmware 1.42.9-1
    [InlineData("10.3.0")] // the oldest config API still accepted
    public void A_tested_config_api_gives_no_warning(string configApi)
    {
        Assert.Null(Reporting(configApi).VersionWarning);
    }

    [Fact]
    public void A_config_api_above_the_newest_tested_major_version_is_too_new()
    {
        Assert.Equal(string.Format(Resources.FirmwareTooNew, Firmware), Reporting("12.0.0").VersionWarning);
    }

    [Fact]
    public void A_config_api_below_the_minimum_is_too_old()
    {
        Assert.Equal(string.Format(Resources.FirmwareTooOld, Firmware, "1.41.10-1"), Reporting("10.2.0").VersionWarning);
    }

    [Fact]
    public void Without_versions_there_is_no_warning()
    {
        Assert.Null(new Gen24Config().VersionWarning);
    }
}
