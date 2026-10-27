using System.Net.NetworkInformation;

namespace De.Hochstaetter.HomeAutomationServer.Services.Ipv4Watchdog;

/// <summary>
/// <see cref="IPinger"/> with <see cref="Ping"/>. The server's container runs as an unprivileged user, so on Linux
/// .NET needs unprivileged ICMP sockets, which <c>net.ipv4.ping_group_range</c> of the host has to allow; where it
/// does not, <see cref="Ping"/> throws a <see cref="PingException"/>, which is passed on rather than read as
/// "no answer".
/// </summary>
public sealed class IcmpPinger : IPinger
{
    public async Task<bool> PingAsync(IPAddress address, TimeSpan timeout, CancellationToken token = default)
    {
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(address, timeout, cancellationToken: token).ConfigureAwait(false);
        return reply.Status == IPStatus.Success;
    }
}
