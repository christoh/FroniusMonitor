using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24.Commands;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace De.Hochstaetter.HomeAutomationClient.Services;

/// <summary>
/// The client side of the web API.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Login"/> exchanges the user name and password for a bearer token, which every later request carries.
/// The token is renewed <see cref="RenewalLead"/> before it expires. Where the server refuses it all the same - it
/// was restarted and has forgotten every token, or renewing failed until the token ran out - the service logs in
/// again with the password it was given and repeats the request, once. The callers never see a token.
/// </para>
/// <para>
/// The password is therefore kept in memory for as long as the session lasts. It is what makes a restart of the
/// server invisible to the user, and it is dropped by <see cref="Logout"/>, by a login the server refuses and by
/// <see cref="Initialize"/>.
/// </para>
/// </remarks>
public sealed class WebClientService : IWebClientService
{
    /// <summary>
    /// How long before a bearer token expires a new one is asked for. A token that lives for less than twice this
    /// is renewed when half of its life is over instead, so a short lifetime cannot turn into a busy loop.
    /// </summary>
    public static readonly TimeSpan RenewalLead = TimeSpan.FromMinutes(2);

    /// <summary>How soon renewing a token is tried again after the server could not be reached.</summary>
    public static readonly TimeSpan RenewalRetryInterval = TimeSpan.FromSeconds(30);

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

    private readonly ILogger<WebClientService> logger;
    private readonly TimeProvider clock;

    /// <summary>
    /// Held while the session changes hands: a login, a renewal, a login again after a refusal. Requests that are
    /// refused at the same time then wait for one login instead of each starting one of their own.
    /// </summary>
    private readonly SemaphoreSlim sessionLock = new(1, 1);

    private HttpClient httpClient;

    /// <summary>The bearer token every request carries, or <see langword="null"/> while nobody is logged in.</summary>
    private volatile string? accessToken;

    /// <summary>When <see cref="accessToken"/> runs out, by the clock of this device.</summary>
    private DateTimeOffset accessTokenExpiry;

    /// <summary>What <see cref="accessToken"/> was obtained with, to log in again where the server has forgotten it.</summary>
    private LoginRequest? credentials;

    private ITimer? renewalTimer;

    /// <summary>The token requests go out with, for the tests to tell a renewed one from the one before.</summary>
    internal string? AccessToken => accessToken;

    public WebClientService(ILogger<WebClientService>? logger = null, TimeProvider? clock = null)
    {
        this.logger = logger ?? NullLogger<WebClientService>.Instance;
        this.clock = clock ?? TimeProvider.System;
        httpClient = NewHttpClient();
    }

    /// <summary>How long a request may take unless the caller says otherwise. The server answers from its own memory or its devices within that.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long a Solar.web request may take: the server may have to ask Solar.web, and log in to it first. The
    /// developer's choice of 2026-09-20; the server itself waits longer, and a chart that takes longer than this is
    /// served from the cache the next time it is asked for.
    /// </summary>
    public static readonly TimeSpan SolarWebTimeout = TimeSpan.FromSeconds(30);

    // No timeout on the client itself: every request gets its own through ReadResult, so one slow endpoint does not
    // decide the patience for all the others.
    private HttpClient NewHttpClient() => new(new BearerTokenHandler(this)) { Timeout = Timeout.InfiniteTimeSpan };

    /// <summary>
    /// Points the client at <paramref name="baseUri"/>. May be called again when the user changes the connection.
    /// </summary>
    /// <remarks>
    /// An <see cref="HttpClient"/> refuses a new <see cref="HttpClient.BaseAddress"/> once it has sent its first
    /// request, so a second call gets a new one. The session ends as well: credentials for the previous server are
    /// worth nothing to the new one.
    /// </remarks>
    public void Initialize(string baseUri, string productName, string version)
    {
        var address = new Uri(baseUri);
        EndSession();

        if (httpClient.BaseAddress != null)
        {
            httpClient.Dispose();
            httpClient = NewHttpClient();
        }

        httpClient.BaseAddress = address;
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(productName, version));
    }

    #region Identity

    public async Task<ApiResult<byte[]>> GetKeyForUserName(string userName, CancellationToken token = default)
    {
        // Not SendResult: the endpoint answers with the key as text/plain, not as JSON.
        return await ReadResult(t => httpClient.GetAsync($"Identity/requestKey?user={Uri.EscapeDataString(userName)}", t),
            async (content, t) => Convert.FromBase64String(await content.ReadAsStringAsync(t).ConfigureAwait(false)), token).ConfigureAwait(false);
    }

    public async Task<ApiResult<UserInfo>> Login(string userName, string password, CancellationToken token = default)
    {
        await sessionLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            return await LoginCore(new LoginRequest { UserName = userName, Password = password }, token).ConfigureAwait(false);
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public async Task<string?> GetHubTicket(CancellationToken token = default)
    {
        using var response = await SendAuthenticated(t => httpClient.GetAsync("Identity/hubTicket", t), token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
    }

    public Task<ApiResult<Uri>> GetBrowserTabUri(string path, CancellationToken token = default) => ReadResult
    (
        t => SendAuthenticated(t2 => httpClient.PostAsync("Identity/tabTicket", null, t2), t),
        async (content, t) =>
        {
            var ticket = await content.ReadAsStringAsync(t).ConfigureAwait(false);
            // "../" against .../api/ is the root of the server, which path is relative to.
            var root = new Uri(httpClient.BaseAddress!, "../");
            return new Uri(root, $"{path}?{BrowserTabTicket.QueryParameter}={Uri.EscapeDataString(ticket)}");
        },
        token
    );

    public async Task<ApiResult<bool>> Logout(CancellationToken token = default)
    {
        // Still with the token, which is what the server revokes.
        var result = await GetResult<bool>("Identity/logout", token).ConfigureAwait(false);

        // Ended regardless of what the server answered: a server that could not be reached is not a reason to go on
        // holding a password that the caller has just decided to forget.
        await sessionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            EndSession();
        }
        finally
        {
            sessionLock.Release();
        }

        return result;
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

    public Task<ApiResult<bool>> ChangePassword(ChangePasswordRequest request, CancellationToken token = default)
    {
        return PutResult<bool, ChangePasswordRequest>("Identity/password", request, token);
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

    #region EnergyData

    public Task<ApiResult<EnergyChartData>> GetEnergyData(CancellationToken token = default) => GetResult<EnergyChartData>("EnergyData", token);

    public Task<ApiResult<EnergyChartData>> GetEnergyData(DateOnly day, CancellationToken token = default) =>
        GetResult<EnergyChartData>(FormattableString.Invariant($"EnergyData/{day:yyyy-MM-dd}"), token);

    #endregion

    #region SolarWeb

    public Task<ApiResult<SolarWebChart>> GetSolarWebChart(SolarWebInterval interval, SolarWebView view, DateOnly date, CancellationToken token = default) =>
        GetResult<SolarWebChart>(FormattableString.Invariant($"SolarWeb/{interval.ToQueryValue()}/{view.ToQueryValue()}/{date:yyyy-MM-dd}"), token, SolarWebTimeout);

    public Task<ApiResult<SolarWebFirmwareStatus>> GetSolarWebFirmwareStatus(CancellationToken token = default) => GetResult<SolarWebFirmwareStatus>("SolarWeb/firmware", token, SolarWebTimeout);

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

    #region Session

    /// <summary>
    /// Logs in and, where the server agrees, makes the answer the session. Where the server refuses the password,
    /// the session there was ends: a caller that has just failed to log in no longer knows who it is. Where the
    /// server cannot be reached, it is left as it was, so that the password is still there to log in with once the
    /// server is back. Only while holding <see cref="sessionLock"/>.
    /// </summary>
    private async Task<ApiResult<UserInfo>> LoginCore(LoginRequest request, CancellationToken token)
    {
        var result = await ReadResult<LoginResponse>
        (
            t => httpClient.PostAsJsonAsync("Identity/login", request, jsonOptions, t),
            (content, t) => content.ReadFromJsonAsync<LoginResponse>(jsonOptions, t),
            token
        ).ConfigureAwait(false);

        if (result is not { Status: HttpStatusCode.OK, Payload: { } response })
        {
            if (result.Status == HttpStatusCode.Unauthorized)
            {
                EndSession();
            }

            return ApiResult<UserInfo>.FromProblemDetails(result, result.Status, result.Exception);
        }

        StartSession(response, request);

        // Only who logged in: the token is this service's business and nobody else's.
        return new ApiResult<UserInfo>
        {
            Payload = new UserInfo { UserName = response.UserName, Roles = response.Roles },
            Status = result.Status,
        };
    }

    /// <summary>Takes the token of <paramref name="response"/> and arranges for its renewal. Only while holding <see cref="sessionLock"/>.</summary>
    private void StartSession(LoginResponse response, LoginRequest request)
    {
        var lifetime = TimeSpan.FromSeconds(Math.Max(1, response.ExpiresInSeconds));
        accessToken = response.AccessToken;
        accessTokenExpiry = clock.GetUtcNow() + lifetime;
        credentials = request;
        ScheduleRenewal(lifetime > RenewalLead * 2 ? lifetime - RenewalLead : lifetime / 2);
    }

    /// <summary>Forgets the token and the password and stops renewing.</summary>
    private void EndSession()
    {
        renewalTimer?.Dispose();
        renewalTimer = null;
        accessToken = null;
        credentials = null;
    }

    private void ScheduleRenewal(TimeSpan dueIn)
    {
        renewalTimer?.Dispose();
        // Discarded, because a timer cannot await. That is only allowed for a call that cannot throw, and
        // RenewTokenAsync cannot: it catches and logs everything itself, there being nobody else to report to.
        renewalTimer = clock.CreateTimer(_ => _ = RenewTokenAsync(), null, dueIn, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Swaps the token for a new one before it runs out. Where the server has forgotten it, logs in again with the
    /// password instead; where the server cannot be reached, tries again after <see cref="RenewalRetryInterval"/>
    /// for as long as the token is still valid - after that, the next request logs in again by itself.
    /// </summary>
    /// <remarks>Never throws: it runs off a timer, where an exception would have nobody to go to.</remarks>
    internal async Task RenewTokenAsync(CancellationToken token = default)
    {
        try
        {
            await sessionLock.WaitAsync(token).ConfigureAwait(false);

            try
            {
                if (accessToken == null || credentials is not { } request)
                {
                    // Logged out while the timer was on its way.
                    return;
                }

                var result = await ReadResult<LoginResponse>
                (
                    t => httpClient.PostAsync("Identity/token", null, t),
                    (content, t) => content.ReadFromJsonAsync<LoginResponse>(jsonOptions, t),
                    token
                ).ConfigureAwait(false);

                if (result is { Status: HttpStatusCode.OK, Payload: { } response })
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("The bearer token was renewed, valid for {Seconds} seconds", response.ExpiresInSeconds);
                    }

                    StartSession(response, request);
                    return;
                }

                if (result.Status == HttpStatusCode.Unauthorized)
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("The server refused to renew the bearer token; logging in again as {Username}", request.UserName);
                    }

                    var login = await LoginCore(request, token).ConfigureAwait(false);

                    if (login.Status != HttpStatusCode.OK && logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Logging in again as {Username} failed with {Status}: {Detail}", request.UserName, login.Status, login.Detail);
                    }

                    return;
                }

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("The bearer token could not be renewed ({Status}: {Detail})", result.Status, result.Detail);
                }

                if (clock.GetUtcNow() + RenewalRetryInterval < accessTokenExpiry)
                {
                    ScheduleRenewal(RenewalRetryInterval);
                }
            }
            finally
            {
                sessionLock.Release();
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Renewing the bearer token failed");
            }
        }
    }

    /// <summary>
    /// Sends a request with the bearer token and, where the server refuses it with 401, logs in again with the
    /// password and sends it once more. That is how a restart of the server passes unnoticed: it forgets every
    /// token it has issued.
    /// </summary>
    /// <param name="send">Called a second time for the repetition, so it has to build its request anew each time.</param>
    private async Task<HttpResponseMessage> SendAuthenticated(Func<CancellationToken, Task<HttpResponseMessage>> send, CancellationToken token)
    {
        var tokenSent = accessToken;
        var response = await send(token).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized || tokenSent == null || !await LoginAgain(tokenSent, token).ConfigureAwait(false))
        {
            return response;
        }

        response.Dispose();
        return await send(token).ConfigureAwait(false);
    }

    /// <summary>
    /// Logs in again after <paramref name="refusedToken"/> was refused. Where another request has already done so
    /// in the meantime, its token is taken instead of logging in a second time.
    /// </summary>
    /// <returns>Whether there is a new token worth repeating the request with.</returns>
    private async Task<bool> LoginAgain(string refusedToken, CancellationToken token)
    {
        await sessionLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            if (accessToken != refusedToken)
            {
                return accessToken != null;
            }

            if (credentials is not { } request)
            {
                return false;
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("The server refused the bearer token, probably because it was restarted; logging in again as {Username}", request.UserName);
            }

            var login = await LoginCore(request, token).ConfigureAwait(false);

            if (login.Status != HttpStatusCode.OK && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Logging in again as {Username} failed with {Status}: {Detail}", request.UserName, login.Status, login.Detail);
            }

            return login.Status == HttpStatusCode.OK;
        }
        finally
        {
            sessionLock.Release();
        }
    }

    /// <summary>
    /// Puts the current bearer token on every request, at the moment it is sent. Not
    /// <see cref="HttpClient.DefaultRequestHeaders"/>: those must not change while requests are going out, and the
    /// token is renewed off a timer, in the middle of whatever else is happening.
    /// </summary>
    private sealed class BearerTokenHandler(WebClientService owner) : DelegatingHandler(new HttpClientHandler())
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (owner.accessToken is { } token)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    #endregion

    private Task<ApiResult<T>> GetResult<T>(string queryString, CancellationToken token = default, TimeSpan? timeout = null)
    {
        return SendResult<T>(t => httpClient.GetAsync(queryString, t), token, timeout);
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

    private Task<ApiResult<T>> SendResult<T>(Func<CancellationToken, Task<HttpResponseMessage>> send, CancellationToken token, TimeSpan? timeout = null)
    {
        return ReadResult(t => SendAuthenticated(send, t), (content, t) => content.ReadFromJsonAsync<T>(jsonOptions, t), token, timeout);
    }

    /// <summary>
    /// Sends a request and turns everything that can come back into an <see cref="ApiResult{T}"/>: the payload
    /// where the server answered 200, the server's own <c>ProblemDetails</c> where it answered anything else, and
    /// one built from the exception where nothing answered at all - a wrong address, no network, a server that is
    /// not running. Nothing here throws, so a caller never has to turn a socket error into words of its own.
    /// </summary>
    /// <param name="readPayload">
    /// How to read a successful body. Most endpoints answer with JSON, but not all of them do.
    /// </param>
    private async Task<ApiResult<T>> ReadResult<T>(Func<CancellationToken, Task<HttpResponseMessage>> send, Func<HttpContent, CancellationToken, Task<T?>> readPayload, CancellationToken token, TimeSpan? timeout = null)
    {
        HttpResponseMessage? responseMessage = null;

        // The request's own timeout, on top of whatever the caller cancels: the client has none of its own.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeoutSource.CancelAfter(timeout ?? DefaultTimeout);
        var t = timeoutSource.Token;

        try
        {
            responseMessage = await send(t).ConfigureAwait(false);

            return responseMessage.StatusCode != HttpStatusCode.OK
                ? ApiResult<T>.FromProblemDetails(await GetErrors(responseMessage, t).ConfigureAwait(false), responseMessage.StatusCode)
                : new ApiResult<T>
                {
                    Payload = await readPayload(responseMessage.Content, t).ConfigureAwait(false),
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

    /// <summary>
    /// The <c>ProblemDetails</c> of a refused request, or <see langword="null"/> where the body held none. Not
    /// every error comes from our server: a reverse proxy or a wrong address answers with an HTML page, and that
    /// must not turn into a <c>JsonException</c> in front of the user.
    /// </summary>
    private static async ValueTask<ProblemDetails?> GetErrors(HttpResponseMessage message, CancellationToken token)
    {
        try
        {
            return await message.Content.ReadFromJsonAsync<ProblemDetails?>(jsonOptions, token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        EndSession();
        httpClient.Dispose();
        sessionLock.Dispose();
    }
}
