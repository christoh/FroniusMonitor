using De.Hochstaetter.HomeAutomationServer.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The XML shape of the server's <c>&lt;SolarWeb&gt;</c> element: the connection's own attributes on the element,
/// the password encrypted on save, the system id and the pacing kept, and a file without the element at all.
/// </summary>
public sealed class SolarWebSettingsTests : IDisposable
{
    private const string PvSystemId = "318e321a-883b-4a82-868a-0fdc784476ad";

    private readonly string fileName = Path.Combine(Path.GetTempPath(), $"SolarWebSettingsTests-{Guid.NewGuid():N}.xml");

    public void Dispose() => File.Delete(fileName);

    [Fact]
    public void A_clear_text_password_is_saved_encrypted_and_the_system_id_reads_back()
    {
        var settings = new Settings
        {
            SolarWeb = new SolarWebSettings { UserName = "you@example.com", ClearTextPassword = "correct horse battery staple", PvSystemId = PvSystemId, RefreshMinutes = 5, TimeZoneId = "Europe/Berlin" },
        };

        settings.Save(fileName);
        var xml = File.ReadAllText(fileName);
        var loaded = Settings.Load(fileName);

        Assert.DoesNotContain("correct horse battery staple", xml);
        Assert.DoesNotContain("ClearTextPassword", xml);
        Assert.Contains("<SolarWeb ", xml);
        Assert.Contains($"PvSystemId=\"{PvSystemId}\"", xml);
        Assert.Contains("RefreshMinutes=\"5\"", xml);

        Assert.NotNull(loaded.SolarWeb);
        Assert.Equal("you@example.com", loaded.SolarWeb.UserName);
        Assert.Equal("correct horse battery staple", loaded.SolarWeb.Password);
        Assert.Equal("https://www.solarweb.com", loaded.SolarWeb.BaseUrl);
        Assert.Equal(PvSystemId, loaded.SolarWeb.PvSystemId);
        Assert.Equal(5, loaded.SolarWeb.RefreshMinutes);
        Assert.Equal("Europe/Berlin", loaded.SolarWeb.TimeZoneId);
        Assert.Equal("Europe/Berlin", loaded.SolarWeb.ResolveTimeZone().Id);
        Assert.True(loaded.SolarWeb.IsConfigured);
    }

    [Fact]
    public void Defaults_are_left_out_and_come_back_as_defaults()
    {
        new Settings { SolarWeb = new SolarWebSettings() }.Save(fileName);
        var xml = File.ReadAllText(fileName);
        var loaded = Settings.Load(fileName);

        Assert.Contains("<SolarWeb ", xml);
        Assert.DoesNotContain("RefreshMinutes", xml);
        Assert.DoesNotContain("TimeZoneId", xml);
        Assert.DoesNotContain("PvSystemId", xml);
        Assert.NotNull(loaded.SolarWeb);
        Assert.Equal(15, loaded.SolarWeb.RefreshMinutes);
        Assert.Equal(TimeZoneInfo.Local.Id, loaded.SolarWeb.ResolveTimeZone().Id);
        Assert.False(loaded.SolarWeb.IsConfigured);
    }

    [Fact]
    public void A_file_without_the_element_has_none_and_a_system_without_a_user_is_not_configured()
    {
        new Settings().Save(fileName);
        Assert.Null(Settings.Load(fileName).SolarWeb);

        Assert.False(new SolarWebSettings { PvSystemId = PvSystemId }.IsConfigured);
        Assert.False(new SolarWebSettings { UserName = "you@example.com" }.IsConfigured);
    }
}
