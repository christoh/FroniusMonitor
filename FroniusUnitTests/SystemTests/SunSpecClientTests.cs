namespace FroniusUnitTests.SystemTests;

using Assert = NUnit.Framework.Legacy.ClassicAssert;

[TestFixture]
public class SunSpecClientTests
{
    private readonly ISunSpecClient client;

    public SunSpecClientTests()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithComputed("SourceContextName", "Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)")
            .WriteTo.Debug(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] ({SourceContextName:l}) {Message:lj}{NewLine}{Exception}", formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        var services = new ServiceCollection()
            .AddSingleton<ISunSpecClient, SunSpecClient>()
            .AddLogging(builder => builder.AddSerilog());

        var provider = services.BuildServiceProvider();
        client = provider.GetRequiredService<ISunSpecClient>();
    }

    [Test]
    public async Task Inverter_Success()
    {
        await client.ConnectAsync("192.168.44.10", 502, 1).ConfigureAwait(false);
        var inverter = new SunSpecInverter(await client.GetDataAsync().ConfigureAwait(false));

        Assert.AreEqual("Fronius", inverter.Manufacturer);
        Assert.LessOrEqual(inverter.InverterBaseSensors?.PowerFactorTotal??0, 1);
        Assert.GreaterOrEqual(inverter.InverterBaseSensors?.PowerFactorTotal ?? 0, -1);
        Assert.AreEqual((ushort)0xffff, inverter.BasicSettings?.ConnectedPhaseI);
        Assert.IsNull(inverter.NamePlate?.AmpereHoursCapacity);
        Assert.AreEqual(4, inverter.Tracker?.NumberOfTrackers);
        Assert.GreaterOrEqual(inverter.ExtendedSensors?.IsolationResistance ?? 0, 100000);
        Assert.AreEqual(SunSpecOnOff.Disabled, inverter.ExtendedSettings?.ActivePowerLimitEnabled);
        Assert.AreEqual(inverter.ExtendedSettings?.RelativeActivePowerLimit ?? 0, 1);
        Assert.AreEqual(inverter.ExtendedSettings?.ActivePowerLimitEnabled ?? 0, SunSpecOnOff.Disabled);
        Assert.AreEqual(SunSpecChargingLimits.None,inverter.StorageSettings?.ChargingLimits);

        if (inverter.StorageSettings == null)
        {
            Assert.IsNotNull(inverter.StorageSettings);
            return;
        }

        var storageSettings = inverter.StorageSettings;
        storageSettings.RelativeOutgoingActivePowerMax = 0;
        storageSettings.RelativeIncomingActivePowerMax = 0;
        storageSettings.ChargingLimits = SunSpecChargingLimits.Charging | SunSpecChargingLimits.Discharging;

        await client.WriteRegisters
        (
            storageSettings,
            default,
            nameof(SunSpecStorageBaseSettings.RelativeOutgoingActivePowerMaxI),
            nameof(SunSpecStorageBaseSettings.RelativeIncomingActivePowerMaxI),
            nameof(SunSpecStorageBaseSettings.ChargingLimits)
        ).ConfigureAwait(false);

        storageSettings.RelativeOutgoingActivePowerMax = 1;
        storageSettings.RelativeIncomingActivePowerMax = 1;
        storageSettings.ChargingLimits = SunSpecChargingLimits.None;

        await client.WriteRegisters
        (
            storageSettings,
            default,
            nameof(SunSpecStorageBaseSettings.RelativeOutgoingActivePowerMaxI),
            nameof(SunSpecStorageBaseSettings.RelativeIncomingActivePowerMaxI),
            nameof(SunSpecStorageBaseSettings.ChargingLimits)
        ).ConfigureAwait(false);
    }

    [Test]
    public async Task Meter_Success()
    {
        await client.ConnectAsync("192.168.44.10", 502, 200).ConfigureAwait(false);
        var device = await client.GetDataAsync().ConfigureAwait(false);
        var sunSpecMeter = new SunSpecMeter(device);
        Assert.IsNotNull(sunSpecMeter);
    }
}
