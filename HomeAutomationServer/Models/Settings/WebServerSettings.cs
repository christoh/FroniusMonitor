using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class WebServerSettings
{
    [XmlArrayItem("Url")]
    [DefaultValue(null)]
    public string[]? Urls { get; set; }
}