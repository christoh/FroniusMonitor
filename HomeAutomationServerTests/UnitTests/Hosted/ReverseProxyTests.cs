using System.Net;
using De.Hochstaetter.HomeAutomationServer.Misc;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

// Every call here goes to a loopback host and completes within a few milliseconds; a test that cannot be cancelled
// for that long costs nothing, and the tokens would only bury the assertions.
#pragma warning disable xUnit1051

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Hosted;

/// <summary>
/// Whom the server believes the forwarded headers of, over a real Kestrel host that answers with the client address
/// and the scheme it sees - what every "from {Ip}" in the log is made of.
/// </summary>
public sealed class ReverseProxyTests
{
    private const string Client = "203.0.113.7";

    [Fact]
    public async Task Behind_a_trusted_proxy_the_server_sees_the_client_and_https()
    {
        var seen = await Request(RemovingLoopbackDefaults(ReverseProxies.CreateOptions(["127.0.0.1"], NullLogger.Instance)));

        Assert.Equal($"{Client}|https", seen);
    }

    /// <summary>
    /// In the container Kestrel listens on every address, dual stack, so an IPv4 proxy connects as an IPv4 address
    /// mapped to IPv6 (<c>::ffff:192.168.44.1</c>). Listing it the way everybody writes it has to be enough. Not over
    /// a socket: the machines the tests run on need not have IPv6 at all, so the real middleware is handed a request
    /// from that address directly.
    /// </summary>
    [Theory]
    [InlineData("192.168.44.1")]
    [InlineData("192.168.44.0/24")]
    public async Task An_IPv4_proxy_is_recognised_when_it_connects_to_a_dual_stack_listener(string trusted)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.44.1").MapToIPv6();
        context.Request.Headers["X-Forwarded-For"] = Client;
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(ReverseProxies.CreateOptions([trusted], NullLogger.Instance)));
        await middleware.Invoke(context);

        Assert.Equal(IPAddress.Parse(Client), context.Connection.RemoteIpAddress);
        Assert.Equal("https", context.Request.Scheme);
    }

    [Fact]
    public async Task A_trusted_network_is_as_good_as_a_trusted_address()
    {
        var seen = await Request(RemovingLoopbackDefaults(ReverseProxies.CreateOptions(["127.0.0.0/30"], NullLogger.Instance)));

        Assert.Equal($"{Client}|https", seen);
    }

    [Fact]
    public async Task A_sender_that_is_not_trusted_cannot_choose_the_address_it_is_logged_under()
    {
        var seen = await Request(RemovingLoopbackDefaults(ReverseProxies.CreateOptions(["192.168.44.1"], NullLogger.Instance)));

        Assert.Equal("127.0.0.1|http", seen);
    }

    [Fact]
    public async Task Loopback_is_trusted_without_being_listed()
    {
        var seen = await Request(ReverseProxies.CreateOptions(null, NullLogger.Instance));

        Assert.Equal($"{Client}|https", seen);
    }

    [Fact]
    public void An_entry_that_is_neither_an_address_nor_a_network_is_logged_and_left_out()
    {
        var logger = new RecordingLogger();

        var options = ReverseProxies.CreateOptions(["192.168.44.1", " 10.0.0.0/8 ", "nginx", "", "fd00::1"], logger);

        Assert.Contains(IPAddress.Parse("192.168.44.1"), options.KnownProxies);
        Assert.Contains(IPAddress.Parse("fd00::1"), options.KnownProxies);
        Assert.Contains(System.Net.IPNetwork.Parse("10.0.0.0/8"), options.KnownIPNetworks);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("nginx"));
    }

    [Fact]
    public void The_trusted_proxies_round_trip_through_the_settings_file()
    {
        var xml = SettingsXml.Serialize(new Settings { WebServerSettings = new WebServerSettings { TrustedProxies = ["192.168.44.1", "10.0.0.0/8"] } });

        Assert.Contains("<Proxy>192.168.44.1</Proxy>", xml);
        Assert.Equal(["192.168.44.1", "10.0.0.0/8"], SettingsXml.Deserialize(xml).WebServerSettings.TrustedProxies);
        Assert.DoesNotContain("TrustedProxies", SettingsXml.Serialize(new Settings()));
    }

    /// <summary>
    /// What <see cref="ReverseProxies.CreateOptions"/> adds, without the loopback ASP.NET trusts by default, so that
    /// a test on loopback can tell a trusted proxy from one that is not.
    /// </summary>
    private static ForwardedHeadersOptions RemovingLoopbackDefaults(ForwardedHeadersOptions options)
    {
        options.KnownProxies.Remove(IPAddress.IPv6Loopback);
        options.KnownIPNetworks.Remove(new System.Net.IPNetwork(IPAddress.Parse("127.0.0.0"), 8));
        return options;
    }

    /// <summary>Sends a request as nginx does - the client in <c>X-Forwarded-For</c>, <c>https</c> in <c>X-Forwarded-Proto</c> - and answers what the server saw.</summary>
    private static async Task<string> Request(ForwardedHeadersOptions options)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddTestOutput(LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        await using var app = builder.Build();
        app.UseForwardedHeaders(options);
        app.MapGet("/who", (HttpContext context) => $"{context.Connection.RemoteIpAddress}|{context.Request.Scheme}");
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, address + "/who");
        request.Headers.Add("X-Forwarded-For", Client);
        request.Headers.Add("X-Forwarded-Proto", "https");

        using var response = await http.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
