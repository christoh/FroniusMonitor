using System.Net.Http.Headers;
using System.Text.Json;
using De.Hochstaetter.Fronius.Models.JsonConverters;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace De.Hochstaetter.Fronius.Services;

public class ToshibaAirConditionService : BindableBase, IToshibaAirConditionService
{
    private readonly SynchronizationContext context;

    private ToshibaAcSession? session;
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    private CancellationTokenSource? tokenSource;
    private DeviceClient? azureClient;
    private ulong messageId = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));

    static ToshibaAirConditionService()
    {
        // jsonOptions.Converters.Add(new ToshibaDateTimeConverter());
        jsonOptions.Converters.Add(new ToshibaHexConverter<int>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<byte>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<sbyte>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ushort>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaAcOperatingMode>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaAcFanSpeed>());
        jsonOptions.Converters.Add(new ToshibaHexConverter<ToshibaAcPowerState>());
        jsonOptions.Converters.Add(new ToshibaStateDataConverter());
        #if DEBUG
        jsonOptions.WriteIndented = true;
        #endif
    }

    public ToshibaAirConditionService(SynchronizationContext context)
    {
        this.context = context;
    }

    public SettingsBase Settings => IoC.Get<SettingsBase>();

    private CancellationToken Token => tokenSource?.Token ?? throw new WebException("Connection closed", WebExceptionStatus.ConnectionClosed);

    public bool IsRunning => tokenSource is not null;

    private bool isConnected;

    public bool IsConnected
    {
        get => isConnected;
        private set => Set(ref isConnected, value);
    }

    private BindableCollection<ToshibaAcMapping>? allDevices;

    public BindableCollection<ToshibaAcMapping>? AllDevices
    {
        get => allDevices;
        private set => Set(ref allDevices, value);
    }

    public async ValueTask Stop()
    {
        tokenSource?.Cancel();
        tokenSource = null;

        if (azureClient != null)
        {
            await azureClient.DisposeAsync().ConfigureAwait(false);

        }

        AllDevices?.Clear();
        session = null;
        azureClient = null;
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async ValueTask Start()
    {
        var settings = Settings;

        if (!settings.HaveToshibaAc || !settings.ShowToshibaAc || settings.ToshibaAcConnection == null)
        {
            return;
        }

        await Stop().ConfigureAwait(false);

        try
        {
            tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var postData = new Dictionary<string, string>
            {
                {"Username", settings.ToshibaAcConnection.UserName},
                {"Password", settings.ToshibaAcConnection.Password},
            };

            session = await Deserialize<ToshibaAcSession>("/api/Consumer/Login", postData).ConfigureAwait(false)
                      ?? throw new WebException("No session data received", WebExceptionStatus.ReceiveFailure);

            postData = new Dictionary<string, string>
            {
                {"DeviceID", $"FroniusMonitor:{Guid.NewGuid():D}"},
                {"DeviceType", "1"},
                {"Username", settings.ToshibaAcConnection!.UserName},
            };

            var azureCredentials = await Deserialize<ToshibaAcAzureCredentials>("/api/Consumer/RegisterMobileDevice", postData).ConfigureAwait(false);
            var auth = AuthenticationMethodFactory.CreateAuthenticationWithToken(azureCredentials.DeviceId, azureCredentials.SasToken);
            azureClient = DeviceClient.Create(azureCredentials.HostName, auth, TransportType.Amqp);

            var devices = await Deserialize<List<ToshibaAcMapping>>($"/api/AC/GetConsumerACMapping?consumerId={session.ConsumerId}").ConfigureAwait(false);
            AllDevices = new BindableCollection<ToshibaAcMapping>(devices, context);

            azureClient.SetConnectionStatusChangesHandler(OnAzureConnectionStatusChange);
            await azureClient.OpenAsync(Token).ConfigureAwait(false);
            await azureClient.SetMethodHandlerAsync("smmobile", HandleSmMobileMethod, null, Token).ConfigureAwait(false);

            #if DEBUG

            await azureClient.SetMethodDefaultHandlerAsync(HandleOtherMethods, null, Token).ConfigureAwait(false);

            #endif

            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource();
        }
        catch
        {
            await Stop().ConfigureAwait(false);
        }
    }

    public async ValueTask SendDeviceCommand(ToshibaAcStateData state, params string[] targetIdStrings)
    {
        var settings = Settings;

        if (settings.ToshibaAcConnection == null || azureClient == null || session == null || !IsRunning)
        {
            throw new IotHubCommunicationException("Not connected");
        }

        var command = new ToshibaAcAzureSmMobileCommand
        {
            CommandName = "CMD_FCU_TO_AC",
            DeviceUniqueId = settings.ToshibaAcConnection.UserName + ":" + session.ConsumerId.ToString("D"),
            MessageId = $"{++messageId:x}",
            TargetIds = targetIdStrings,
            TimeStamp = $"{(DateTime.UtcNow - DateTime.UnixEpoch).Ticks:x}",
            PayLoad = JsonDocument.Parse($"{{ \"data\":\"{state}\"}}").RootElement
        };

        var commandString = JsonSerializer.Serialize(command, jsonOptions);
        Debug.Print(commandString);
        await azureClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(commandString)), Token).ConfigureAwait(false);
    }

    private void OnAzureConnectionStatusChange(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        IsConnected = status == ConnectionStatus.Connected;
    }

    private Task<MethodResponse> HandleSmMobileMethod(MethodRequest request, object _) => Task.Run(() =>
    {
        Debug.Print(request.Name);
        Debug.Print(request.DataAsJson);

        try
        {
            var command = JsonSerializer.Deserialize<ToshibaAcAzureSmMobileCommand>(request.Data, jsonOptions)!;
            var device = AllDevices!.SelectMany(d => d.Devices).First(d => d.DeviceUniqueId.ToString("D") == command.DeviceUniqueId.ToLowerInvariant());

            switch (command.CommandName)
            {
                case "CMD_FCU_FROM_AC":
                    var stateData = command.PayLoad.EnumerateObject().First(o => o.Name == "data").Value.Deserialize<ToshibaAcStateData>(jsonOptions)!;
                    device.State.UpdateStateData(stateData);
                    break;

                case "CMD_HEARTBEAT":
                    var heartbeat = command.PayLoad.Deserialize<ToshibaAcHeartbeat>(jsonOptions)!;
                    device.State.UpdateHeartBeatData(heartbeat);
                    break;

                case "CMD_SET_SCHEDULE_FROM_AC":
                    break;

                default:
                    break;
            }
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
        Debug.Print(request.Name);
        Debug.Print(request.DataAsJson);
        return new MethodResponse(0);
    }, Token);

    #endif

    private async ValueTask<T> Deserialize<T>(string uri, IEnumerable<KeyValuePair<string, string>>? postVariables = null) where T : new()
    {
        var settings = IoC.Get<SettingsBase>();

        if (!settings.HaveToshibaAc || settings.ToshibaAcConnection == null)
        {
            throw new InvalidDataException("No active Toshiba connection");
        }

        using var client = new HttpClient();
        client.BaseAddress = new Uri(settings.ToshibaAcConnection!.BaseUrl);

        var message = new HttpRequestMessage(postVariables == null ? HttpMethod.Get : HttpMethod.Post, uri);

        if (session != null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue(session.TokenType, session.AccessToken);
        }

        if (postVariables != null)
        {
            message.Content = new FormUrlEncodedContent(postVariables);
        }

        using var response = (await client.SendAsync(message, Token).ConfigureAwait(false)).EnsureSuccessStatusCode();

        #if DEBUG // This allows you to see the raw JSON string
        var jsonText = await response.Content.ReadAsStringAsync(Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");
        Debug.Print(jsonText);
        var jDocument = JsonDocument.Parse(jsonText);
        var result = jDocument.Deserialize<ToshibaAcResponse<T>>(jsonOptions) ?? throw new InvalidDataException("No data");

        #else
        var result = await response.Content.ReadFromJsonAsync<ToshibaAcResponse<T>>(jsonOptions, Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");

        #endif

        if (!result.IsSuccess)
        {
            throw new InvalidDataException(result.Message);
        }

        return result.Data;
    }
}
