using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Services;
using De.Hochstaetter.FroniusMonitor.Models;
using De.Hochstaetter.FroniusMonitor.Services;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.SystemTests;

/// <summary>
/// Reads a real smart meter calibration history back off the developer's machine. The two file names below are
/// his and nobody else's, which is the second reason this one is explicit.
/// </summary>
/// <remarks>
/// It is also the one class that adds to the assembly-wide injector of <see cref="TestInjector"/>, because
/// <c>DataCollectionService</c> is the WPF app's and no other test wants it: <c>Gen24PowerFlow</c> reads its
/// calibration history through <c>IoC.TryGet</c> into a static field, so registering it for everybody would change
/// what an ordinary test sees. Adding to the standard registrations rather than replacing them keeps whatever else
/// is running able to resolve what it always could.
/// </remarks>
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public sealed class EnergyHistoryTests
{
    public EnergyHistoryTests() => IoC.Update
    (
        TestInjector.CreateServices()
            .AddSingleton<IFritzBoxService, FritzBoxService>()
            .AddSingleton<IWattPilotService, WattPilotService>()
            .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
            .AddSingleton<IDataCollectionService, DataCollectionService>()
            .AddSingleton(new SynchronizationContext())
            .BuildServiceProvider()
    );

    [SystemFact]
    public async Task A_calibration_history_is_read_back_from_its_log()
    {
        var settings = IoC.Get<SettingsBase>();
        settings.EnergyHistoryFileName = @"V:\var\log\EnergyHistory-2456552877.log";
        settings.DriftFileName = @"C:\Users\hocc\OneDrive\Dokumente\FroniusMonitor\Drifts-Meter2.xml";

        var service = IoC.Get<IDataCollectionService>();
        var history = await service.ReadCalibrationHistory();

        // The property has no public setter: the service fills it itself while it is collecting.
        var property = typeof(DataCollectionService).GetProperty(nameof(DataCollectionService.SmartMeterHistory));
        property!.SetValue(service, history);

        Assert.NotNull(history);
    }
}
