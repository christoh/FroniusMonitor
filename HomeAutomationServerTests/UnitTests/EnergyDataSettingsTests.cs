using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The EnergyData element of Settings.xml as the server writes and reads it: attributes, defaults that are left
/// out, and a file without the element at all.
/// </summary>
public sealed class EnergyDataSettingsTests
{
    [Fact]
    public void The_section_round_trips_as_attributes()
    {
        var settings = new Settings
        {
            EnergyData = new EnergyDataSettings
            {
                Bearer = "token",
                PostalCode = "85375",
                GridOperatorId = "9901068000001",
                SurchargeCentsPerKiloWattHour = 1.25m,
                VatRatePercent = 20,
                PriceRegion = AwattarCountry.Austria,
                DwdStationId = "10870",
                TimeZoneId = "Europe/Vienna",
            },
        };

        var xml = Serialize(settings);
        var back = Deserialize(xml);

        Assert.Contains("<EnergyData ", xml);
        Assert.Contains("Bearer=\"token\"", xml);
        Assert.Contains("SurchargeCentsPerKiloWattHour=\"1.25\"", xml);
        Assert.Contains("PriceRegion=\"Austria\"", xml);
        Assert.NotNull(back.EnergyData);
        Assert.Equal("token", back.EnergyData.Bearer);
        Assert.Equal("85375", back.EnergyData.PostalCode);
        Assert.Equal("9901068000001", back.EnergyData.GridOperatorId);
        Assert.Equal(1.25m, back.EnergyData.SurchargeCentsPerKiloWattHour);
        Assert.Equal(0.2m, back.EnergyData.VatRate);
        Assert.Equal(AwattarCountry.Austria, back.EnergyData.PriceRegion);
        Assert.Equal("10870", back.EnergyData.DwdStationId);
        Assert.Equal("Europe/Vienna", back.EnergyData.TimeZoneId);
        Assert.True(back.EnergyData.HasTariffQuery);
    }

    [Fact]
    public void Defaults_are_left_out_and_come_back_as_defaults()
    {
        var xml = Serialize(new Settings { EnergyData = new EnergyDataSettings() });
        var back = Deserialize(xml);

        Assert.Contains("<EnergyData", xml);
        Assert.DoesNotContain("SurchargeCentsPerKiloWattHour", xml);
        Assert.DoesNotContain("VatRatePercent", xml);
        // The attribute of EnergyData, not anything with the word in it: the Authentication element has a
        // BearerTokenLifetimeMinutes of its own, which is always written.
        Assert.DoesNotContain(" Bearer=", xml);
        Assert.NotNull(back.EnergyData);
        Assert.Equal(1.5m, back.EnergyData.SurchargeCentsPerKiloWattHour);
        Assert.Equal(0.19m, back.EnergyData.VatRate);
        Assert.Equal(AwattarCountry.GermanyLuxembourg, back.EnergyData.PriceRegion);
        Assert.False(back.EnergyData.HasTariffQuery);
        Assert.Equal(TimeZoneInfo.Local.Id, back.EnergyData.ResolveTimeZone().Id);
    }

    [Fact]
    public void A_file_without_the_section_has_none_and_an_unknown_zone_falls_back_to_the_local_one()
    {
        var back = Deserialize(Serialize(new Settings()));
        Assert.Null(back.EnergyData);

        var settings = new EnergyDataSettings { TimeZoneId = "Mars/Olympus_Mons" };
        Assert.Equal(TimeZoneInfo.Local.Id, settings.ResolveTimeZone().Id);
        Assert.Equal("Europe/Berlin", new EnergyDataSettings { TimeZoneId = "Europe/Berlin" }.ResolveTimeZone().Id);
    }

    private static string Serialize(Settings settings)
    {
        using var writer = new StringWriter();
        new XmlSerializer(typeof(Settings)).Serialize(writer, settings);
        return writer.ToString();
    }

    private static Settings Deserialize(string xml)
    {
        using var reader = new StringReader(xml);
        return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(reader)!;
    }
}
