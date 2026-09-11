using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Events;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

/// <summary>
/// A Toshiba service that talks to nobody: it remembers what it was started with and what it was asked to send,
/// answers with whatever the test put into it, and lets the test raise the events a real one would.
/// </summary>
internal sealed class FakeToshibaHvacService : IToshibaHvacService
{
    /// <summary>The devices <see cref="Start"/> and <see cref="RefreshDevices"/> pretend the account has.</summary>
    public List<ToshibaHvacMappingDevice> Devices { get; } = [];

    /// <summary>Targets (device unique ids) that <see cref="SendDeviceCommandAndWait"/> reports as silent.</summary>
    public List<string> SilentTargets { get; } = [];

    /// <summary>When false, <see cref="Start"/> leaves the service not running, as the real one does on failure.</summary>
    public bool StartSucceeds { get; set; } = true;

    public int StartCalls { get; private set; }
    public int StopCalls { get; private set; }
    public int RefreshCalls { get; private set; }
    public AzureConnection? StartedWith { get; private set; }
    public string? StartedWithDeviceId { get; private set; }
    public ToshibaHvacStateData? SentState { get; private set; }
    public string[]? SentTargets { get; private set; }
    public TimeSpan? SentTimeout { get; private set; }

    public bool IsRunning { get; set; }
    public bool IsConnected { get; set; }
    public BindableCollection<ToshibaHvacMapping>? AllDevices { get; private set; }

    public event EventHandler<ToshibaHvacAzureSmMobileCommand>? LiveDataReceived;
    public event EventHandler<ToshibaHvacDeviceUpdatedEventArgs>? DeviceUpdated;
    public event EventHandler? ConnectionLost;

    public ValueTask Start(AzureConnection? azureConnection, string azureDeviceId)
    {
        StartCalls++;
        StartedWith = azureConnection;
        StartedWithDeviceId = azureDeviceId;
        IsRunning = IsConnected = StartSucceeds;

        AllDevices = StartSucceeds
            ? new BindableCollection<ToshibaHvacMapping>([new ToshibaHvacMapping { GroupName = "Home", Devices = [.. Devices] }], new SynchronizationContext())
            : null;

        return ValueTask.CompletedTask;
    }

    public ValueTask Stop()
    {
        StopCalls++;
        IsRunning = IsConnected = false;
        AllDevices = null;
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<ToshibaHvacMappingDevice>> RefreshDevices()
    {
        RefreshCalls++;
        return ValueTask.FromResult<IReadOnlyList<ToshibaHvacMappingDevice>>(Devices);
    }

    public ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings)
    {
        SentState = state;
        SentTargets = targetIdStrings;
        return ValueTask.FromResult("MB_TEST-00000001");
    }

    public async ValueTask<ToshibaHvacCommandResult> SendDeviceCommandAndWait(ToshibaHvacStateData state, TimeSpan timeout, params string[] targetIdStrings)
    {
        SentTimeout = timeout;
        var messageId = await SendDeviceCommand(state, targetIdStrings);
        return new ToshibaHvacCommandResult { MessageId = messageId, Unconfirmed = [.. targetIdStrings.Where(t => SilentTargets.Contains(t, StringComparer.OrdinalIgnoreCase))] };
    }

    public void RaiseDeviceUpdated(ToshibaHvacMappingDevice device, string commandName = "CMD_FCU_FROM_AC") =>
        DeviceUpdated?.Invoke(this, new ToshibaHvacDeviceUpdatedEventArgs(device, new ToshibaHvacAzureSmMobileCommand { CommandName = commandName, DeviceUniqueId = device.DeviceUniqueId.ToString("D") }));

    public void RaiseLiveDataReceived(ToshibaHvacAzureSmMobileCommand command) => LiveDataReceived?.Invoke(this, command);

    public void RaiseConnectionLost() => ConnectionLost?.Invoke(this, EventArgs.Empty);
}
