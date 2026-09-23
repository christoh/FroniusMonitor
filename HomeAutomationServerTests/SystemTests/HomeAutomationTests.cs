using System.Net;
using De.Hochstaetter.HomeAutomationClient.Services;

namespace De.Hochstaetter.HomeAutomationServerTests.SystemTests;

/// <summary>
/// The deployed server's web API, answered over the internet rather than by a test host.
/// </summary>
public sealed class HomeAutomationTests
{
    /// <summary>
    /// Logs in the way the client does, with a bearer token: Basic credentials are refused by a server that has
    /// not switched them on for debugging.
    /// </summary>
    [SystemFact]
    public async Task The_deployed_server_lists_its_devices()
    {
        using var client = new WebClientService();
        client.Initialize("https://home.hochstaetter.de/api/", "test", "1.0");

        var login = await client.Login("TestUser", "TestPassword", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.Status);

        var devices = await client.ListDevices(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, devices.Status);
        Assert.NotNull(devices.Payload);
    }
}
