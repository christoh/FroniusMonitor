using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace De.Hochstaetter.HomeAutomationServer.Misc;

/// <summary>
/// Makes the server see the client behind a reverse proxy - nginx in front of the container - instead of the proxy:
/// its address in <c>HttpContext.Connection.RemoteIpAddress</c>, which every "from {Ip}" in the log shows, and the
/// scheme it used.
/// </summary>
/// <remarks>
/// Only proxies in <see cref="WebServerSettings.TrustedProxies"/> (and loopback) are believed. Anybody else may put
/// an <c>X-Forwarded-For</c> into their request, and the server port is reachable on the LAN without the proxy, so
/// believing every sender would let any machine there choose the address it is logged under.
/// </remarks>
public static class ReverseProxies
{
    /// <summary>
    /// The options for <c>UseForwardedHeaders</c>: <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> from the
    /// trusted proxies, one hop deep - the proxy's own entry, the one it appends with
    /// <c>$proxy_add_x_forwarded_for</c>. Entries that are neither an address nor a network are logged and left out.
    /// </summary>
    public static ForwardedHeadersOptions CreateOptions(IEnumerable<string>? trustedProxies, ILogger logger)
    {
        var options = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };

        foreach (var entry in (trustedProxies ?? []).Select(entry => entry.Trim()).Where(entry => entry.Length > 0))
        {
            if (entry.Contains('/') && System.Net.IPNetwork.TryParse(entry, out var network))
            {
                options.KnownIPNetworks.Add(network);
            }
            else if (IPAddress.TryParse(entry, out var address))
            {
                options.KnownProxies.Add(address);
            }
            else if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("{Entry} in WebServerSettings/TrustedProxies of {FileName} is neither an IP address nor a network and is ignored", entry, Settings.SettingsFileName);
            }
        }

        var trusted = options.KnownProxies.Select(address => address.ToString()).Concat(options.KnownIPNetworks.Select(network => network.ToString())).ToList();

        if (trusted.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation
                (
                    "No reverse proxy is trusted: the log shows the address a request comes from, the proxy's where there is one. Add it to WebServerSettings/TrustedProxies in {FileName} to see the client's instead",
                    Settings.SettingsFileName
                );
            }
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("X-Forwarded-For and X-Forwarded-Proto are believed from {Proxies} and from loopback", string.Join(", ", trusted));
        }

        return options;
    }
}
