using De.Hochstaetter.Fronius.Models.Modbus;

namespace FroniusUnitTests.SystemTests;

[TestFixture]
public class SunSpecClientTests
{
    //private readonly ILogger<SunSpecClient> logger;
    private readonly SunSpecClient client;

    public SunSpecClientTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
            .Verbose()
            .WriteTo
            .Debug(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        var services = new ServiceCollection()
                .AddSingleton<SunSpecClient>()
                .AddLogging(builder => builder.AddSerilog())
            ;

        var provider = services.BuildServiceProvider();
        client = provider.GetRequiredService<SunSpecClient>();
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
        var sunSpecMeter = new SunSpecMeter(device);
    }
}
