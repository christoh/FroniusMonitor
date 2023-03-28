using System.Net.Http.Headers;
using System.Text.Json;
using De.Hochstaetter.Fronius.Models.JsonConverters;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
// ReSharper disable RedundantUsingDirective
using System.Net.Http.Json;
// ReSharper restore RedundantUsingDirective

namespace De.Hochstaetter.Fronius.Services;

public class ToshibaHvacService : BindableBase, IToshibaHvacService
{
    private readonly SynchronizationContext context;
    private readonly SettingsBase settings;

    private ToshibaHvacSession? session;
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    private CancellationTokenSource? tokenSource;
    private DeviceClient? azureClient;
    private ulong messageId = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
    private readonly Guid deviceId = Guid.NewGuid();
    private bool isStarting;

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

    public ToshibaHvacService(SynchronizationContext context, SettingsBase settings)
    {
        this.context = context;
        this.settings = settings;
    }

    private CancellationToken Token => tokenSource?.Token ?? throw new WebException("Connection closed", WebExceptionStatus.ConnectionClosed);

    public bool IsRunning => tokenSource is not null;

    private bool isConnected;

    public bool IsConnected
    {
        get => isConnected;
        private set => Set(ref isConnected, value);
    }

    private BindableCollection<ToshibaHvacMapping>? allDevices;

    public BindableCollection<ToshibaHvacMapping>? AllDevices
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
        if (isStarting || !settings.HaveToshibaAc || !settings.ShowToshibaAc || settings.ToshibaAcConnection == null)
        {
            return;
        }

        try
        {
            isStarting = true;
            await Stop().ConfigureAwait(false);

            try
            {
                tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                var azureCredentials = await RefreshAll().ConfigureAwait(false);
                var auth = AuthenticationMethodFactory.CreateAuthenticationWithToken(azureCredentials.DeviceId, azureCredentials.SasToken);
                azureClient = DeviceClient.Create(azureCredentials.HostName, auth, settings.ToshibaAcConnection.TransportType);
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
        finally
        {
            isStarting = false;
        }
    }

    private async ValueTask<ToshibaHvacAzureCredentials> RefreshAll()
    {
        var postData = new Dictionary<string, string>
        {
            {"Username", settings.ToshibaAcConnection!.UserName},
            {"Password", settings.ToshibaAcConnection.Password},
        };

        session = await Deserialize<ToshibaHvacSession>("/api/Consumer/Login", postData).ConfigureAwait(false)
                  ?? throw new WebException("No session data received", WebExceptionStatus.ReceiveFailure);

        postData = new Dictionary<string, string>
        {
            {"DeviceID", $"{deviceId:D}"},
            {"DeviceType", "1"},
            {"Username", settings.ToshibaAcConnection.UserName},
        };

        var azureCredentials = await Deserialize<ToshibaHvacAzureCredentials>("/api/Consumer/RegisterMobileDevice", postData).ConfigureAwait(false);

        var devices = await Deserialize<List<ToshibaHvacMapping>>($"/api/AC/GetConsumerACMapping?consumerId={session.ConsumerId}").ConfigureAwait(false);
        AllDevices = new BindableCollection<ToshibaHvacMapping>(devices, context);
        return azureCredentials;
    }

    public async ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings)
    {
        if (settings.ToshibaAcConnection == null || azureClient == null || session == null || !IsRunning)
        {
            throw new IotHubCommunicationException("Not connected");
        }

        var command = new ToshibaHvacAzureSmMobileCommand
        {
            CommandName = "CMD_FCU_TO_AC",
            DeviceUniqueId = $"{deviceId:D}",
            MessageId = $"{++messageId:x}",
            TargetIds = targetIdStrings,
            TimeStamp = $"{(DateTime.UtcNow - DateTime.UnixEpoch).Ticks:x}",
            PayLoad = JsonDocument.Parse($"{{ \"data\":\"{state}\"}}").RootElement
        };

        var commandString = JsonSerializer.Serialize(command, jsonOptions);
        Debug.Print(commandString);
        await azureClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(commandString)), Token).ConfigureAwait(false);
        return command.MessageId;
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
            var command = JsonSerializer.Deserialize<ToshibaHvacAzureSmMobileCommand>(request.Data, jsonOptions)!;
            var device = AllDevices!.SelectMany(d => d.Devices).First(d => d.DeviceUniqueId.ToString("D") == command.DeviceUniqueId.ToLowerInvariant());

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
        Debug.Print(request.Name);
        Debug.Print(request.DataAsJson);
        return new MethodResponse(0);
    }, Token);

#endif

    private async ValueTask<T> Deserialize<T>(string uri, IEnumerable<KeyValuePair<string, string>>? postVariables = null) where T : new()
    {
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
        var result = jDocument.Deserialize<ToshibaHvacResponse<T>>(jsonOptions) ?? throw new InvalidDataException("No data");

#else
        var result = await response.Content.ReadFromJsonAsync<ToshibaHvacResponse<T>>(jsonOptions, Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");

#endif

        if (!result.IsSuccess)
        {
            throw new InvalidDataException(result.Message);
        }

        return result.Data;
    }
}
