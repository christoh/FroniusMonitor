// ReSharper disable once RedundantUsingDirective
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;
using De.Hochstaetter.Fronius.Models.JsonConverters;
using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.Fronius.Services;

public class ToshibaAirConditionService : BindableBase, IToshibaAirConditionService
{
    private ToshibaAcSession? session;
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    private CancellationTokenSource? tokenSource;
    private event EventHandler<ToshibaAcDataReceivedEventArgs>? NewDataReceived;

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
        jsonOptions.WriteIndented = true;
    }

    public SettingsBase Settings => IoC.Get<SettingsBase>();

    private CancellationToken Token => tokenSource?.Token ?? throw new WebException("Connection closed", WebExceptionStatus.ConnectionClosed);

    public bool IsRunning => tokenSource != null;

    private ObservableCollection<ToshibaAcMapping>? allDevices;

    public ObservableCollection<ToshibaAcMapping>? AllDevices
    {
        get => allDevices;
        set => Set(ref allDevices, value);
    }

    public void Stop()
    {
        tokenSource?.Cancel();
    }

    public async ValueTask Start()
    {
        var settings = Settings;

        if (!settings.HaveToshibaAc||!settings.ShowToshibaAc || settings.ToshibaAcConnection == null)
        {
            return;
        }

        Stop();

        tokenSource = new CancellationTokenSource();

        session = await GetJsonResult<ToshibaAcSession>("/api/Consumer/Login", new Dictionary<string, string>
        {
            {"Username", settings.ToshibaAcConnection.UserName},
            {"Password", settings.ToshibaAcConnection.Password},
        }).ConfigureAwait(false);

        new Thread(UpdateThread).Start();
    }

    private async void UpdateThread()
    {
        try
        {
            while (true)
            {
                var settings = IoC.Get<SettingsBase>();

                if (!settings.HaveToshibaAc || !settings.ShowToshibaAc)
                {
                    tokenSource?.Cancel();
                }

                AllDevices = await GetJsonResult<ObservableCollection<ToshibaAcMapping>>($"/api/AC/GetConsumerACMapping?consumerId={session!.ConsumerId}").ConfigureAwait(false);
                //var supi = await GetJsonResult<ToshibaAcStatusDevice>($"/api/AC/GetCurrentACState?ACId={AllDevices[0].Devices[0].AcId}").ConfigureAwait(false);
                NewDataReceived?.Invoke(this, new ToshibaAcDataReceivedEventArgs(AllDevices));
                await Task.Delay(TimeSpan.FromSeconds(settings.ToshibaAcUpdateRate), Token).ConfigureAwait(false);
            }
        }
        catch
        {
            tokenSource?.Dispose();
            tokenSource = null;
        }
    }

    private async ValueTask<T> GetJsonResult<T>(string uri, IEnumerable<KeyValuePair<string, string>>? postVariables = null) where T : new()
    {
        var settings = IoC.Get<SettingsBase>();
        EnsureConnectionParameters(settings);
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
        #if DEBUG
        var jsonText = await response.Content.ReadAsStringAsync(Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");
        var jDocument = JsonDocument.Parse(jsonText);
        Token.ThrowIfCancellationRequested();
        var result = jDocument.Deserialize<ToshibaAcResponse<T>>(jsonOptions) ?? throw new InvalidDataException("No data");
        Token.ThrowIfCancellationRequested();
        #else
        var result = await response.Content.ReadFromJsonAsync<ToshibaAcResponse<T>>(jsonOptions, Token).ConfigureAwait(false) ?? throw new InvalidDataException("No data");
        #endif

        if (!result.IsSuccess)
        {
            throw new InvalidDataException(result.Message);
        }

        return result.Data;
    }

    private static void EnsureConnectionParameters(SettingsBase settings)
    {
        if (!settings.HaveToshibaAc || settings.ToshibaAcConnection == null)
        {
            throw new InvalidDataException("No active Toshiba connection");
        }
    }
}
