using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace FroniusUnitTests.SystemTests;

[TestFixture]
public class HomeAutomationTests
{
    [Test]
    public async Task Test1()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://home.hochstaetter.de");
        var authString = Convert.ToBase64String("TestUser:TestPassword"u8);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var x = await client.GetFromJsonAsync<IDictionary<string, DeviceInfo>>("/api/devices");
    }
}
