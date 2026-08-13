using System.Net.Http.Json;
using System.Text.Json;
using De.Hochstaetter.Fronius.Models.JsonConverters;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace De.Hochstaetter.Fronius.Services;

public partial class ToshibaHvacService(SynchronizationContext context, SettingsBase settings, ILogger<ToshibaHvacService> logger) : BindableBase, IToshibaHvacService
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    private string? azureDeviceId;
    private AzureConnection? azureConnection;
    private readonly SemaphoreSlim lifecycleSemaphore = new(1, 1);
    private ToshibaHvacSession? session;
    private DeviceClient? azureClient;
    private ulong messageId; // = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));

    public event EventHandler<ToshibaHvacAzureSmMobileCommand>? LiveDataReceived;

    static ToshibaHvacService()
    {
        // jsonOptions.Converters.Add(new ToshibaDateTimeConverter());
        jsonOptions.Converters.Add(new ToshibaHexConverter<int>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<byte>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<sbyte>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ushort>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaHvacOperatingMode>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaHvacFanSpeed>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaHvacPowerState>());
        jsonOptions.Converters.Add(new ToshibaStateDataConverter());
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
            if (TokenSource != null)
            {
                await TokenSource.CancelAsync();
            }

            if (azureClient != null)
            {
                await azureClient.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            AllDevices?.Clear();
            session = null;
            azureClient = null;
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
                TokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                var azureCredentials = await RefreshAll().ConfigureAwait(false);

                var connectionString = $"HostName={azureCredentials.HostName};DeviceId={azureCredentials.DeviceId};SharedAccessKey={azureCredentials.PrimaryKey}";
                azureClient = DeviceClient.CreateFromConnectionString(connectionString, azureConnection.TransportType);

                //var auth = AuthenticationMethodFactory.CreateAuthenticationWithToken(azureCredentials.DeviceId, azureCredentials.SasToken);
                //azureClient = DeviceClient.Create(azureCredentials.HostName, auth, azureConnection.TransportType);

                azureClient.SetRetryPolicy(new ExponentialBackoff(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100.0)));
                azureClient.SetConnectionStatusChangesHandler(OnAzureConnectionStatusChange);
                await azureClient.OpenAsync(Token).ConfigureAwait(false);
                await azureClient.SetMethodHandlerAsync("smmobile", HandleSmMobileMethod, null, Token).ConfigureAwait(false);

#if DEBUG

                // ReSharper disable once UnusedParameter.Local
                await azureClient.SetReceiveMessageHandlerAsync(async (message, userContext) => { await azureClient.CompleteAsync(message, Token).ConfigureAwait(false); }, null, Token);

                await azureClient.SetMethodDefaultHandlerAsync(HandleOtherMethods, null, Token).ConfigureAwait(false);

#endif

                TokenSource?.Dispose();
                TokenSource = new CancellationTokenSource();
            }
            catch
            {
                await StopCore().ConfigureAwait(false);
            }
        }
        finally
        {
            lifecycleSemaphore.Release();
        }
    }

    private async ValueTask<ToshibaHvacAzureCredentials> RefreshAll()
    {
        session = settings.ToshibaHvacSession;
        var username = azureConnection?.UserName ?? string.Empty;
        ToshibaHvacAzureCredentials? azureCredentials = null;
        var triedLogin = false;

        if (session == null)
        {
            await GetBearer().ConfigureAwait(false);
        }

        while (azureCredentials == null)
        {
            try
            {
                var postData = new Dictionary<string, string>
                {
                    { "DeviceID", username.ToLowerInvariant() + "_" + azureDeviceId },
                    { "DeviceType", "1" },
                    { "Username", username },
                };
                
                logger.LogDebug("Registering Toshiba HVAC mobile device with DeviceID: {DeviceID}", postData["DeviceID"]);
                azureCredentials = await Deserialize<ToshibaHvacAzureCredentials>("/api/Consumer/RegisterMobileDevice", postData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to register mobile device");

                if (triedLogin)
                {
                    throw;
                }
                
                await GetBearer().ConfigureAwait(false);
            }
        }

        if (session == null)
        {
            throw new UnauthorizedAccessException("Could not get Azure credentials");
        }

        var devices = await Deserialize<List<ToshibaHvacMapping>>($"/api/AC/GetConsumerACMapping?consumerId={session.ConsumerId}").ConfigureAwait(false);
        AllDevices = [with(devices, context)];
        // var test = await Deserialize<ToshibaHvacStatusDevice>($"/api/AC/GetCurrentACState?ACId={AllDevices[0].Devices[0].AcId}").ConfigureAwait(false);
        return azureCredentials;

        async ValueTask GetBearer()
        {
            triedLogin = true;
            
            var getBearerPostData = new Dictionary<string, string>
            {
                { "Username", username },
                { "Password", azureConnection?.Password ?? string.Empty },
            };

            try
            {
                session = await Deserialize<ToshibaHvacSession>("/api/Consumer/Login", getBearerPostData).ConfigureAwait(false)
                          ?? throw new WebException("No session data received", WebExceptionStatus.ReceiveFailure);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Failed to get Toshiba AC bearer token");
                throw;
            }

            settings.ToshibaHvacSession = session;
            settings.ToshibaHvacSessionTime = DateTime.UtcNow;
            await settings.Save().ConfigureAwait(false);
        }
    }

    public async ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings)
    {
        if (azureConnection == null || azureClient == null || session == null || !IsRunning)
        {
            throw new IotHubCommunicationException("Not connected");
        }

        var currentMessageId = Interlocked.Increment(ref messageId);

        var command = new ToshibaHvacAzureSmMobileCommand
        {
            CommandName = "CMD_FCU_TO_AC",
            DeviceUniqueId = settings.ToshibaAcConnection.UserName.ToLowerInvariant() + "_" + azureDeviceId!,
            MessageId = $"MB_{azureDeviceId![..Math.Min(15, azureDeviceId!.Length)].ToUpperInvariant()}-{currentMessageId % 100000000:D8}",
            TargetIds = targetIdStrings,
            TimeStamp = DateTime.UtcNow.TimeOfDay.ToString(),
            PayLoad = JsonDocument.Parse($"{{ \"data\":\"{state}\"}}").RootElement,
        };

        await using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, command, jsonOptions, Token).ConfigureAwait(false);
        memoryStream.Position = 0;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Sending Toshiba command: {Command}", Encoding.UTF8.GetString(memoryStream.ToArray()));
        }

        using var message = new Message(memoryStream);
        await azureClient.SendEventAsync(message, Token).ConfigureAwait(false);
        return command.MessageId;
    }

    private void OnAzureConnectionStatusChange(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        IsConnected = status == ConnectionStatus.Connected;
    }

    private Task<MethodResponse> HandleSmMobileMethod(MethodRequest request, object _) => Task.Run(() =>
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Received Toshiba method {MethodName}: {Data}", request.Name, request.DataAsJson);
        }

        try
        {
            var command = JsonSerializer.Deserialize<ToshibaHvacAzureSmMobileCommand>(request.Data, jsonOptions)!;
            var device = AllDevices!.SelectMany(d => d.Devices).First(d => string.Equals(d.DeviceUniqueId.ToString("D"), command.DeviceUniqueId, StringComparison.InvariantCultureIgnoreCase));

            switch (command.CommandName)
            {
                case "CMD_FCU_FROM_AC":
                    var stateData = command.PayLoad.EnumerateObject().First(o => o.Name == "data").Value.Deserialize<ToshibaHvacStateData>(jsonOptions)!;
                    device.State.UpdateStateData(stateData);
                    break;

                case "CMD_HEARTBEAT":
                    var heartbeat = command.PayLoad.Deserialize<ToshibaHvacHeartbeat>(jsonOptions)!;
                    device.State.UpdateHeartBeatData(heartbeat);
                    break;

                case "CMD_SET_SCHEDULE_FROM_AC":
                    break;
            }

            LiveDataReceived?.Invoke(this, command);
        }
        catch
        {
            return new MethodResponse(1);
        }

        return new MethodResponse(0);
    }, Token);

#if DEBUG

    private Task<MethodResponse> HandleOtherMethods(MethodRequest request, object _) => Task.Run(() =>
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Received Toshiba method {MethodName}: {Data}", request.Name, request.DataAsJson);
        }

        return new MethodResponse(0);
    }, Token);

#endif

    private async ValueTask<T> Deserialize<T>(string uri, IDictionary<string, string>? postVariables = null) where T : new()
    {
        if (azureConnection == null)
        {
            throw new InvalidDataException("No active Toshiba connection");
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new("HomeAutomationClient", FroniusGitInfo.Version.ToString()));
        client.BaseAddress = new Uri(azureConnection.BaseUrl);

        var message = new HttpRequestMessage(postVariables == null ? HttpMethod.Get : HttpMethod.Post, uri);

        if (session != null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue(session.TokenType, session.AccessToken);
        }

        if (postVariables != null)
        {
            message.Content = JsonContent.Create(postVariables);
        }

        using var response = await client.SendAsync(message, Token).ConfigureAwait(false);

#if DEBUG // This allows you to see the raw JSON string
        var jsonText = await response.Content.ReadAsStringAsync(Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Toshiba response from {Uri}: {Json}", uri, jsonText);
        }

        var jDocument = JsonDocument.Parse(jsonText);
        var result = jDocument.Deserialize<ToshibaHvacResponse<T>>(jsonOptions) ?? throw new InvalidDataException("No data");
#else
        var result = await response.Content.ReadFromJsonAsync<ToshibaHvacResponse<T>>(jsonOptions, Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");
#endif

        return !result.IsSuccess
            ? throw new InvalidDataException(result.Message)
            : result.Data;
    }
}
