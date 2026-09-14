using System.Collections.ObjectModel;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Contracts;

public interface IUpdateService : IDisposable, IAsyncDisposable, IToshibaHvacCommander
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

    /// <summary>
    /// The price chart data of today and tomorrow as the server last pushed it, or <see langword="null"/> where
    /// the server collects none. Replaced as a whole on every push; <see cref="EnergyChartDataChanged"/> says when.
    /// </summary>
    public EnergyChartData? EnergyChartData { get; }

    /// <summary>True as soon as the server has sent price chart data, which is what makes the menu offer the chart.</summary>
    public bool HasEnergyData { get; }

    /// <summary>Raised on the thread the hub delivers on whenever <see cref="EnergyChartData"/> is replaced.</summary>
    event EventHandler<EnergyChartData>? EnergyChartDataChanged;

    public Task StartAsync();

    /// <summary>
    /// The counterpart of <see cref="StartAsync"/>: closes the hub connection and forgets every device, so a
    /// second <see cref="StartAsync"/> - after logging back in, possibly as somebody else - starts from nothing
    /// rather than from what the last session left behind.
    /// </summary>
    public Task StopAsync();

    /// <summary>
    /// Writes a Wattpilot's settings through the server: whatever differs between <paramref name="wanted"/> and
    /// <paramref name="loaded"/> goes to the charger, and the answer says which writes failed or went unconfirmed.
    /// Over the hub, because a Wattpilot write is a conversation on the connection the server holds to it - see
    /// <c>WattPilot.md</c>. Needs the Operator role.
    /// </summary>
    Task<WattPilotWriteResult> SetWattPilotSettings(string deviceId, WattPilot wanted, WattPilot loaded);

    Task RebootWattPilot(string deviceId);
}
