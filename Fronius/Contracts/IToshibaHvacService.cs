namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     The connection to one Toshiba account: the HTTPS side for login, registration and the device list, and the
///     Azure IoT Hub side for live state and commands. One instance serves every air conditioner of the account.
/// </summary>
public interface IToshibaHvacService
{
    /// <summary>
    ///     Ends a running connection, then registers with the Toshiba service under <paramref name="azureDeviceId" />,
    ///     loads the device list and opens the IoT Hub connection. Failures are logged and leave
    ///     <see cref="IsRunning" /> false; nothing is thrown.
    /// </summary>
    ValueTask Start(AzureConnection? azureConnection, string azureDeviceId);

    ValueTask Stop();

    /// <summary>The service holds a session and an IoT Hub client; see <see cref="IsConnected" /> for the hub's own state.</summary>
    bool IsRunning { get; }

    /// <summary>What the IoT Hub client last reported about its connection.</summary>
    bool IsConnected { get; }

    BindableCollection<ToshibaHvacMapping>? AllDevices { get; }

    /// <summary>
    ///     Reads the device list again and merges it into <see cref="AllDevices" /> in place, so a device the caller
    ///     already holds keeps its identity and only its values change. Returns every device of the account.
    /// </summary>
    ValueTask<IReadOnlyList<ToshibaHvacMappingDevice>> RefreshDevices();

    /// <summary>Sends a command and returns the message id it went out under, without waiting for anything.</summary>
    ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings);

    /// <summary>
    ///     Sends a command and waits until every target has echoed it or <paramref name="timeout" /> has passed.
    ///     The result names the targets that stayed silent.
    /// </summary>
    ValueTask<ToshibaHvacCommandResult> SendDeviceCommandAndWait(ToshibaHvacStateData state, TimeSpan timeout, params string[] targetIdStrings);

    ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params Guid[] deviceUniqueIds) => SendDeviceCommand(state, deviceUniqueIds.Select(id => id.ToString("D")).ToArray());

    ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params ToshibaHvacMappingDevice[] device) => SendDeviceCommand(state, device.Select(d => d.DeviceUniqueId.ToString("D")).ToArray());

    /// <summary>Every message the IoT Hub delivered, after it has been applied to the device it came from.</summary>
    event EventHandler<ToshibaHvacAzureSmMobileCommand>? LiveDataReceived;

    /// <summary>One device has new state, from a live update or a heartbeat.</summary>
    event EventHandler<ToshibaHvacDeviceUpdatedEventArgs>? DeviceUpdated;

    /// <summary>
    ///     The IoT Hub client gave up: its own retries are exhausted and it will not come back on its own. The service
    ///     is still <see cref="IsRunning" /> but delivers nothing until it is started again.
    /// </summary>
    event EventHandler? ConnectionLost;
}
