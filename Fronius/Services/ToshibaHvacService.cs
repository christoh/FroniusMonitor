using System.Net.Http.Json;
using System.Net.WebSockets;
using De.Hochstaetter.Fronius.Models.JsonConverters;

namespace De.Hochstaetter.Fronius.Services;

/// <summary>
///     See <see cref="IToshibaHvacService" />. Talks to the Toshiba cloud the way the official app does since it
///     dropped the IoT Hub: HTTPS for the login, the device list and the commands (<c>/api/IoTHub/SendCommand</c>),
///     and an Azure Web PubSub socket for the live state, negotiated at <c>/api/RealTime/GetWebPubSubToken</c> with
///     one group per air conditioner. Nothing is registered with the service any more, so any number of clients may
///     share one account. The session (bearer token) comes from and goes to <see cref="IToshibaHvacSessionStore" />,
///     so this class knows nothing about whose settings it lives in. The <see cref="SynchronizationContext" /> is
///     optional: the WPF app registers its UI context so that <see cref="AllDevices" /> can be bound; the server has
///     none and gets a plain context that runs inline.
/// </summary>
/// <remarks>
///     The Web PubSub token lives 15 minutes. Like the app, the service negotiates a new one <see cref="renewalLead" />
///     before it expires and swaps the socket; a socket that closes for any other reason is reopened after
///     <see cref="reconnectDelay" />. Only when that reopening itself fails is <see cref="ConnectionLost" /> raised
///     and the owner expected to <see cref="Start" /> again.
/// </remarks>
public partial class ToshibaHvacService(IToshibaHvacSessionStore sessionStore, ILogger<ToshibaHvacService> logger, SynchronizationContext? context = null) : BindableBase, IToshibaHvacService
{
    private const string RealtimeSubProtocol = "json.webpubsub.azure.v1";
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan setupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan renewalLead = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan minimumRenewalDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan reconnectDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     A command that has been sent and whose echo is awaited: the targets that have not answered yet, by
    ///     device unique id in lower case, and the task the sender waits on.
    /// </summary>
    private sealed record PendingEcho(HashSet<string> Outstanding, TaskCompletionSource Completion);

    private readonly SynchronizationContext context = context ?? SynchronizationContext.Current ?? new SynchronizationContext();
    private readonly SemaphoreSlim lifecycleSemaphore = new(1, 1);
    private readonly SemaphoreSlim realtimeSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, PendingEcho> pendingEchoes = new();
    private string? clientId;
    private WebConnection? connection;
    private ToshibaHvacSession? session;
    private ClientWebSocket? socket;
    private int realtimeGeneration;
    private ulong messageId;

    public event EventHandler<ToshibaHvacAzureSmMobileCommand>? LiveDataReceived;
    public event EventHandler<ToshibaHvacDeviceUpdatedEventArgs>? DeviceUpdated;
    public event EventHandler? ConnectionLost;

    static ToshibaHvacService()
    {
        jsonOptions.Converters.Add(new ToshibaHexConverter<int>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<byte>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<sbyte>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ushort>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaHvacOperatingMode>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaHvacFanSpeed>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaHvacPowerState>());
#if DEBUG
        jsonOptions.WriteIndented = true;
#endif
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Token), nameof(IsRunning))]
    private partial CancellationTokenSource? TokenSource { get; set; }

    private CancellationToken Token => TokenSource?.Token ?? throw new WebException("Connection closed", WebExceptionStatus.ConnectionClosed);

    public bool IsRunning => TokenSource is not null;

    [ObservableProperty]
    public partial bool IsConnected { get; private set; }

    [ObservableProperty]
    public partial BindableCollection<ToshibaHvacMapping>? AllDevices { get; private set; }

    public async ValueTask Stop()
    {
        await lifecycleSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await StopCore().ConfigureAwait(false);
        }
        finally
        {
            lifecycleSemaphore.Release();
        }
    }

    private async ValueTask StopCore()
    {
        try
        {
            // A new generation first, so that the receive loop of the socket closed below does not try to reopen it.
            Interlocked.Increment(ref realtimeGeneration);

            if (TokenSource != null)
            {
                await TokenSource.CancelAsync().ConfigureAwait(false);
            }

            await CloseSocket(Interlocked.Exchange(ref socket, null)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The Toshiba realtime channel did not close cleanly");
            }
        }
        finally
        {
            foreach (var pending in pendingEchoes.Values)
            {
                pending.Completion.TrySetCanceled();
            }

            pendingEchoes.Clear();
            AllDevices?.Clear();
            IsConnected = false;
            TokenSource?.Dispose();
            TokenSource = null;
        }
    }

    public async ValueTask Start(WebConnection? connection, string azureDeviceId)
    {
        if (connection == null)
        {
            return;
        }

        await lifecycleSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await StopCore().ConfigureAwait(false);
            clientId = azureDeviceId;
            this.connection = connection;

            try
            {
                TokenSource = new CancellationTokenSource();
                using var setup = CancellationTokenSource.CreateLinkedTokenSource(Token);
                setup.CancelAfter(setupTimeout);

                await EnsureSession(setup.Token).ConfigureAwait(false);
                await WithLoginRetry(() => RefreshDevicesCore(setup.Token), setup.Token).ConfigureAwait(false);
                await ConnectRealtime(setup.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Unable to start the Toshiba HVAC service for {UserName}", connection.UserName);
                }

                await StopCore().ConfigureAwait(false);
            }
        }
        finally
        {
            lifecycleSemaphore.Release();
        }
    }

    /// <summary>Takes the stored session, or logs in when there is none. Nothing is sent to the service for a stored one.</summary>
    private async ValueTask EnsureSession(CancellationToken token)
    {
        session = sessionStore.Session;

        if (session != null)
        {
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("No stored Toshiba session, logging in as {UserName}", connection?.UserName);
        }

        await Login(token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Runs one authenticated call. Only when the Toshiba service rejects the token (HTTP 401 or 403) is a new
    ///     login made, once, and the call repeated; any other failure is thrown as it is - it is not a reason to log
    ///     in again, and the service resents logins.
    /// </summary>
    private async ValueTask<T> WithLoginRetry<T>(Func<ValueTask<T>> call, CancellationToken token)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (ToshibaHvacUnauthorizedException ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The stored Toshiba session was rejected, logging in again as {UserName}", connection?.UserName);
            }

            await Login(token).ConfigureAwait(false);
            return await call().ConfigureAwait(false);
        }
    }

    private async ValueTask Login(CancellationToken token)
    {
        var postData = new Dictionary<string, string>
        {
            { "Username", connection?.UserName ?? string.Empty },
            { "Password", connection?.Password ?? string.Empty },
        };

        session = null;

        try
        {
            session = await Deserialize<ToshibaHvacSession>("/api/Consumer/Login", postData, token).ConfigureAwait(false)
                      ?? throw new WebException("No session data received", WebExceptionStatus.ReceiveFailure);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to get Toshiba AC bearer token");
            }

            throw;
        }

        await sessionStore.SaveSessionAsync(session).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<ToshibaHvacMappingDevice>> RefreshDevices()
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("The Toshiba HVAC service is not running");
        }

        return await RefreshDevicesCore(Token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reads the mapping and merges it into <see cref="AllDevices" />: a group or device that is already there is
    ///     updated in place, so anybody holding the instance - the control service, a bound view - sees the new
    ///     values without the object changing under them. New ones are added, vanished ones removed.
    /// </summary>
    private async ValueTask<IReadOnlyList<ToshibaHvacMappingDevice>> RefreshDevicesCore(CancellationToken token)
    {
        if (session == null)
        {
            throw new UnauthorizedAccessException("No Toshiba session");
        }

        var mappings = await Deserialize<List<ToshibaHvacMapping>>($"/api/AC/GetConsumerACMapping?consumerId={session.ConsumerId}", null, token).ConfigureAwait(false);

        if (AllDevices == null)
        {
            AllDevices = new BindableCollection<ToshibaHvacMapping>(mappings, context);
        }
        else
        {
            foreach (var mapping in mappings)
            {
                var existing = AllDevices.FirstOrDefault(m => m.GroupId == mapping.GroupId);

                if (existing == null)
                {
                    AllDevices.Add(mapping);
                    continue;
                }

                existing.GroupName = mapping.GroupName;
                existing.ConsumerId = mapping.ConsumerId;
                existing.TimeZone = mapping.TimeZone;

                foreach (var device in mapping.Devices)
                {
                    var existingDevice = existing.Devices.FirstOrDefault(d => d.DeviceUniqueId == device.DeviceUniqueId);

                    if (existingDevice == null)
                    {
                        existing.Devices.Add(device);
                    }
                    else
                    {
                        existingDevice.CopyFrom(device);
                    }
                }

                foreach (var vanished in existing.Devices.Where(d => mapping.Devices.All(m => m.DeviceUniqueId != d.DeviceUniqueId)).ToList())
                {
                    existing.Devices.Remove(vanished);
                }
            }

            foreach (var vanished in AllDevices.Where(m => mappings.All(n => n.GroupId != m.GroupId)).ToList())
            {
                AllDevices.Remove(vanished);
            }
        }

        var devices = AllDevices.SelectMany(m => m.Devices).ToList();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Toshiba account {UserName} has {Count} air conditioner(s): {Devices}", connection?.UserName, devices.Count, string.Join(", ", devices.Select(d => d.Name)));
        }

        return devices;
    }

    /// <summary>
    ///     Negotiates a Web PubSub token, opens the socket on the URL it names and makes it the current one; a
    ///     socket that was current before is closed. The receive loop and the renewal run on their own from here.
    /// </summary>
    private async ValueTask ConnectRealtime(CancellationToken setupToken)
    {
        var realtimeToken = await Deserialize<ToshibaHvacRealtimeToken>("/api/RealTime/GetWebPubSubToken", new Dictionary<string, string>(), setupToken).ConfigureAwait(false);
        var newSocket = new ClientWebSocket();
        newSocket.Options.AddSubProtocol(RealtimeSubProtocol);

        try
        {
            await newSocket.ConnectAsync(new Uri(realtimeToken.Url), setupToken).ConfigureAwait(false);
        }
        catch
        {
            newSocket.Dispose();
            throw;
        }

        var generation = Interlocked.Increment(ref realtimeGeneration);
        var previous = Interlocked.Exchange(ref socket, newSocket);
        await CloseSocket(previous).ConfigureAwait(false);
        IsConnected = true;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Toshiba realtime channel connected for {Count} device group(s), token expires at {ExpiresAt:u}", realtimeToken.Groups.Count, realtimeToken.ExpiresAt);
        }

        // Both catch everything themselves; a task nobody awaits must never fault.
        var token = Token;
        _ = Task.Run(() => ReceiveLoop(newSocket, generation, token), CancellationToken.None);
        _ = Task.Run(() => RenewLater(realtimeToken.ExpiresAt, generation, token), CancellationToken.None);
    }

    /// <summary>
    ///     Reads frames until the socket closes or the service stops. A socket that closes while it is still the
    ///     current one is reopened; one that has been replaced or stopped is simply done.
    /// </summary>
    private async Task ReceiveLoop(ClientWebSocket loopSocket, int generation, CancellationToken token)
    {
        var buffer = new byte[64 * 1024];
        var text = new StringBuilder();
        var closedByServer = false;

        try
        {
            while (loopSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                text.Clear();
                ValueWebSocketReceiveResult result;

                do
                {
                    result = await loopSocket.ReceiveAsync(buffer.AsMemory(), token).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        closedByServer = true;

                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning("The Toshiba realtime channel was closed by the service: {Status} {Description}", loopSocket.CloseStatus, loopSocket.CloseStatusDescription);
                        }

                        break;
                    }

                    text.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                if (closedByServer)
                {
                    break;
                }

                if (text.Length > 0)
                {
                    HandleRealtimeMessage(text.ToString(), generation);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The service is stopping.
        }
        catch (Exception ex) when (generation != realtimeGeneration)
        {
            // The socket was replaced or closed on purpose; whatever the pending receive threw is of no interest.
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(ex, "A replaced Toshiba realtime socket ended");
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The Toshiba realtime channel failed");
            }
        }

        if (!token.IsCancellationRequested && generation == realtimeGeneration)
        {
            IsConnected = false;
            await Reconnect(generation, "the socket closed", reconnectDelay, token).ConfigureAwait(false);
        }
    }

    /// <summary>Waits until the token is about to expire, then negotiates a new one and swaps the socket.</summary>
    private async Task RenewLater(DateTimeOffset expiresAt, int generation, CancellationToken token)
    {
        try
        {
            var delay = expiresAt - DateTimeOffset.UtcNow - renewalLead;

            if (delay < minimumRenewalDelay)
            {
                delay = minimumRenewalDelay;
            }

            await Task.Delay(delay, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await Reconnect(generation, "the token is about to expire", TimeSpan.Zero, token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Opens a new socket in place of the one of <paramref name="generation" />, unless a newer one exists already
    ///     or the service has stopped. When the new one cannot be opened, <see cref="ConnectionLost" /> is raised.
    /// </summary>
    private async Task Reconnect(int generation, string reason, TimeSpan delay, CancellationToken token)
    {
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
            }

            await realtimeSemaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested || generation != realtimeGeneration)
                {
                    return;
                }

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Toshiba realtime channel: {Reason}, connecting again", reason);
                }

                using var setup = CancellationTokenSource.CreateLinkedTokenSource(token);
                setup.CancelAfter(setupTimeout);
                await ConnectRealtime(setup.Token).ConfigureAwait(false);
            }
            finally
            {
                realtimeSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The service is stopping.
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Unable to reopen the Toshiba realtime channel");
            }

            IsConnected = false;
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }
    }

    private static async ValueTask CloseSocket(ClientWebSocket? closing)
    {
        if (closing == null)
        {
            return;
        }

        try
        {
            if (closing.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await closing.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "closing", timeout.Token).ConfigureAwait(false);
            }
        }
        catch
        {
            // A socket that is already gone needs no goodbye.
        }
        finally
        {
            closing.Dispose();
        }
    }

    /// <summary>
    ///     One frame from Web PubSub: a system event about the connection, or a group message whose data is the same
    ///     command the IoT Hub used to deliver as the <c>smmobile</c> method.
    /// </summary>
    private void HandleRealtimeMessage(string json, int generation)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ToshibaHvacRealtimeEnvelope>(json, jsonOptions);

            switch (envelope?.Type)
            {
                case "system" when envelope.Event == "disconnected":
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("The Toshiba realtime service sent a disconnect: {Message}", envelope.Message);
                    }

                    IsConnected = false;
                    _ = Task.Run(() => Reconnect(generation, "the service sent a disconnect", reconnectDelay, Token), CancellationToken.None);
                    break;

                case "system":
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Toshiba realtime system event {Event}, connection {ConnectionId}", envelope.Event, envelope.ConnectionId);
                    }

                    break;

                case "message" when envelope.Data is { ValueKind: JsonValueKind.Object } data:
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Received Toshiba realtime message from group {Group}: {Data}", envelope.Group, data.GetRawText());
                    }

                    ApplyCommand(data.Deserialize<ToshibaHvacAzureSmMobileCommand>(jsonOptions)!);
                    break;

                default:
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Ignoring Toshiba realtime frame: {Json}", json);
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Unable to handle a Toshiba realtime message: {Json}", json);
            }
        }
    }

    /// <summary>
    ///     Applies a command an air conditioner sent - its state, its heartbeat - to the device it came from, strikes
    ///     the device off any echo that is being waited for, and raises the events.
    /// </summary>
    private void ApplyCommand(ToshibaHvacAzureSmMobileCommand command)
    {
        var device = AllDevices?.SelectMany(d => d.Devices).FirstOrDefault(d => string.Equals(d.DeviceUniqueId.ToString("D"), command.DeviceUniqueId, StringComparison.InvariantCultureIgnoreCase));

        if (device == null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Toshiba message {MessageId} from unknown device {DeviceUniqueId}", command.MessageId, command.DeviceUniqueId);
            }

            return;
        }

        var deviceUpdated = false;

        switch (command.CommandName)
        {
            case "CMD_FCU_FROM_AC":
                var stateData = command.PayLoad.EnumerateObject().First(o => o.Name == "data").Value.Deserialize<ToshibaHvacStateData>(jsonOptions)!;
                device.State.UpdateStateData(stateData);
                deviceUpdated = true;
                break;

            case "CMD_HEARTBEAT":
                var heartbeat = command.PayLoad.Deserialize<ToshibaHvacHeartbeat>(jsonOptions)!;
                device.State.UpdateHeartBeatData(heartbeat);
                deviceUpdated = true;
                break;

            case "CMD_SET_SCHEDULE_FROM_AC":
                break;
        }

        if (pendingEchoes.TryGetValue(command.MessageId, out var pending))
        {
            bool complete;

            lock (pending.Outstanding)
            {
                pending.Outstanding.Remove(command.DeviceUniqueId.ToLowerInvariant());
                complete = pending.Outstanding.Count == 0;
            }

            if (complete)
            {
                pending.Completion.TrySetResult();
            }
        }

        if (deviceUpdated)
        {
            DeviceUpdated?.Invoke(this, new ToshibaHvacDeviceUpdatedEventArgs(device, command));
        }

        LiveDataReceived?.Invoke(this, command);
    }

    public async ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings)
    {
        var command = CreateCommand(state, targetIdStrings);
        await SendCommand(command).ConfigureAwait(false);
        return command.MessageId;
    }

    private ToshibaHvacAzureSmMobileCommand CreateCommand(ToshibaHvacStateData state, string[] targetIdStrings)
    {
        if (connection == null || session == null || clientId == null || !IsRunning || !IsConnected)
        {
            throw new InvalidOperationException("Not connected to the Toshiba service");
        }

        var currentMessageId = Interlocked.Increment(ref messageId);

        // The app's message id is "MB_" + 15 characters of its own device id + a counter; an echo carries it back.
        return new ToshibaHvacAzureSmMobileCommand
        {
            CommandName = "CMD_FCU_TO_AC",
            DeviceUniqueId = clientId,
            MessageId = $"MB_{clientId[..Math.Min(15, clientId.Length)].ToUpperInvariant()}-{currentMessageId % 100000000:D8}",
            TargetIds = targetIdStrings,
            TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            PayLoad = JsonDocument.Parse($"{{ \"data\":\"{state}\"}}").RootElement,
        };
    }

    /// <summary>
    ///     Posts the command to the service, which forwards it to the air conditioners. A target the service could not
    ///     or would not deliver to is an error here; a target that took it but does not echo is what
    ///     <see cref="SendDeviceCommandAndWait" /> reports.
    /// </summary>
    private async ValueTask SendCommand(ToshibaHvacAzureSmMobileCommand command)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Sending Toshiba command {MessageId} to {Targets}: {Payload}", command.MessageId, string.Join(", ", command.TargetIds), command.PayLoad.GetRawText());
        }

        var delivery = await Deserialize<ToshibaHvacCommandDelivery>("/api/IoTHub/SendCommand", command, Token).ConfigureAwait(false);
        var undelivered = delivery.FailedTargetIds.Concat(delivery.UnauthorizedTargetIds).ToList();

        if (undelivered.Count > 0)
        {
            throw new InvalidDataException($"The Toshiba service did not deliver command {command.MessageId} to {string.Join(", ", undelivered)} ({delivery.Status})");
        }
    }

    public async ValueTask<ToshibaHvacCommandResult> SendDeviceCommandAndWait(ToshibaHvacStateData state, TimeSpan timeout, params string[] targetIdStrings)
    {
        // The echo carries the message id of the command, so the record is keyed by that - and it is registered
        // before the send, because the answer can be on the socket before the HTTPS call has returned.
        var command = CreateCommand(state, targetIdStrings);
        var sentMessageId = command.MessageId;
        var pending = new PendingEcho([.. targetIdStrings.Select(id => id.ToLowerInvariant())], new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        pendingEchoes[sentMessageId] = pending;

        try
        {
            await SendCommand(command).ConfigureAwait(false);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(Token);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await pending.Completion.Task.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!Token.IsCancellationRequested)
            {
                // Timed out; whoever is still outstanding is reported below.
            }

            List<string> unconfirmed;

            lock (pending.Outstanding)
            {
                unconfirmed = [.. targetIdStrings.Where(id => pending.Outstanding.Contains(id.ToLowerInvariant()))];
            }

            if (unconfirmed.Count > 0)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Toshiba command {MessageId} was not echoed by {Targets} within {Timeout:N0} ms", sentMessageId, string.Join(", ", unconfirmed), timeout.TotalMilliseconds);
                }
            }

            return new ToshibaHvacCommandResult { MessageId = sentMessageId, Unconfirmed = unconfirmed };
        }
        finally
        {
            pendingEchoes.TryRemove(sentMessageId, out _);
        }
    }

    /// <summary>
    ///     One call to the Toshiba HTTPS API: GET without a body, POST with <paramref name="body" /> as JSON. HTTP 401
    ///     and 403 become <see cref="ToshibaHvacUnauthorizedException" />, because that is the rejected token the
    ///     login logic looks for; every other failure is what it is.
    /// </summary>
    private async ValueTask<T> Deserialize<T>(string uri, object? body, CancellationToken token) where T : new()
    {
        if (connection == null)
        {
            throw new InvalidDataException("No active Toshiba connection");
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HomeAutomationClient", FroniusGitInfo.Version.ToString()));
        client.BaseAddress = new Uri(connection.BaseUrl);

        using var message = new HttpRequestMessage(body == null ? HttpMethod.Get : HttpMethod.Post, uri);

        if (session != null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue(session.TokenType, session.AccessToken);
        }

        if (body != null)
        {
            message.Content = JsonContent.Create(body, body.GetType(), options: jsonOptions);
        }

        using var response = await client.SendAsync(message, token).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new ToshibaHvacUnauthorizedException(uri, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

#if !DEBUG // This allows you to see the raw JSON string
        var jsonText = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Toshiba response from {Uri}: {Json}", uri, jsonText);
        }

        var result = JsonSerializer.Deserialize<ToshibaHvacResponse<T>>(jsonText, jsonOptions) ?? throw new InvalidDataException("No data");
#else
        var result = await response.Content.ReadFromJsonAsync<ToshibaHvacResponse<T>>(jsonOptions, token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");
#endif

        return !result.IsSuccess
            ? throw new InvalidDataException(result.Message)
            : result.Data;
    }
}
