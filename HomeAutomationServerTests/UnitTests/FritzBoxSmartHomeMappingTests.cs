using System.Xml.Serialization;
using De.Hochstaetter.FritzBox.SmartHome;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The REST overview of the FritzBox library mapped onto <see cref="FritzBoxDevice"/>, held against what AHA delivers
/// for the same moment of the same box: the server and its clients must not see a difference.
/// </summary>
public sealed class FritzBoxSmartHomeMappingTests
{
    /// <summary>What only AHA knows: the REST overview says nothing about the microphone of an outlet.</summary>
    private const FritzBoxFeatures AhaOnly = FritzBoxFeatures.Microphone;

    private static readonly XmlSerializer ahaSerializer = new(typeof(FritzBoxDeviceList));

    [Fact]
    public void There_is_one_device_per_unit_under_the_AIN_AHA_switches_it_by()
    {
        var ahaUnits = Aha().Devices.Where(d => d.Features != FritzBoxFeatures.HanFunDevice).Select(d => d.Ain).Order();

        Assert.Equal(ahaUnits, Rest().Devices.Select(d => d.Ain).Order());
        Assert.Contains("13077 0000003-1", ahaUnits);
    }

    [Theory]
    [InlineData("08761 0000001")]
    [InlineData("11630 0000002")]
    [InlineData("13077 0000003-1")]
    [InlineData("13077 0000004-1")]
    [InlineData("08761 0000005")]
    public void A_unit_reads_the_same_as_over_AHA(string ain)
    {
        var rest = Rest().Devices.Single(d => d.Ain == ain);
        var aha = Aha().Devices.Single(d => d.Ain == ain);

        Assert.Equal(aha.DisplayName, rest.DisplayName);
        Assert.Equal(aha.Manufacturer, rest.Manufacturer);
        Assert.Equal(aha.IsPresent, rest.IsPresent);
        Assert.Equal(aha.Features & ~AhaOnly, rest.Features);

        Assert.Equal(aha.SimpleSwitch?.IsTurnedOn, rest.SimpleSwitch?.IsTurnedOn);
        Assert.Equal(aha.Switch is null, rest.Switch is null);
        Assert.Equal(aha.Switch?.IsTurnedOn, rest.Switch?.IsTurnedOn);
        Assert.Equal(aha.Switch?.IsUiLocked, rest.Switch?.IsUiLocked);
        Assert.Equal(aha.Switch?.IsDeviceLocked, rest.Switch?.IsDeviceLocked);

        Assert.Equal(aha.LevelControl?.Level, rest.LevelControl?.Level);
        Assert.Equal(aha.LevelControl?.LevelAbsolute, rest.LevelControl?.LevelAbsolute);

        Assert.Equal(aha.Color?.SupportedModes, rest.Color?.SupportedModes);
        Assert.Equal(aha.Color?.CurrentMode, rest.Color?.CurrentMode);
        Assert.Equal(aha.Color?.HueDegrees, rest.Color?.HueDegrees);
        Assert.Equal(aha.Color?.SaturationAbsolute, rest.Color?.SaturationAbsolute);
        Assert.Equal(aha.Color?.TemperatureKelvin, rest.Color?.TemperatureKelvin);

        Assert.Equal(aha.PowerMeter?.PowerWatts, rest.PowerMeter?.PowerWatts);
        Assert.Equal(aha.PowerMeter?.Voltage, rest.PowerMeter?.Voltage);
        Assert.Equal(aha.PowerMeter?.EnergyConsumed, rest.PowerMeter?.EnergyConsumed);

        Assert.Equal(aha.TemperatureSensor?.Temperature, rest.TemperatureSensor?.Temperature);
    }

    [Fact]
    public void The_values_arrive_in_the_units_of_the_model()
    {
        var outlet = Rest().Devices.Single(d => d.Ain == "11630 0000002");
        var lamp = Rest().Devices.Single(d => d.Ain == "13077 0000003-1");

        Assert.Equal(228.38, outlet.PowerMeter?.PowerWatts);
        Assert.Equal(231.867, outlet.PowerMeter?.Voltage);
        Assert.Equal(1208122, outlet.PowerMeter?.EnergyConsumed);
        Assert.Equal(24.5, outlet.TemperatureSensor?.Temperature);
        Assert.Equal(true, outlet.Switch?.IsUiLocked);
        Assert.False(((ISwitchable)outlet).IsSwitchingEnabled);

        Assert.Equal(1, lamp.LevelControl?.Level);
        Assert.Equal(3000, lamp.Color?.TemperatureKelvin);
        Assert.True(((IColorTemperatureControl)lamp).IsColorTemperatureActive);
        Assert.True(((ISwitchable)lamp).IsSwitchingEnabled);
    }

    [Fact]
    public void A_lamp_takes_model_and_firmware_from_its_device_where_AHA_leaves_them_out_of_the_unit()
    {
        var aha = Aha().Devices.Single(d => d.Ain == "13077 0000004-1");
        var rest = Rest().Devices.Single(d => d.Ain == "13077 0000004-1");

        Assert.Equal((string.Empty, "0.0"), (aha.Model, aha.FirmwareVersionString));
        Assert.Equal(("FRITZ!Smart Light 500", "34.10.16.16.015"), (rest.Model, rest.FirmwareVersionString));
    }

    [Fact]
    public void A_device_out_of_radio_range_has_no_values_and_cannot_be_switched()
    {
        var outlet = Rest().Devices.Single(d => d.Ain == "08761 0000005");

        Assert.False(outlet.IsPresent);
        Assert.Null(((ISwitchable)outlet).IsTurnedOn);
        Assert.Null(outlet.PowerMeter?.PowerWatts);
        Assert.Null(outlet.TemperatureSensor?.Temperature);
        Assert.False(((ISwitchable)outlet).IsSwitchingEnabled);
    }

    [Fact]
    public void The_id_is_the_same_at_every_poll_and_differs_between_units()
    {
        var first = Rest().Devices.Select(d => d.Id).ToList();

        Assert.Equal(first, Rest().Devices.Select(d => d.Id));
        Assert.Equal(first.Count, first.Distinct().Count());
        Assert.Equal(0xE40C292Cu, SmartHomeMapper.GetId("a")); // the published FNV-1a value
    }

    [Fact]
    public void Group_units_are_left_out_as_AHA_groups_are()
    {
        var overview = new SmartHomeOverview
        {
            Units =
            [
                new SmartHomeUnit { Uid = "grp1", Ain = "grp1", IsGroupUnit = true, Interfaces = new SmartHomeInterfaces { OnOffInterface = new SmartHomeOnOff { State = "valid", Active = true } } },
                new SmartHomeUnit { Uid = "11630 0000009", Ain = "11630 0000009", IsConnected = true },
            ],
        };

        Assert.Equal("11630 0000009", Assert.Single(SmartHomeMapper.ToDeviceList(overview).Devices).Ain);
    }

    [Fact]
    public void Templates_and_routines_come_with_the_overview()
    {
        var overview = SmartHomeClient.Parse(FritzBoxSmartHomeFixtures.Overview);

        Assert.Equal(["Template 1", "Template 2"], overview.Templates.Select(t => t.Name));
        Assert.Equal("tmp000000-418B80CE0", overview.Templates[0].Uid);
        var routine = Assert.Single(overview.Triggers);
        Assert.Equal(("trg000000-419524153", "Routine 1", true), (routine.Uid, routine.Name, routine.Enabled));
    }

    private static FritzBoxDeviceList Rest() => SmartHomeMapper.ToDeviceList(SmartHomeClient.Parse(FritzBoxSmartHomeFixtures.Overview));

    private static FritzBoxDeviceList Aha()
    {
        using var reader = new StringReader(FritzBoxSmartHomeFixtures.DeviceList);
        return (FritzBoxDeviceList)ahaSerializer.Deserialize(reader)!;
    }
}
