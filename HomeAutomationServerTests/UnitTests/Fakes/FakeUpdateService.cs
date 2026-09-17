using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.EnergyData;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using De.Hochstaetter.Fronius.Models.WebApi;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// An update service with no devices and no server behind it, for the tests of what the client does around it:
/// the presenters, the menu bar and the controls. Everything here is empty or does nothing, but the collections,
/// the site power flow and the peak power can be replaced the way the real service replaces them, and the
/// replacement is announced, so what follows the service can be tested following.
/// </summary>
/// <remarks>
/// It has to live in this assembly rather than be a mock, because <see cref="IUpdateService"/> declares an
/// internal event and only an assembly that sees the internals of the client can implement it - see the
/// <c>InternalsVisibleTo</c> in <c>HomeAutomationClient.csproj</c>.
/// </remarks>
public sealed partial class FakeUpdateService : BindableBase, IUpdateService
{
    event EventHandler<SitePowerFlowUpdatedEventArgs>? IUpdateService.SitePowerFlowUpdated
    {
        add { }
        remove { }
    }

    public event EventHandler<EnergyChartData>? EnergyChartDataChanged
    {
        add { }
        remove { }
    }

    [ObservableProperty]
    public partial ObservableCollection<KeyedGen24System> Inverters { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<IKeyedDevice> AllPowerConsumers { get; set; } = [];

    public List<KeyedWattPilotUpdate> WattPilotUpdates { get; } = [];

    public Gen24PowerMeter3P? SmartMeter => null;

    public Gen24Status? MeterStatus => null;

    public Gen24Config? PrimaryGen24Config => null;

    public Gen24System? BatteryGen24System => null;

    [ObservableProperty]
    public partial Gen24PowerFlow SitePowerFlow { get; set; } = new();

    [ObservableProperty]
    public partial double SitePvPeakPower { get; set; }

    public IEnumerable<IKeyedDevice> DetailDevices => [];

    public IEnumerable<IKeyedDevice> DevicesWithSettings => [];

    public bool ShowInverters => false;

    public bool ShowPowerConsumers => false;

    public EnergyChartData? EnergyChartData => null;

    public bool HasEnergyData => false;

    public Task StartAsync(Roles roles) => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;

    public Task<WattPilotWriteResult> SetWattPilotSettings(string deviceId, WattPilot wanted, WattPilot loaded) => throw new NotSupportedException();

    public Task RebootWattPilot(string deviceId) => throw new NotSupportedException();

    public Task<ToshibaHvacCommandResult> SendToshibaHvacCommand(string[] ids, ToshibaHvacStateData state) => throw new NotSupportedException();

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
