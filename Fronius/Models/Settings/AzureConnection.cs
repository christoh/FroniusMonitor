using Microsoft.Azure.Devices.Client;

namespace De.Hochstaetter.Fronius.Models.Settings;

public enum Protocol : byte
{
    Amqp,
    Mqtt,
    Http1,
}

public enum TunnelMode : byte
{
    Auto,
    Websocket,
    NoTunnel,
}

public class AzureConnection : WebConnection
{
    private TunnelMode tunnelMode = TunnelMode.Auto;

    [XmlAttribute("TunnelMode"), DefaultValue(TunnelMode.Auto)]
    public TunnelMode TunnelMode
    {
        get => tunnelMode;
        set => Set(ref tunnelMode, value,NotifyProtocols);
    }

    private Protocol protocol = Protocol.Amqp;

    [XmlAttribute("Protocol"), DefaultValue(Protocol.Amqp)]
    public Protocol Protocol
    {
        get => protocol;
        set => Set(ref protocol, value, NotifyProtocols);
    }

    public bool CanUseTunnel => Protocol != Protocol.Http1;

    public TransportType TransportType => Protocol switch
    {
        Protocol.Http1 => TransportType.Http1,

        Protocol.Amqp => TunnelMode switch
        {
            TunnelMode.Auto => TransportType.Amqp,
            TunnelMode.Websocket => TransportType.Amqp_WebSocket_Only,
            TunnelMode.NoTunnel => TransportType.Amqp_Tcp_Only,
            _ => throw new NotSupportedException(nameof(TunnelMode))
        },

        Protocol.Mqtt => TunnelMode switch
        {
            TunnelMode.Auto => TransportType.Mqtt,
            TunnelMode.Websocket => TransportType.Mqtt_WebSocket_Only,
            TunnelMode.NoTunnel => TransportType.Mqtt_Tcp_Only,
            _ => throw new NotSupportedException(nameof(TunnelMode))
        },

        _ => throw new NotSupportedException(nameof(Protocol))
    };

    private void NotifyProtocols()
    {
        NotifyOfPropertyChange(nameof(TransportType));
        NotifyOfPropertyChange(nameof(Protocol));
    }
}
