using De.Hochstaetter.HomeAutomationServer.Contracts.Modbus;
using De.Hochstaetter.HomeAutomationServer.Models.Modbus;
using De.Hochstaetter.HomeAutomationServer.Services.Modbus;

namespace De.Hochstaetter.HomeAutomationServerTests.SystemTests;

/// <summary>
/// Reads and writes the real SunSpec registers of the inverter and the smart meter on the developer's LAN. The
/// addresses below are his, and the inverter test writes to the battery's charging limits and puts them back.
/// </summary>
public sealed class SunSpecClientTests
{
    private readonly ISunSpecClient client;

    public SunSpecClientTests()
    {
        var services = new ServiceCollection()
            .AddSingleton<ISunSpecClient, SunSpecClient>()
            .AddLogging(builder => builder.AddTestOutput());

        client = services.BuildServiceProvider().GetRequiredService<ISunSpecClient>();
    }

    [SystemFact]
    public async Task The_inverter_reads_back_what_was_written_to_it()
    {
        await client.ConnectAsync("192.168.44.20", 502, 1);
        var inverter = new SunSpecInverter(await client.GetDataAsync());

        Assert.Equal("Fronius", inverter.Manufacturer);
        Assert.True(inverter.InverterBaseSensors?.PowerFactorTotal is >= -1 and <= 1);
        Assert.Equal((ushort?)0xffff, inverter.BasicSettings?.ConnectedPhaseI);
        Assert.Null(inverter.NamePlate?.AmpereHoursCapacity);
        Assert.Equal((ushort?)4, inverter.Tracker?.NumberOfTrackers);
        Assert.True(inverter.ExtendedSensors?.IsolationResistance >= 100000);
        Assert.Equal(1d, inverter.ExtendedSettings?.RelativeActivePowerLimit);
        Assert.Equal(SunSpecOnOff.Disabled, inverter.ExtendedSettings?.ActivePowerLimitEnabled);
        Assert.Equal(SunSpecChargingLimits.None, inverter.StorageSettings?.ChargingLimits);
        Assert.NotNull(inverter.StorageSettings);

        var storageSettings = inverter.StorageSettings;
        storageSettings.RelativeOutgoingActivePowerMax = 0;
        storageSettings.RelativeIncomingActivePowerMax = 0;
        storageSettings.ChargingLimits = SunSpecChargingLimits.Charging | SunSpecChargingLimits.Discharging;
        await WriteStorageSettings(storageSettings);

        storageSettings.RelativeOutgoingActivePowerMax = 1;
        storageSettings.RelativeIncomingActivePowerMax = 1;
        storageSettings.ChargingLimits = SunSpecChargingLimits.None;
        await WriteStorageSettings(storageSettings);
    }

    [SystemFact]
    public async Task The_smart_meter_answers_its_registers()
    {
        await client.ConnectAsync("192.168.44.10", 502, 200);
        var device = await client.GetDataAsync();

        Assert.NotNull(new SunSpecMeter(device));
    }

    private Task WriteStorageSettings(SunSpecStorageBaseSettings storageSettings) => client.WriteRegisters
    (
        storageSettings,
        CancellationToken.None,
        nameof(SunSpecStorageBaseSettings.RelativeOutgoingActivePowerMaxI),
        nameof(SunSpecStorageBaseSettings.RelativeIncomingActivePowerMaxI),
        nameof(SunSpecStorageBaseSettings.ChargingLimits)
    );
}
