using System.Net.Http.Json;
using System.Text.Json;
using De.Hochstaetter.Fronius.Models.HomeAutomationClient;
using De.Hochstaetter.Fronius.Models.WebApi;

namespace De.Hochstaetter.Fronius.Services.HomeAutomationClient;

public sealed class WebClientService : IWebClientService
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyProperties = true,
        IgnoreReadOnlyFields = true,
        IncludeFields = false,
    };

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

    public Task<ApiResult<UserInfo>> Login(string userName, string password, CancellationToken token = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
        return GetResult<UserInfo>($"Identity/login?user={userName}&password={password}", token);
    }

    public async Task<string?> GetHubTicket(CancellationToken token = default)
    {
        using var response = await httpClient.GetAsync("Identity/hubTicket", token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
    }

    public Task<ApiResult<List<UserInfo>>> GetUsers(CancellationToken token = default)
    {
        return GetResult<List<UserInfo>>("Identity/users", token);
    }

    public Task<ApiResult<UserInfo>> AddUser(UserAccount account, CancellationToken token = default)
    {
        return PostResult<UserInfo, UserAccount>("Identity/users", account, token);
    }

    public Task<ApiResult<UserInfo>> UpdateUser(string userName, UserAccount account, CancellationToken token = default)
    {
        return PutResult<UserInfo, UserAccount>($"Identity/users/{Uri.EscapeDataString(userName)}", account, token);
    }

    public Task<ApiResult<bool>> DeleteUser(string userName, CancellationToken token = default)
    {
        return DeleteResult<bool>($"Identity/users/{Uri.EscapeDataString(userName)}", token);
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

    #region WattPilot
    public async Task<ApiResult<Dictionary<string, WattPilot>>> GetWattPilots(CancellationToken token = default)
    {
        var result = await GetResult<Dictionary<string, WattPilot>>("WattPilot", token);
        return result;
    }

    #endregion

    #region ToshibaHvac

    public Task<ApiResult<Dictionary<string, ToshibaHvacMappingDevice>>> GetToshibaHvacDevices(CancellationToken token = default) =>
        GetResult<Dictionary<string, ToshibaHvacMappingDevice>>("ToshibaHvac", token);

    #endregion

    #region Gen24

    public async Task<ApiResult<Dictionary<string, Gen24System>>> GetGen24Devices(CancellationToken token = default)
    {
        var result = await GetResult<Dictionary<string, Gen24System>>("Gen24System", token);

        if (result is { Status: HttpStatusCode.OK, Payload: not null })
        {
            result.Payload.Values.Apply(inverter =>
            {
                if (inverter.Sensors is null)
                {
                    return;
                }

                inverter.Sensors.GeneratePowerFlow();
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

    public Task<ApiResult<Gen24SettingsSnapshot>> GetGen24Settings(string deviceId, CancellationToken token = default)
    {
        return GetResult<Gen24SettingsSnapshot>(FormattableString.Invariant($"gen24system/{deviceId}/settings"), token);
    }

    public Task<ApiResult<List<Gen24Event>>> GetGen24Events(string deviceId, CancellationToken token = default)
    {
        return GetResult<List<Gen24Event>>(FormattableString.Invariant($"gen24system/{deviceId}/events"), token);
    }

    public Task<ApiResult<bool>> SetGen24ModbusSettings(string deviceId, Gen24ModbusSettings settings, CancellationToken token = default)
    {
        return PutResult<bool, Gen24ModbusSettings>(FormattableString.Invariant($"gen24system/{deviceId}/settings/modbus"), settings, token);
    }

    public Task<ApiResult<bool>> SetGen24BatterySettings(string deviceId, Gen24BatterySettings settings, CancellationToken token = default)
    {
        return PutResult<bool, Gen24BatterySettings>(FormattableString.Invariant($"gen24system/{deviceId}/settings/batteries"), settings, token);
    }

    public Task<ApiResult<bool>> SetGen24TimeOfUse(string deviceId, IEnumerable<Gen24ChargingRule> rules, CancellationToken token = default)
    {
        return PutResult<bool, List<Gen24ChargingRule>>(FormattableString.Invariant($"gen24system/{deviceId}/settings/timeOfUse"), [.. rules], token);
    }

    public Task<ApiResult<bool>> SetGen24CommonSettings(string deviceId, Gen24InverterSettings settings, CancellationToken token = default)
    {
        return PutResult<bool, Gen24InverterSettings>(FormattableString.Invariant($"gen24system/{deviceId}/settings/common"), settings, token);
    }

    public Task<ApiResult<bool>> SetGen24MpptSettings(string deviceId, Gen24Mppt mppt, CancellationToken token = default)
    {
        return PutResult<bool, Gen24Mppt>(FormattableString.Invariant($"gen24system/{deviceId}/settings/mppt"), mppt, token);
    }

    public Task<ApiResult<bool>> SetGen24PowerLimits(string deviceId, Gen24PowerLimitSettings powerLimits, CancellationToken token = default)
    {
        return PutResult<bool, Gen24PowerLimitSettings>(FormattableString.Invariant($"gen24system/{deviceId}/settings/powerLimits"), powerLimits, token);
    }

    #endregion

    #region Fritzbox

    public Task<ApiResult<IDictionary<string, FritzBoxDevice>>> GetFritzBoxDevices(CancellationToken token = default)
    {
        return GetResult<IDictionary<string, FritzBoxDevice>>("FritzBoxDevice", token);
    }

    #endregion

    private Task<ApiResult<T>> GetResult<T>(string queryString, CancellationToken token = default)
    {
        return SendResult<T>(t => httpClient.GetAsync(queryString, t), token);
    }

    /// <summary>
    /// The counterpart of <see cref="GetResult{T}"/> for the endpoints that change something. The body goes out as
    /// JSON with the same options the rest of the API uses, so the server sees the very shape these models have.
    /// </summary>
    private Task<ApiResult<TResult>> PutResult<TResult, TBody>(string queryString, TBody body, CancellationToken token = default)
    {
        return SendResult<TResult>(t => httpClient.PutAsJsonAsync(queryString, body, jsonOptions, t), token);
    }

    /// <inheritdoc cref="PutResult{TResult,TBody}"/>
    private Task<ApiResult<TResult>> PostResult<TResult, TBody>(string queryString, TBody body, CancellationToken token = default)
    {
        return SendResult<TResult>(t => httpClient.PostAsJsonAsync(queryString, body, jsonOptions, t), token);
    }

    private Task<ApiResult<T>> DeleteResult<T>(string queryString, CancellationToken token = default)
    {
        return SendResult<T>(t => httpClient.DeleteAsync(queryString, t), token);
    }

    private async Task<ApiResult<T>> SendResult<T>(Func<CancellationToken, Task<HttpResponseMessage>> send, CancellationToken token)
    {
        HttpResponseMessage? responseMessage = null;

        try
        {
            responseMessage = await send(token).ConfigureAwait(false);

            return responseMessage.StatusCode != HttpStatusCode.OK
                ? ApiResult<T>.FromProblemDetails(await GetErrors(responseMessage, token).ConfigureAwait(false), responseMessage.StatusCode)
                : new ApiResult<T>
                {
                    Payload = await responseMessage.Content.ReadFromJsonAsync<T>(jsonOptions, token).ConfigureAwait(false),
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
        return await message.Content.ReadFromJsonAsync<ProblemDetails?>(jsonOptions, token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
