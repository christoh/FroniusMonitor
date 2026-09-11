using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The XML shape of the server's <c>&lt;ToshibaHvac&gt;</c> element: the connection's own attributes on the
/// element, the password encrypted on save, the device id kept, and a session that comes back as it went in.
/// </summary>
public sealed class ToshibaHvacSettingsTests : IDisposable
{
    private readonly string fileName = Path.Combine(Path.GetTempPath(), $"ToshibaHvacSettingsTests-{Guid.NewGuid():N}.xml");

    public void Dispose() => File.Delete(fileName);

    [Fact]
    public void A_clear_text_password_is_saved_encrypted_and_reads_back()
    {
        var settings = new Settings
        {
            ToshibaHvac = new ToshibaHvacSettings { UserName = "you@example.com", ClearTextPassword = "correct horse battery staple", AzureDeviceId = 4711 },
        };

        settings.Save(fileName);
        var xml = File.ReadAllText(fileName);
        var loaded = Settings.Load(fileName);

        Assert.DoesNotContain("correct horse battery staple", xml);
        Assert.DoesNotContain("ClearTextPassword", xml);
        Assert.Contains("<ToshibaHvac ", xml);
        Assert.DoesNotContain("<Connection", xml);
        Assert.Contains("AzureDeviceId=\"004711\"", xml);

        Assert.NotNull(loaded.ToshibaHvac);
        Assert.Equal("you@example.com", loaded.ToshibaHvac.UserName);
        Assert.Equal("correct horse battery staple", loaded.ToshibaHvac.Password);
        Assert.Equal("https://mobileapi.toshibahomeaccontrols.com", loaded.ToshibaHvac.BaseUrl);
        Assert.Equal(4711u, loaded.ToshibaHvac.AzureDeviceId);
        Assert.Equal(30, loaded.ToshibaHvac.MappingRefreshMinutes);
        Assert.Null(loaded.ToshibaHvac.Session);
    }

    [Fact]
    public void An_element_without_a_device_id_gets_a_six_digit_one_that_is_then_written_back()
    {
        File.WriteAllText(fileName, """
            <?xml version="1.0" encoding="utf-8"?>
            <Settings>
               <ToshibaHvac UserName="you@example.com" ClearTextPassword="x" />
            </Settings>
            """);

        var loaded = Settings.Load(fileName);
        var id = loaded.ToshibaHvac!.AzureDeviceIdString;
        loaded.Save(fileName);

        Assert.Equal(6, id.Length);
        Assert.All(id, c => Assert.True(char.IsAsciiDigit(c)));
        Assert.Contains($"AzureDeviceId=\"{id}\"", File.ReadAllText(fileName));
        Assert.Equal(id, Settings.Load(fileName).ToshibaHvac!.AzureDeviceIdString);
    }

    [Fact]
    public void The_session_survives_a_round_trip()
    {
        var consumerId = Guid.NewGuid();

        var settings = new Settings
        {
            ToshibaHvac = new ToshibaHvacSettings
            {
                UserName = "you@example.com",
                Session = new ToshibaHvacSession { ConsumerId = consumerId, AccessToken = "the-token", TokenType = "Bearer", ConsumerMasterId = "master", CountryId = 49 },
                SessionTime = new DateTime(2026, 9, 12, 8, 0, 0, DateTimeKind.Utc),
            },
        };

        settings.Save(fileName);
        var loaded = Settings.Load(fileName);

        Assert.NotNull(loaded.ToshibaHvac?.Session);
        Assert.Equal(consumerId, loaded.ToshibaHvac.Session.ConsumerId);
        Assert.Equal("the-token", loaded.ToshibaHvac.Session.AccessToken);
        Assert.Equal("Bearer", loaded.ToshibaHvac.Session.TokenType);
        Assert.Equal(49, loaded.ToshibaHvac.Session.CountryId);
        Assert.Equal(settings.ToshibaHvac.SessionTime, loaded.ToshibaHvac.SessionTime.ToUniversalTime());
    }
}
