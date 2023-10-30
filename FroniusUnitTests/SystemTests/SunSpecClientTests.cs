﻿using System.Linq;
using De.Hochstaetter.Fronius.Contracts.Modbus;
using De.Hochstaetter.Fronius.Models.Modbus;

namespace FroniusUnitTests.SystemTests;

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
        var device = await client.GetDataAsync().ConfigureAwait(false);
        var commonBlock = device.OfType<SunSpecCommonBlock>().Single();
        var inverter = device.OfType<ISunSpecInverter>().Single();
        var namePlate = device.OfType<SunSpecNamePlate>().Single();
        var basicSettings = device.OfType<SunSpecInverterBasicSettings>().Single();
        var multipleMppts = device.OfType<SunSpecMultipleMppt>().Single();
        var extendedMeasurements = device.OfType<SunSpecInverterExtendedMeasurements>().Single();
        var controls = device.OfType<SunSpecInverterControls>().Single();

        Assert.AreEqual("Fronius", commonBlock.Manufacturer);
        Assert.LessOrEqual(inverter.PowerFactorTotal, 1);
        Assert.GreaterOrEqual(inverter.PowerFactorTotal, -1);
        Assert.AreEqual((ushort)0xffff, basicSettings.ConnectedPhaseI);
        Assert.IsNull(namePlate.AmpereHoursCapacity);
        Assert.AreEqual(4, multipleMppts.NumberOfTrackers);
        Assert.GreaterOrEqual(extendedMeasurements.IsolationResistance, 100000);
        Assert.AreEqual(SunSpecOnOff.Disabled, controls.ActivePowerLimitEnabled);
        Assert.LessOrEqual(controls.RelativeActivePowerLimit, 1);
    }

    [Test]
    public async Task Meter_Success()
    {
        await client.ConnectAsync("192.168.44.10", 502, 200).ConfigureAwait(false);
        var device = await client.GetDataAsync().ConfigureAwait(false);
        var sunSpecMeter = new SunSpecMeter(device);
    }
}
