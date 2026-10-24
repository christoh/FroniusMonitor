using System.Xml.Linq;
using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>The color part of an AHA device, written back as AHA wrote it.</summary>
public sealed class FritzBoxColorControlTests
{
    [Fact]
    public void Supported_and_current_mode_are_written_back_as_they_were_read()
    {
        var serializer = new XmlSerializer(typeof(FritzBoxDeviceList));
        using var reader = new StringReader(FritzBoxSmartHomeFixtures.DeviceList);
        var list = (FritzBoxDeviceList)serializer.Deserialize(reader)!;
        using var writer = new StringWriter();
        serializer.Serialize(writer, list);

        var read = XDocument.Parse(FritzBoxSmartHomeFixtures.DeviceList).Descendants("colorcontrol").Select(Modes);
        var written = XDocument.Parse(writer.ToString()).Descendants("colorcontrol").Select(Modes);

        Assert.Equal([("5", "4"), ("5", "1")], read);
        Assert.Equal(read, written);
    }

    [Theory]
    [InlineData(254d, 1d)]
    [InlineData(127d, 0.5d)]
    [InlineData(0d, 0d)]
    [InlineData(255d, 1d)]
    public void A_saturation_of_254_is_100_percent_and_255_set_another_way_is_no_more(double absolute, double percent)
    {
        IHsvColorControl lamp = new FritzBoxDevice { Color = new FritzBoxColorControl { SaturationAbsolute = absolute } };

        Assert.Equal(percent, lamp.Saturation);
    }

    private static (string?, string?) Modes(XElement color) => (color.Attribute("supported_modes")?.Value, color.Attribute("current_mode")?.Value);
}
