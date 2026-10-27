namespace De.Hochstaetter.HomeAutomationServer.Contracts;

/// <summary>Sends an ICMP echo request, so that a test can decide who answers.</summary>
public interface IPinger
{
    /// <summary>Whether <paramref name="address"/> answered one echo request within <paramref name="timeout"/>.</summary>
    /// <exception cref="System.Net.NetworkInformation.PingException">This host cannot ping at all, for example for lack of rights.</exception>
    Task<bool> PingAsync(IPAddress address, TimeSpan timeout, CancellationToken token = default);
}
