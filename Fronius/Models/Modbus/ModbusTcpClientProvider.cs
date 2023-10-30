using FluentModbus;

namespace De.Hochstaetter.Fronius.Models.Modbus;

internal sealed class ModbusTcpClientProvider : ITcpClientProvider
{
    private TcpListener? listener;

    public ModbusTcpClientProvider(IPEndPoint endPoint, bool ipv6Only = false)
    {
        listener = new TcpListener(endPoint);
        listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, ipv6Only);
        listener.Start();
    }

    public void Dispose()
    {
        listener?.Stop();
        listener = null;
    }

    public Task<TcpClient> AcceptTcpClientAsync()
    {
        return listener?.AcceptTcpClientAsync() ?? throw new ObjectDisposedException($"{GetType().Name} was disposed");
    }
}