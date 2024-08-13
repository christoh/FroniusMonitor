using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Services;

namespace FroniusUnitTests.UnitTests;

public class Settings : SettingsBase;

[TestFixture]
public class EnergyHistoryTests
{
    private readonly IServiceProvider provider;
        
    /*        SettingsBase settings,
       IGen24Service gen24Service,
       IFritzBoxService fritzBoxService,
       IWattPilotService wattPilotService,
       IToshibaHvacService hvacService,
       SynchronizationContext context
*/

    public EnergyHistoryTests()
    {
        var services = new ServiceCollection()
                .AddSingleton<IGen24Service, Gen24Service>()
                .AddSingleton<IGen24JsonService, Gen24JsonService>()
                .AddSingleton<IFritzBoxService, FritzBoxService>()
                .AddSingleton<IWattPilotService, WattPilotService>()
                .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
                .AddSingleton<IDataCollectionService,DataCollectionService>()
                .AddSingleton(new SynchronizationContext())
                .AddSingleton<SettingsBase>(new Settings())
            ;

        provider = services.BuildServiceProvider();
        IoC.Update(provider);
    }

    [Test]
    public async Task TestEnergyHistory()
    {
        const string energyHistoryFileName = @"V:\var\log\EnergyHistory-2456552877.log";
        const string excelFileName = @"C:\Users\hocc\Downloads\2024-08-12_2024-08-12_Messwerte_1EFR2375141563.xlsx";
        var service = IoC.Get<IDataCollectionService>();
        await service.DoBayernwerkCalibration(energyHistoryFileName, excelFileName);
    }
}