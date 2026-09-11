using System.ComponentModel;
using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
///     The Toshiba account of this server. One account covers every air conditioner registered with it, which is why
///     this is a single element and not a list like the other devices.
/// </summary>
/// <remarks>
///     <para>
///         The element <em>is</em> the connection: an <see cref="AzureConnection" /> (a <see cref="WebConnection" />
///         with the IoT Hub transport), so <c>BaseUrl</c>, <c>UserName</c> and the password are its attributes, and
///         the usual password handling applies - write <c>ClearTextPassword="..."</c> once, and the server's save
///         replaces it with the encrypted <c>Password</c> and its checksum.
///     </para>
///     <para>
///         <see cref="AzureDeviceId" /> is drawn at random when the element has none and is written back by the
///         server's save at start-up; from then on it stays, because the Toshiba service knows this installation by
///         it. <see cref="Session" /> is the bearer token of the last login. It lasts for months and is used as long
///         as the service accepts it, so that the account is not logged into again and again - the service does not
///         like that. It is stored in clear text for now.
///     </para>
/// </remarks>
public class ToshibaHvacSettings : AzureConnection
{
    public ToshibaHvacSettings()
    {
        BaseUrl = "https://mobileapi.toshibahomeaccontrols.com";
        Protocol = Protocol.Amqp;
        TunnelMode = TunnelMode.Auto;
    }

    [XmlIgnore]
    public uint AzureDeviceId { get; set; } = ToshibaHvacAzureDeviceId.CreateRandom();

    [XmlAttribute(nameof(AzureDeviceId))]
    public string AzureDeviceIdString
    {
        get => ToshibaHvacAzureDeviceId.ToString(AzureDeviceId);
        set => AzureDeviceId = ToshibaHvacAzureDeviceId.Parse(value, IoC.TryGetRegistered<ILogger<ToshibaHvacSettings>>());
    }

    /// <summary>How often the device list is read over HTTPS, to make good what message queuing lost.</summary>
    [XmlAttribute, DefaultValue(30)]
    public int MappingRefreshMinutes { get; set; } = 30;

    [XmlElement, DefaultValue(null)]
    public ToshibaHvacSession? Session { get; set; }

    /// <summary>When <see cref="Session" /> was obtained, UTC.</summary>
    [XmlAttribute, DefaultValue(typeof(DateTime), "0001-01-01T00:00:00")]
    public DateTime SessionTime { get; set; }
}
