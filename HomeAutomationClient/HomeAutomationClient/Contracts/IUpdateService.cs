using System.Collections.ObjectModel;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.HomeAutomationClient.Contracts;

public interface IUpdateService : IDisposable, IAsyncDisposable
{
    internal event EventHandler<SitePowerFlowUpdatedEventArgs>? SitePowerFlowUpdated;
    
    public ObservableCollection<KeyedGen24System> Inverters { get; }

    public ObservableCollection<IKeyedDevice> AllPowerConsumers { get; }

    public List<KeyedWattPilotUpdate> WattPilotUpdates { get; }

    public Gen24PowerMeter3P? SmartMeter { get; }

    public Gen24Status? MeterStatus { get; }

    public Gen24Config? PrimaryGen24Config { get; }

    public Gen24System? BatteryGen24System { get; }

    public Gen24PowerFlow SitePowerFlow { get; }

    public double SitePvPeakPower { get; }

    public bool ShowInverters { get; }

    public bool ShowPowerConsumers { get; }

    public Task StartAsync();
}
