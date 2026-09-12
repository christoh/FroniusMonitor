namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     The connection to one Toshiba account, made the way the official app makes it: HTTPS for the login, the device
///     list and the commands, an Azure Web PubSub socket for the live state. One instance serves every air
///     conditioner of the account, and any number of instances may share an account.
/// </summary>
public interface IToshibaHvacService
{
    /// <summary>
    ///     Ends a running connection, then logs in if there is no stored session, loads the device list and opens the
    ///     realtime socket. <paramref name="azureDeviceId" /> is this installation's client id, which only names the
    ///     sender in the message ids of its commands. Failures are logged and leave <see cref="IsRunning" /> false;
    ///     nothing is thrown.
    /// </summary>
    ValueTask Start(WebConnection? connection, string azureDeviceId);

    ValueTask Stop();

    /// <summary>The service has been started and holds a session; see <see cref="IsConnected" /> for the socket.</summary>
    bool IsRunning { get; }

    /// <summary>The realtime socket is open. It is briefly false while the token is renewed every ten minutes.</summary>
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

    /// <summary>Every message the realtime socket delivered, after it has been applied to the device it came from.</summary>
    event EventHandler<ToshibaHvacAzureSmMobileCommand>? LiveDataReceived;

    /// <summary>One device has new state, from a live update or a heartbeat.</summary>
    event EventHandler<ToshibaHvacDeviceUpdatedEventArgs>? DeviceUpdated;

    /// <summary>
    ///     The realtime socket closed and could not be reopened. The service is still <see cref="IsRunning" /> but
    ///     delivers nothing until it is started again.
    /// </summary>
    event EventHandler? ConnectionLost;
}
