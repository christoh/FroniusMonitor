using System.Xml.Serialization;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary><see cref="Settings"/> to and from the XML of Settings.xml, with the serializer the server uses, but in memory.</summary>
internal static class SettingsXml
{
    public static string Serialize(Settings settings)
    {
        using var writer = new StringWriter();
        new XmlSerializer(typeof(Settings)).Serialize(writer, settings);
        return writer.ToString();
    }

    public static Settings Deserialize(string xml)
    {
        using var reader = new StringReader(xml);
        return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(reader)!;
    }
}
