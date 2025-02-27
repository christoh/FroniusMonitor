﻿// ReSharper disable RedundantUsingDirective

using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;
using De.Hochstaetter.Fronius.Models.JsonConverters;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;

// ReSharper restore RedundantUsingDirective

namespace De.Hochstaetter.Fronius.Services;

public class ToshibaHvacService(SynchronizationContext context, SettingsBase settings) : BindableBase, IToshibaHvacService
{
    private string? azureDeviceId;
    private AzureConnection? azureConnection;

    private ToshibaHvacSession? session;
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    private CancellationTokenSource? tokenSource;
    private DeviceClient? azureClient;
    private ulong messageId;// = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
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

    private CancellationToken Token => tokenSource?.Token ?? throw new WebException("Connection closed", WebExceptionStatus.ConnectionClosed);

    public bool IsRunning => tokenSource is not null;

    public bool IsConnected
    {
        get;
        private set => Set(ref field, value);
    }

    public BindableCollection<ToshibaHvacMapping>? AllDevices
    {
        get;
        private set => Set(ref field, value);
    }

    private bool isStopping;

    public async ValueTask Stop()
    {
        if (isStopping)
        {
            return;
        }

        try
        {
            isStopping = true;

            if (tokenSource != null)
            {
                await tokenSource.CancelAsync();
                tokenSource = null;
            }

            if (azureClient != null)
            {
                await azureClient.DisposeAsync().ConfigureAwait(false);
            }

            AllDevices?.Clear();
            session = null;
            azureClient = null;
        }
        finally
        {
            isStopping = false;
        }
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async ValueTask Start(AzureConnection? connection, string deviceId)
    {
        if (isStarting || connection == null)
        {
            return;
        }

        azureDeviceId = deviceId;
        azureConnection = connection;

        try
        {
            isStarting = true;
            await Stop().ConfigureAwait(false);

            try
            {
                tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

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
                await azureClient.SetReceiveMessageHandlerAsync(async (message, userContext) =>
                {
                    await azureClient.CompleteAsync(message, Token).ConfigureAwait(false);
                }, null, Token);

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
            { "Username", azureConnection!.UserName },
            { "Password", azureConnection.Password }
        };

        session = await Deserialize<ToshibaHvacSession>("/api/Consumer/Login", postData).ConfigureAwait(false)
                  ?? throw new WebException("No session data received", WebExceptionStatus.ReceiveFailure);

        postData = new Dictionary<string, string>
        {
            { "DeviceID", azureConnection.UserName.ToLower() + "_" + azureDeviceId },
            { "DeviceType", "1" },
            { "Username", azureConnection.UserName }
        };

        var azureCredentials = await Deserialize<ToshibaHvacAzureCredentials>("/api/Consumer/RegisterMobileDevice", postData).ConfigureAwait(false);

        var devices = await Deserialize<List<ToshibaHvacMapping>>($"/api/AC/GetConsumerACMapping?consumerId={session.ConsumerId}").ConfigureAwait(false);
        AllDevices = new BindableCollection<ToshibaHvacMapping>(devices, context);
        // var supi = await Deserialize<ToshibaHvacStatusDevice>($"/api/AC/GetCurrentACState?ACId={AllDevices[0].Devices[0].AcId}").ConfigureAwait(false);
        return azureCredentials;
    }

    public async ValueTask<string> SendDeviceCommand(ToshibaHvacStateData state, params string[] targetIdStrings)
    {
        if (azureConnection == null || azureClient == null || session == null || !IsRunning)
        {
            throw new IotHubCommunicationException("Not connected");
        }

        var command = new ToshibaHvacAzureSmMobileCommand
        {
            CommandName = "CMD_FCU_TO_AC",
            DeviceUniqueId = settings.ToshibaAcConnection.UserName.ToLower() + "_" + azureDeviceId!,
            MessageId = $"MB_{azureDeviceId![..Math.Min(15, azureDeviceId!.Length)].ToUpperInvariant()}-{++messageId % 10000000:D8}",
            TargetIds = targetIdStrings,
            TimeStamp = DateTime.UtcNow.TimeOfDay.ToString(),
            PayLoad = JsonDocument.Parse($"{{ \"data\":\"{state}\"}}").RootElement
        };

        await using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, command, jsonOptions, Token).ConfigureAwait(false);
        memoryStream.Position = 0;
        Debug.Print(Encoding.UTF8.GetString(memoryStream.ToArray()));
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
        Debug.Print(request.Name);
        Debug.Print(request.DataAsJson);

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
        Debug.Print(request.Name);
        Debug.Print(request.DataAsJson);
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

        using var response = (await client.SendAsync(message, Token).ConfigureAwait(false));

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