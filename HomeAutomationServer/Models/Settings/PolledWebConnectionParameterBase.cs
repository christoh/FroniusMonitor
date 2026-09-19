using System.ComponentModel;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public abstract class PolledWebConnectionParameterBase
{
    /// <summary>
    /// Not settable through Settings.xml itself - an interface type is not something <see cref="XmlSerializer"/>
    /// can build back up. Where a derived type is configurable there, its connections come from a plain
    /// <c>List&lt;WebConnection&gt;</c> element of <see cref="Settings"/> instead, the way <see cref="Settings.Gen24Connections"/> does.
    /// </summary>
    [XmlIgnore]
    public IEnumerable<WebConnection>? Connections { get; set; }

    [XmlAttribute, DefaultValue(typeof(TimeSpan), "0:0:5")]
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromSeconds(5);
}
