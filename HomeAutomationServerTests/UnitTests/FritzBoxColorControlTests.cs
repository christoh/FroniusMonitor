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

    private static (string?, string?) Modes(XElement color) => (color.Attribute("supported_modes")?.Value, color.Attribute("current_mode")?.Value);
}
