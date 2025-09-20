using De.Hochstaetter.Fronius.Models.Events;

namespace De.Hochstaetter.Fronius.Contracts;

public interface IWattPilotService
{
    public event EventHandler<NewWattPilotFirmwareEventArgs>? NewFirmwareAvailable;
    public event EventHandler<WattPilotServiceStoppedEventArgs>? OnLostConnection;
    public event EventHandler<WattPilotUpdateEventArgs>? OnUpdate;
    
    WebConnection? Connection { get; }
    WattPilot? WattPilot { get; }
    IReadOnlyList<WattPilotAcknowledge> UnsuccessfulWrites { get; }
    ValueTask StartAsync(WebConnection connection);
    ValueTask StopAsync();
    Task WaitSendValues(int timeout = 5000);
    void BeginSendValues();
    ValueTask SendValue(WattPilot instance, string propertyName);
    ValueTask<List<string>> Send(WattPilot? localWattPilot = null, WattPilot? oldWattPilot = null);
    Task RebootWattPilot();
    void OpenChargingLog();
    void OpenConfigPdf();
}