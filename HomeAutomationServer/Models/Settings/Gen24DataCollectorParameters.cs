using System.ComponentModel;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class Gen24DataCollectorParameters : PolledWebConnectionParameterBase
{
    [XmlAttribute, DefaultValue(typeof(TimeSpan), "0:5:0")]
    public TimeSpan ConfigRefreshRate { get; set; } = TimeSpan.FromMinutes(5);

    [XmlAttribute, DefaultValue("/var/log")]
    public string LogDirectory { get; set; } = "/var/log";
}
