using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.Fronius.Services.HomeAutomationClient;

public sealed class WebClientService : IWebClientService
{
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(7) };

    public void Initialize(string baseUri, string productName, string version)
    {
        var address = new Uri(baseUri);
        httpClient.BaseAddress = address;
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(productName, version));
    }

    #region Identity

    public async Task<byte[]> GetKeyForUserName(string userName, CancellationToken token = default)
    {
        var keyString = await httpClient.GetStringAsync($"Identity/requestKey?user={userName}", token).ConfigureAwait(false);
        return Convert.FromBase64String(keyString);
    }

    public async Task<ProblemDetails?> Login(string userName, string password, CancellationToken token = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
        return await Get($"Identity/login?user={userName}&password={password}", token).ConfigureAwait(false);
    }

    #endregion

    #region Devices

    public Task<ApiResult<IDictionary<string, DeviceInfo>>> ListDevices(CancellationToken token = default)
    {
        return GetResult<IDictionary<string, DeviceInfo>>("Devices", token);
    }

    public Task<ApiResult<bool>> SwitchDevice(string deviceId, bool turnOn, CancellationToken token = default)
    {
        return GetResult<bool>($"Devices/{deviceId}/switch/{(turnOn ? "on" : "off")}", token);
    }

    public Task<ApiResult<bool>> SetDeviceBrightness(string deviceId, double amount, CancellationToken token = default)
    {
        return GetResult<bool>($"Devices/{deviceId}/setBrightness?amount={amount.ToString(CultureInfo.InvariantCulture)}", token);
    }

    public Task<ApiResult<bool>> SetColorTemperature(string deviceId, double temperatureKelvin, CancellationToken token = default)
    {
        return GetResult<bool>($"Devices/{deviceId}/setColorTemperature?temperatureKelvin={temperatureKelvin.ToString(CultureInfo.InvariantCulture)}", token);
    }

    public Task<ApiResult<bool>> SetHsv(string deviceId, double? hueDegrees = null, double? saturation = null, double? value = null, CancellationToken token = default)
    {
        var builder = new StringBuilder($"Devices/{deviceId}/setHsv?");

        if (hueDegrees.HasValue)
        {
            builder.Append($"hueDegrees={hueDegrees.Value.ToString(CultureInfo.InvariantCulture)}&");
        }

        if (saturation.HasValue)
        {
            builder.Append($"saturation={saturation.Value.ToString(CultureInfo.InvariantCulture)}&");
        }

        if (value.HasValue)
        {
            builder.Append($"value={value.Value.ToString(CultureInfo.InvariantCulture)}&");
        }

        builder.Remove(builder.Length - 1, 1);

        var query = builder.ToString();

        return GetResult<bool>(query, token);
    }

    #endregion

    #region Gen24

    public async Task<ApiResult<IDictionary<string, Gen24System>>> GetGen24Devices(CancellationToken token = default)
    {
        var result = await GetResult<IDictionary<string, Gen24System>>("Gen24System", token);

        if (result is { Status: HttpStatusCode.OK, Payload: not null })
        {
            result.Payload.Values.Apply(inverter =>
            {
                if (inverter.Sensors is null)
                {
                    return;
                }
                
                inverter.Sensors.PowerFlow = new Gen24PowerFlow
                {
                    GridPower = inverter.Sensors.PrimaryPowerMeter?.ActivePowerSum ?? 0,
                    StoragePower = inverter.Sensors.Inverter?.StoragePower ?? 0,
                    SolarPower = inverter.Sensors.Inverter?.SolarPowerSum ?? 0,
                    LoadPower = -(inverter.Sensors.Inverter?.PowerActiveSum ?? 0) - (inverter.Sensors.PrimaryPowerMeter?.ActivePowerSum ?? 0),
                    InverterAcPower = inverter.Sensors.Inverter?.PowerActiveSum ?? 0,
                };
            });
        }

        return result;
    }

    public Task<ApiResult<JsonElement>> GetGen24Localization(string deviceId, string iso2LanguageCode, string name, CancellationToken token = default)
    {
        return GetResult<JsonElement>(FormattableString.Invariant($"gen24system/{deviceId}/i18n/{iso2LanguageCode}/{name}"), token);
    }

    public Task<ApiResult<bool>> RequestGen24StandBy(string deviceId, bool isStandBy, CancellationToken token = default)
    {
        return GetResult<bool>(FormattableString.Invariant($"gen24system/{deviceId}/requestStandBy?isStandBy={isStandBy}"), token);
    }

    public Task<ApiResult<Gen24StandByStatus>> GetGen24StandbyStatus(string deviceId, CancellationToken token = default)
    {
        return GetResult<Gen24StandByStatus>(FormattableString.Invariant($"gen24system/{deviceId}/GetStandbyStatus"), token);
    }

    #endregion

    #region Fritzbox

    public Task<ApiResult<IDictionary<string, FritzBoxDevice>>> GetFritzBoxDevices(CancellationToken token = default)
    {
        return GetResult<IDictionary<string, FritzBoxDevice>>("FritzBoxDevice", token);
    }

    #endregion

    private async ValueTask<ProblemDetails?> Get(string queryString, CancellationToken token = default)
    {
        HttpResponseMessage? responseMessage = null;

        try
        {
            responseMessage = await httpClient.GetAsync(queryString, token).ConfigureAwait(false);

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                return await GetErrors(responseMessage, token);
            }

            return null;
        }
        catch (Exception ex)
        {
            return new ProblemDetails
            {
                Title = ex.GetType().Name,
                Detail = ex.Message,
                Status = responseMessage?.StatusCode,
            };
        }
        finally
        {
            responseMessage?.Dispose();
        }
    }

    private async Task<ApiResult<T>> GetResult<T>(string queryString, CancellationToken token = default)
    {
        HttpResponseMessage? responseMessage = null;

        try
        {
            responseMessage = await httpClient.GetAsync(queryString, token).ConfigureAwait(false);

            return responseMessage.StatusCode != HttpStatusCode.OK
                ? ApiResult<T>.FromProblemDetails(await GetErrors(responseMessage, token).ConfigureAwait(false), responseMessage.StatusCode)
                : new ApiResult<T>
                {
                    Payload = await responseMessage.Content.ReadFromJsonAsync<T>(token).ConfigureAwait(false),
                    Status = responseMessage.StatusCode,
                };
        }
        catch (Exception ex)
        {
            return ApiResult<T>.FromProblemDetails(new ProblemDetails
            {
                Title = ex.GetType().Name,
                Detail = ex.Message,
                Status = responseMessage?.StatusCode,
                Errors = new Dictionary<string, List<string>> { { "Errors", [ex.Message] } },
            }, responseMessage?.StatusCode, ex);
        }
        finally
        {
            responseMessage?.Dispose();
        }
    }

    private static async ValueTask<ProblemDetails?> GetErrors(HttpResponseMessage message, CancellationToken token)
    {
        return await message.Content.ReadFromJsonAsync<ProblemDetails?>(token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
