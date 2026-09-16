using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace De.Hochstaetter.HomeAutomationServerTests.SystemTests;

/// <summary>
/// The deployed server's web API, answered over the internet rather than by a test host.
/// </summary>
public sealed class HomeAutomationTests
{
    [SystemFact]
    public async Task The_deployed_server_lists_its_devices()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://home.hochstaetter.de");
        var authString = Convert.ToBase64String("TestUser:TestPassword"u8);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var devices = await client.GetFromJsonAsync<IDictionary<string, DeviceInfo>>("/api/devices");

        Assert.NotNull(devices);
    }
}
