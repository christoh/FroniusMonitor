using De.Hochstaetter.Fronius.Services.Modbus;
using Microsoft.Extensions.Logging;

namespace FroniusUnitTests.SystemTests;

[TestFixture]
public class SunSpecClientTests
{
    private readonly ILogger<SunSpecClient> logger = Substitute.For<ILogger<SunSpecClient>>();
    private readonly SunSpecClient client;

    public SunSpecClientTests()
    {
        client = new(logger);
    }

    [Test]
    public async Task Inverter_Success()
    {
        await client.ConnectAsync("192.168.44.10", 502, 1).ConfigureAwait(false);
        var device = await client.GetDataAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task Meter_Success()
    {
        await client.ConnectAsync("192.168.44.10", 502, 200).ConfigureAwait(false);
        var device = await client.GetDataAsync().ConfigureAwait(false);
    }
}