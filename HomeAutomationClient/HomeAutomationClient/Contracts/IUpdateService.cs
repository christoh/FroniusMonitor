using System.Collections.ObjectModel;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;

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

    public IEnumerable<IKeyedDevice> DetailDevices { get; }

    IEnumerable<IKeyedDevice> DevicesWithSettings { get; }

    public bool ShowInverters { get; }

    public bool ShowPowerConsumers { get; }

    public Task StartAsync();

    /// <summary>
    /// Writes a Wattpilot's settings through the server: whatever differs between <paramref name="wanted"/> and
    /// <paramref name="loaded"/> goes to the charger, and the answer says which writes failed or went unconfirmed.
    /// Over the hub, because a Wattpilot write is a conversation on the connection the server holds to it - see
    /// <c>WattPilot.md</c>. Needs the Operator role.
    /// </summary>
    Task<WattPilotWriteResult> SetWattPilotSettings(string deviceId, WattPilot wanted, WattPilot loaded);

    Task RebootWattPilot(string deviceId);
}
