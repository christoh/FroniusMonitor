using System.ComponentModel;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class WebServerSettings
{
    [XmlArrayItem("Url")]
    [DefaultValue(null)]
    public string[]? Urls { get; set; }
}
