using System.ComponentModel;
using System.Xml.Serialization;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

public class WebServerSettings
{
    [XmlArrayItem("Url")]
    [DefaultValue(null)]
    public string[]? Urls { get; set; }

    /// <summary>
    /// The reverse proxies whose <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> the server believes: single
    /// addresses (<c>192.168.44.1</c>) or networks (<c>192.168.44.0/24</c>), as the server sees them connect - which
    /// is the address every "from {Ip}" in the log shows while the proxy is not in here. The loopback addresses are
    /// trusted without being listed. See <see cref="Misc.ReverseProxies"/>.
    /// </summary>
    [XmlArrayItem("Proxy")]
    [DefaultValue(null)]
    public string[]? TrustedProxies { get; set; }
}
