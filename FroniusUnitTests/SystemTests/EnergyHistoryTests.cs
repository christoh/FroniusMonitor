using System.Diagnostics.CodeAnalysis;
using System.Threading;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Services;

namespace FroniusUnitTests.SystemTests;

public class Settings : SettingsBase;

[TestFixture]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class EnergyHistoryTests
{
    public EnergyHistoryTests()
    {
        var services = new ServiceCollection()
                .AddSingleton<IGen24Service, Gen24Service>()
                .AddSingleton<IGen24JsonService, Gen24JsonService>()
                .AddSingleton<IFritzBoxService, FritzBoxService>()
                .AddSingleton<IWattPilotService, WattPilotService>()
                .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
                .AddSingleton<IDataCollectionService, DataCollectionService>()
                .AddSingleton(new SynchronizationContext())
                .AddSingleton<SettingsBase>(new Settings())
            ;

        IoC.Update(services.BuildServiceProvider());
    }

    [Test]
    public async Task TestEnergyHistory()
    {
        //const string excelFileName = @"C:\Users\hocc\Downloads\2024-08-13_2024-08-14_Messwerte_1EFR2375141563.xlsx";
        var settings = IoC.Get<SettingsBase>();
        settings.EnergyHistoryFileName = @"V:\var\log\EnergyHistory-2456552877.log";
        settings.DriftFileName = @"C:\Users\hocc\OneDrive\Dokumente\FroniusMonitor\Drifts-Meter2.xml";
        var service = IoC.Get<IDataCollectionService>();
        var property = typeof(DataCollectionService).GetProperty(nameof(DataCollectionService.SmartMeterHistory));
        property!.SetValue(service, await service.ReadCalibrationHistory());
    }
}