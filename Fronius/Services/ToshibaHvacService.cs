using System.Net.Http.Json;
using De.Hochstaetter.Fronius.Models.JsonConverters;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace De.Hochstaetter.Fronius.Services;

/// <summary>
///     See <see cref="IToshibaHvacService" />. The session (bearer token) comes from and goes to
///     <see cref="IToshibaHvacSessionStore" />, so this class knows nothing about whose settings it lives in. The
///     <see cref="SynchronizationContext" /> is optional: the WPF app registers its UI context so that
///     <see cref="AllDevices" /> can be bound; the server has none and gets a plain context that runs inline.
/// </summary>
public partial class ToshibaHvacService(IToshibaHvacSessionStore sessionStore, ILogger<ToshibaHvacService> logger, SynchronizationContext? context = null) : BindableBase, IToshibaHvacService
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     A command that has been sent and whose echo is awaited: the targets that have not answered yet, by
    ///     device unique id in lower case, and the task the sender waits on.
    /// </summary>
    private sealed record PendingEcho(HashSet<string> Outstanding, TaskCompletionSource Completion);

    private readonly SynchronizationContext context = context ?? SynchronizationContext.Current ?? new SynchronizationContext();
    private readonly SemaphoreSlim lifecycleSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, PendingEcho> pendingEchoes = new();
    private string? azureDeviceId;
    private AzureConnection? azureConnection;
    private ToshibaHvacSession? session;
    private DeviceClient? azureClient;
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

    /// <summary>The mobile device this installation is registered as: the account name and the six digit id.</summary>
    private string MobileDeviceId => (azureConnection?.UserName ?? string.Empty).ToLowerInvariant() + "_" + azureDeviceId;

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
            if (TokenSource != null)
            {
                await TokenSource.CancelAsync().ConfigureAwait(false);
            }

            if (azureClient != null)
            {
                await azureClient.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The Toshiba IoT Hub client did not close cleanly");
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
            azureClient = null;
            IsConnected = false;
            TokenSource?.Dispose();
            TokenSource = null;
        }
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async ValueTask Start(AzureConnection? connection, string deviceId)
    {
        if (connection == null)
        {
            return;
        }

        await lifecycleSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await StopCore().ConfigureAwait(false);
            azureDeviceId = deviceId;
            azureConnection = connection;

            try
            {
                // A short leash for the setup only; the token of the running service is replaced below.
                TokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var azureCredentials = await RegisterMobileDevice().ConfigureAwait(false);
                await RefreshDevicesCore().ConfigureAwait(false);

                var connectionString = $"HostName={azureCredentials.HostName};DeviceId={azureCredentials.DeviceId};SharedAccessKey={azureCredentials.PrimaryKey}";
                azureClient = DeviceClient.CreateFromConnectionString(connectionString, azureConnection.TransportType);

                azureClient.SetRetryPolicy(new ExponentialBackoff(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100.0)));
                azureClient.SetConnectionStatusChangesHandler(OnAzureConnectionStatusChange);
                await azureClient.OpenAsync(Token).ConfigureAwait(false);
                await azureClient.SetMethodHandlerAsync("smmobile", HandleSmMobileMethod, null, Token).ConfigureAwait(false);

//#if DEBUG
                // ReSharper disable once UnusedParameter.Local
                await azureClient.SetReceiveMessageHandlerAsync(async (message, userContext) => { await azureClient.CompleteAsync(message, Token).ConfigureAwait(false); }, null, Token).ConfigureAwait(false);
                await azureClient.SetMethodDefaultHandlerAsync(HandleOtherMethods, null, Token).ConfigureAwait(false);
//#endif

                TokenSource?.Dispose();
                TokenSource = new CancellationTokenSource();
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

    /// <summary>
    ///     Registers this installation as a mobile device of the account and gets the IoT Hub credentials for it.
    ///     The stored session is used first; only when the Toshiba service rejects its token (HTTP 401 or 403) is a
    ///     new login made, once. Any other failure is thrown as it is - it is not a reason to log in again.
    /// </summary>
    private async ValueTask<ToshibaHvacAzureCredentials> RegisterMobileDevice()
    {
        session = sessionStore.Session;

        if (session == null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("No stored Toshiba session, logging in as {UserName}", azureConnection?.UserName);
            }

            await Login().ConfigureAwait(false);
        }

        try
        {
            return await Register().ConfigureAwait(false);
        }
        catch (ToshibaHvacUnauthorizedException ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "The stored Toshiba session was rejected, logging in again as {UserName}", azureConnection?.UserName);
            }

            await Login().ConfigureAwait(false);
            return await Register().ConfigureAwait(false);
        }

        async ValueTask<ToshibaHvacAzureCredentials> Register()
        {
            var postData = new Dictionary<string, string>
            {
                { "DeviceID", MobileDeviceId },
                { "DeviceType", "1" },
                { "Username", azureConnection?.UserName ?? string.Empty },
            };

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Registering Toshiba HVAC mobile device with DeviceID: {DeviceID}", postData["DeviceID"]);
            }

            return await Deserialize<ToshibaHvacAzureCredentials>("/api/Consumer/RegisterMobileDevice", postData).ConfigureAwait(false);
        }

        async ValueTask Login()
        {
            var postData = new Dictionary<string, string>
            {
                { "Username", azureConnection?.UserName ?? string.Empty },
                { "Password", azureConnection?.Password ?? string.Empty },
            };

            session = null;

            try
            {
                session = await Deserialize<ToshibaHvacSession>("/api/Consumer/Login", postData).ConfigureAwait(false)
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
    }

    public async ValueTask<IReadOnlyList<ToshibaHvacMappingDevice>> RefreshDevices()
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("The Toshiba HVAC service is not running");
        }

        return await RefreshDevicesCore().ConfigureAwait(false);
    }

    /// <summary>
    ///     Reads the mapping and merges it into <see cref="AllDevices" />: a group or device that is already there is
    ///     updated in place, so anybody holding the instance - the control service, a bound view - sees the new
    ///     values without the object changing under them. New ones are added, vanished ones removed.
    /// </summary>
    private async ValueTask<IReadOnlyList<ToshibaHvacMappingDevice>> RefreshDevicesCore()
    {
        if (session == null)
        {
            throw new UnauthorizedAccessException("No Toshiba session");
        }

        var mappings = await Deserialize<List<ToshibaHvacMapping>>($"/api/AC/GetConsumerACMapping?consumerId={session.ConsumerId}").ConfigureAwait(false);

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
            logger.LogInformation("Toshiba account {UserName} has {Count} air conditioner(s): {Devices}", azureConnection?.UserName, devices.Count, string.Join(", ", devices.Select(d => d.Name)));
        }

        return devices;
    }

    public async ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings)
    {
        var command = CreateCommand(state, targetIdStrings);
        await SendCommand(command).ConfigureAwait(false);
        return command.MessageId;
    }

    private ToshibaHvacAzureSmMobileCommand CreateCommand(ToshibaHvacStateData state, string[] targetIdStrings)
    {
        if (azureConnection == null || azureClient == null || session == null || !IsRunning)
        {
            throw new IotHubCommunicationException("Not connected");
        }

        var currentMessageId = Interlocked.Increment(ref messageId);

        return new ToshibaHvacAzureSmMobileCommand
        {
            CommandName = "CMD_FCU_TO_AC",
            DeviceUniqueId = MobileDeviceId,
            MessageId = $"MB_{azureDeviceId![..Math.Min(15, azureDeviceId!.Length)].ToUpperInvariant()}-{currentMessageId % 100000000:D8}",
            TargetIds = targetIdStrings,
            TimeStamp = DateTime.UtcNow.TimeOfDay.ToString(),
            PayLoad = JsonDocument.Parse($"{{ \"data\":\"{state}\"}}").RootElement,
        };
    }

    private async ValueTask SendCommand(ToshibaHvacAzureSmMobileCommand command)
    {
        if (azureClient == null)
        {
            throw new IotHubCommunicationException("Not connected");
        }

        await using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, command, jsonOptions, Token).ConfigureAwait(false);
        memoryStream.Position = 0;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Sending Toshiba command: {Command}", Encoding.UTF8.GetString(memoryStream.ToArray()));
        }

        using var message = new Message(memoryStream);
        await azureClient.SendEventAsync(message, Token).ConfigureAwait(false);
    }

    public async ValueTask<ToshibaHvacCommandResult> SendDeviceCommandAndWait(ToshibaHvacStateData state, TimeSpan timeout, params string[] targetIdStrings)
    {
        // The echo carries the message id of the command, so the record is keyed by that - and it is registered
        // before the send, because the answer can be on its way before SendEventAsync has returned.
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

    private void OnAzureConnectionStatusChange(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        IsConnected = status == ConnectionStatus.Connected;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Toshiba IoT Hub connection is {Status} ({Reason})", status, reason);
        }

        // Disconnected_Retrying is the client still working on it. Disconnected is the client having given up -
        // the retries are exhausted or the credentials went bad - and it never recovers from that by itself.
        if (status is ConnectionStatus.Disconnected or ConnectionStatus.Disabled && reason != ConnectionStatusChangeReason.Client_Close)
        {
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }
    }

    private Task<MethodResponse> HandleSmMobileMethod(MethodRequest request, object _) => Task.Run(() =>
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Received Toshiba method {MethodName}: {Data}", request.Name, request.DataAsJson);
        }

        try
        {
            var command = JsonSerializer.Deserialize<ToshibaHvacAzureSmMobileCommand>(request.Data, jsonOptions)!;
            var device = AllDevices?.SelectMany(d => d.Devices).FirstOrDefault(d => string.Equals(d.DeviceUniqueId.ToString("D"), command.DeviceUniqueId, StringComparison.InvariantCultureIgnoreCase));

            if (device == null)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Toshiba message {MessageId} from unknown device {DeviceUniqueId}", command.MessageId, command.DeviceUniqueId);
                }

                return new MethodResponse(1);
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
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Unable to handle Toshiba method {MethodName}", request.Name);
            }

            return new MethodResponse(1);
        }

        return new MethodResponse(0);
    }, Token);

//#if DEBUG

    private Task<MethodResponse> HandleOtherMethods(MethodRequest request, object _) => Task.Run(() =>
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Received Toshiba method {MethodName}: {Data}", request.Name, request.DataAsJson);
        }

        return new MethodResponse(0);
    }, Token);

//#endif

    /// <summary>
    ///     One call to the Toshiba HTTPS API. HTTP 401 and 403 become <see cref="ToshibaHvacUnauthorizedException" />,
    ///     because that is the rejected token the login logic looks for; every other failure is what it is.
    /// </summary>
    private async ValueTask<T> Deserialize<T>(string uri, IDictionary<string, string>? postVariables = null) where T : new()
    {
        if (azureConnection == null)
        {
            throw new InvalidDataException("No active Toshiba connection");
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HomeAutomationClient", FroniusGitInfo.Version.ToString()));
        client.BaseAddress = new Uri(azureConnection.BaseUrl);

        using var message = new HttpRequestMessage(postVariables == null ? HttpMethod.Get : HttpMethod.Post, uri);

        if (session != null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue(session.TokenType, session.AccessToken);
        }

        if (postVariables != null)
        {
            message.Content = JsonContent.Create(postVariables);
        }

        using var response = await client.SendAsync(message, Token).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new ToshibaHvacUnauthorizedException(uri, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

#if !DEBUG // This allows you to see the raw JSON string
        var jsonText = await response.Content.ReadAsStringAsync(Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Toshiba response from {Uri}: {Json}", uri, jsonText);
        }

        var result = JsonSerializer.Deserialize<ToshibaHvacResponse<T>>(jsonText, jsonOptions) ?? throw new InvalidDataException("No data");
#else
        var result = await response.Content.ReadFromJsonAsync<ToshibaHvacResponse<T>>(jsonOptions, Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");
#endif

        return !result.IsSuccess
            ? throw new InvalidDataException(result.Message)
            : result.Data;
    }
}
