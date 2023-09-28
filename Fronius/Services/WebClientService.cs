using De.Hochstaetter.Fronius.Models.Gen24.Commands;

namespace De.Hochstaetter.Fronius.Services;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class WebClientService : BindableBase, IWebClientService
{
    private readonly IGen24JsonService gen24JsonService;
    private DigestAuthHttp? froniusHttpClient;
    private string? fritzBoxSid;
    private DateTime lastSolarApiCall = DateTime.UtcNow.AddSeconds(-4);
    private JObject? invariantConfigToken, localConfigToken;
    private JObject? localUiToken, invariantUiToken;
    private JObject? localEventToken, invariantEventToken;
    private JObject? localChannelToken, invariantChannelToken;

    public WebClientService(IGen24JsonService gen24JsonService)
    {
        this.gen24JsonService = gen24JsonService;
    }

    private WebConnection? inverterConnection;

    public WebConnection? InverterConnection
    {
        get => inverterConnection;
        set => Set(ref inverterConnection, value, () =>
        {
            lock (froniusHttpClientLockObject)
            {
                froniusHttpClient?.Dispose();
                froniusHttpClient = null;
            }
        });
    }

    private WebConnection? fritzBoxConnection;

    public WebConnection? FritzBoxConnection
    {
        get => fritzBoxConnection;
        set => Set(ref fritzBoxConnection, value);
    }

    public async ValueTask<T> ReadGen24Entity<T>(string request, CancellationToken token = default) where T : new()
    {
        var jToken = (await GetFroniusJsonResponse(request, token: token).ConfigureAwait(false)).Token;
        return gen24JsonService.ReadFroniusData<T>(jToken);
    }

    public async ValueTask<IOrderedEnumerable<Gen24Event>> GetFroniusEvents(CancellationToken token = default)
    {
        var eventList = new List<Gen24Event>(256);

        Parallel.ForEach
        (
            (await GetFroniusJsonResponse("status/events", token: token).ConfigureAwait(false)).Token,
            eventToken => { eventList.Add(gen24JsonService.ReadFroniusData<Gen24Event>(eventToken)); }
        );

        return eventList.OrderByDescending(e => e.EventTime);
    }

    [SuppressMessage("ReSharper", "CommentTypo")]
    public async Task<Gen24Sensors> GetFroniusData(Gen24Components components, CancellationToken token = default)
    {
        var gen24Sensors = new Gen24Sensors();
        var (jToken, _) = await GetFroniusJsonResponse("status/devices", token: token).ConfigureAwait(false);

        foreach (var statusToken in (JArray)jToken)
        {
            var status = gen24JsonService.ReadFroniusData<Gen24Status>(statusToken);

            switch (status.DeviceType)
            {
                case DeviceType.Inverter:
                    gen24Sensors.InverterStatus = status;
                    break;

                case DeviceType.PowerMeter:
                    gen24Sensors.MeterStatus = status;
                    break;
            }
        }

        var (_, dataToken) = await GetJsonResponse<BaseResponse>("components/readable", true, token: token).ConfigureAwait(false);
        gen24Sensors.Inverter = gen24JsonService.ReadFroniusData<Gen24Inverter>(dataToken[components.Groups["Inverter"].FirstOrDefault() ?? "1"]);

        if (components.Groups.TryGetValue("BatteryManagementSystem", out var storages))
        {
            gen24Sensors.Storage = gen24JsonService.ReadFroniusData<Gen24Storage>(dataToken[storages.FirstOrDefault() ?? "16580608"]);
        }

        var restrictions = components.Groups["Application"].Select(key => dataToken[key]).FirstOrDefault(t => t?["attributes"]?["PowerRestrictionControllerVersion"] != null);
        gen24Sensors.Restrictions = gen24JsonService.ReadFroniusData<Gen24Restrictions>(restrictions);

        if (components.Groups.TryGetValue("PowerMeter", out var powerMeters))
        {
            foreach (var meter in powerMeters.Select(key => dataToken[key]))
            {
                var gen24PowerMeter = gen24JsonService.ReadFroniusData<Gen24PowerMeter3P>(meter);
                gen24Sensors.Meters.Add(gen24PowerMeter);
            }
        }

        gen24Sensors.DataManager = gen24JsonService.ReadFroniusData<Gen24DataManager>(dataToken[components.Groups["DataManager"].Single()]);
        gen24Sensors.PowerFlow = gen24JsonService.ReadFroniusData<Gen24PowerFlow>(dataToken[components.Groups["Site"].Single()]);
        gen24Sensors.Cache = gen24JsonService.ReadFroniusData<Gen24Cache>(dataToken[components.Groups["CACHE"].FirstOrDefault() ?? "393216"]);

        return gen24Sensors;
    }

    public Task<string> GetFroniusName<T>(T enumValue, CancellationToken token = default) where T : Enum
    {
        var enumValueString = enumValue.ToString();

        var attribute = typeof(T)
            .GetMember(enumValueString).Single()
            .GetCustomAttribute(typeof(EnumParseAttribute)) as EnumParseAttribute;

        var key = attribute?.ParseAs ?? enumValueString;
        return GetChannelString(key, token);
    }

    public async Task<string> GetUiString(string path, CancellationToken token = default)
    {
        (localUiToken, invariantUiToken) = await EnsureText("app/assets/i18n/WeblateTranslations/ui", localUiToken, invariantUiToken, token).ConfigureAwait(false);
        return GetLocalizedString(localUiToken, invariantUiToken, path);
    }

    public async Task<string> GetConfigString(string path, CancellationToken token = default)
    {
        (localConfigToken, invariantConfigToken) = await EnsureText("app/assets/i18n/WeblateTranslations/config", localConfigToken, invariantConfigToken, token).ConfigureAwait(false);
        return GetLocalizedString(localConfigToken, invariantConfigToken, path);
    }

    public async Task<string> GetChannelString(string key, CancellationToken token = default)
    {
        (localChannelToken, invariantChannelToken) = await EnsureText("app/assets/i18n/WeblateTranslations/channels", localChannelToken, invariantChannelToken, token).ConfigureAwait(false);
        return GetLocalizedString(localChannelToken, invariantChannelToken, key);
    }

    public async ValueTask<string> GetEventDescription(string code, CancellationToken token = default)
    {
        (localEventToken, invariantEventToken) = await EnsureText("app/assets/i18n/StateCodeTranslations", localEventToken, invariantEventToken, token).ConfigureAwait(false);
        return GetLocalizedString(localEventToken, invariantEventToken, "StateCodes." + code);
    }

    private static string GetLocalizedString(JObject? localToken, JObject? invariantToken, string path)
    {
        var keys = path.Split('.');

        if (localToken != null)
        {
            var result = GetStringFromKeys(localToken, keys);

            if (result != null)
            {
                return result;
            }
        }

        return GetStringFromKeys(invariantToken, keys) ?? path;
    }

    private static string? GetStringFromKeys(JObject? token, string[] keys)
    {
        while (true)
        {
            if (token == null)
            {
                return null;
            }

            if (keys.Length == 1)
            {
                return token[keys[0]]?.Value<string>();
            }

            token = token[keys[0]]?.Value<JObject>();
            keys = keys[1..];
        }
    }

    private async ValueTask<(JObject?, JObject?)> EnsureText(string baseUrl, JObject? l, JObject? i, CancellationToken token = default)
    {
        while (InverterConnection?.BaseUrl == null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), token).ConfigureAwait(false);
        }
        
        try
        {
            if (i == null)
            {
                await Task.Run(async () => { i = JObject.Parse((await GetFroniusStringResponse($"{baseUrl}/en.json", token: token).ConfigureAwait(false)).JsonString); }, token).ConfigureAwait(false);
            }
        }
        catch
        {
            //i ??= new JObject();
        }


        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "en")
        {
            try
            {
                if (l == null)
                {
                    await Task.Run(async () => { l = JObject.Parse((await GetFroniusStringResponse($"{baseUrl}/{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.json", token: token).ConfigureAwait(false)).JsonString); }, token).ConfigureAwait(false);
                }
            }
            catch
            {
                //l = new JObject();
            }
        }

        return (l, i);
    }

    public async ValueTask FritzBoxLogin(CancellationToken token = default)
    {
        if (FritzBoxConnection == null)
        {
            throw new NullReferenceException(Resources.NoFritzBoxConnection);
        }

        var document = await GetXmlResponse("login_sid.lua", token: token);
        var challenge = document.SelectSingleNode("/SessionInfo/Challenge")?.InnerText ?? throw new InvalidDataException("FritzBox did not supply challenge");
        var text = challenge + "-" + FritzBoxConnection.Password;
        var response = challenge + "-" + MD5.HashData(Encoding.Unicode.GetBytes(text)).Aggregate("", (current, next) => $"{current}{next:x2}");

        var dict = new Dictionary<string, string>
        {
            { "username", FritzBoxConnection.UserName },
            { "response", response }
        };

        document = await GetXmlResponse("login_sid.lua", dict, token);
        fritzBoxSid = document.SelectSingleNode("/SessionInfo/SID")?.InnerText ?? throw new UnauthorizedAccessException(Resources.AccessDenied);

        if (fritzBoxSid.All(c => c == '0'))
        {
            fritzBoxSid = null;
            throw new UnauthorizedAccessException(Resources.AccessDenied);
        }
    }

    public async ValueTask<FritzBoxDeviceList> GetFritzBoxDevices(CancellationToken token = default)
    {
        await using var stream = await GetStreamResponse("webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos", token: token).ConfigureAwait(false) ?? throw new InvalidDataException();
        var serializer = new XmlSerializer(typeof(FritzBoxDeviceList));
        var result = serializer.Deserialize(stream) as FritzBoxDeviceList ?? throw new InvalidDataException();
        result.Devices.Apply(d => d.WebClientService = this);
        return result;
    }

    public async ValueTask TurnOnFritzBoxDevice(string ain, CancellationToken token = default)
    {
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setswitchon", token: token).ConfigureAwait(false);
    }

    public async ValueTask TurnOffFritzBoxDevice(string ain, CancellationToken token = default)
    {
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setswitchoff", token: token).ConfigureAwait(false);
    }

    public async ValueTask SetFritzBoxLevel(string ain, double level, CancellationToken token = default)
    {
        var byteLevel = Math.Max((byte)Math.Round(level * 255, MidpointRounding.AwayFromZero), (byte)2);
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setlevel&level={byteLevel}", token: token).ConfigureAwait(false);
    }

    public async ValueTask SetFritzBoxColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default)
    {
        var intTemperature = (int)Math.Round(temperatureKelvin, MidpointRounding.AwayFromZero);
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setcolortemperature&temperature={intTemperature}&duration=0", token: token).ConfigureAwait(false);
    }


    private static readonly Dictionary<int, IEnumerable<int>> allowedFritzBoxColors = new()
    {
        { 358, new[] { 180, 112, 54 } },
        { 35, new[] { 214, 140, 72 } },
        { 52, new[] { 153, 102, 51 } },
        { 92, new[] { 123, 79, 38 } },
        { 120, new[] { 160, 82, 38 } },
        { 160, new[] { 145, 84, 41 } },
        { 195, new[] { 179, 118, 59 } },
        { 212, new[] { 169, 110, 56 } },
        { 225, new[] { 204, 135, 67 } },
        { 266, new[] { 169, 110, 54 } },
        { 296, new[] { 140, 92, 46 } },
        { 335, new[] { 180, 107, 51 } },
    };

    public async ValueTask SetFritzBoxColor(string ain, double hueDegrees, double saturation, CancellationToken token = default)
    {
        var intHue = allowedFritzBoxColors.Keys.MinBy(k => Math.Min(Math.Abs(hueDegrees - k), Math.Abs(hueDegrees + 360 - k)));
        var intSaturation = allowedFritzBoxColors[intHue].MinBy(s => Math.Abs(s - saturation * 255));
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setcolor&hue={intHue}&saturation={intSaturation}&duration=0", token: token).ConfigureAwait(false);
    }

    private async ValueTask<HttpResponseMessage> GetFritzBoxResponse(string request, IEnumerable<KeyValuePair<string, string>>? postVariables = null, CancellationToken token = default)
    {
        HttpResponseMessage response;

        if (fritzBoxSid == null && !request.StartsWith("login_sid.lua"))
        {
            await FritzBoxLogin(token).ConfigureAwait(false);
        }

        while (true)
        {
            if (FritzBoxConnection == null)
            {
                throw new NullReferenceException(Resources.NoSystemConnection);
            }

            var requestString = $"{FritzBoxConnection.BaseUrl}/{request}{(fritzBoxSid == null || request.StartsWith("login_sid.lua") ? string.Empty : $"&sid={fritzBoxSid}")}";

            using var client = new HttpClient
            (
                new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true, }
            );

            // ReSharper disable once PossibleMultipleEnumeration
            response = postVariables == null ? await client.GetAsync(requestString, token).ConfigureAwait(false) : await client.PostAsync(requestString, new FormUrlEncodedContent(postVariables), token).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                await FritzBoxLogin(token).ConfigureAwait(false);
                continue;
            }

            response.EnsureSuccessStatusCode();
            break;
        }

        return response;
    }

    private async ValueTask<Stream> GetStreamResponse(string request, IEnumerable<KeyValuePair<string, string>>? postVariables = null, CancellationToken token = default)
    {
        var response = await GetFritzBoxResponse(request, postVariables, token).ConfigureAwait(false);
        return await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable IDE0051
    private async ValueTask<string> GetStringResponse(string request)
    {
        var response = await GetFritzBoxResponse(request).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
#pragma warning restore IDE0051

    private async ValueTask<XmlDocument> GetXmlResponse(string request, IEnumerable<KeyValuePair<string, string>>? postVariables = null, CancellationToken token = default)
    {
        await using var stream = await GetStreamResponse(request, postVariables, token).ConfigureAwait(false);
        var result = new XmlDocument();
        result.Load(stream);
        return result;
    }

    private readonly object froniusHttpClientLockObject = new();

    public async ValueTask<(string JsonString, HttpStatusCode StatusCode)> GetFroniusStringResponse(string request, JToken? jToken = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
    {
        var client = await GetFroniusHttpClient(token);
        var result = await client.GetString(request, jToken?.ToString(), allowedStatusCodes, token).ConfigureAwait(false);
        lastSolarApiCall = DateTime.UtcNow;
        return result;
    }

    public async ValueTask<(JToken Token, HttpStatusCode StatusCode)> GetFroniusJsonResponse(string request, JToken? jToken = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
    {
        var client = await GetFroniusHttpClient(token);
        var result = await client.GetJsonToken(request, jToken, allowedStatusCodes, token).ConfigureAwait(false);
        lastSolarApiCall = DateTime.UtcNow;
        return result;
    }

    public async ValueTask<T?> SendFroniusCommand<T>(string request, JToken? jToken = null, CancellationToken token = default) where T : Gen24NoResultCommand, new()
    {
        var client = await GetFroniusHttpClient(token);
        var (result, statusCode) = await client.GetJsonToken(request, jToken, new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest, }, token).ConfigureAwait(false);

        if (statusCode == HttpStatusCode.BadRequest)
        {
            var message = result["failure"]?.Value<string>() ?? "Unknown bad request";
            throw new HttpRequestException(message, null, statusCode);
        }

        var success = result["success"]?.Value<bool>() ?? false;

        if (!success)
        {
            throw new InvalidDataException(result.ToString());
        }

        var resultData = result["resultData"];

        return resultData is { HasValues: true } ? gen24JsonService.ReadFroniusData<T>(resultData) : null;
    }

    public ValueTask<Gen24StandByStatus?> GetInverterStandByStatus(CancellationToken token = default) => SendFroniusCommand<Gen24StandByStatus>("commands/StandbyState", token: token);

    public async ValueTask RequestInverterStandBy(bool isStandBy, CancellationToken token = default)
    {
        var jToken = JObject.Parse($"{{\"requestState\": {(isStandBy ? "0" : "1")}}}");
        await SendFroniusCommand<Gen24NoResultCommand>("commands/StandbyRequestState", jToken, token).ConfigureAwait(false);
    }

    private async Task<DigestAuthHttp> GetFroniusHttpClient(CancellationToken token = default)
    {
        if (InverterConnection?.BaseUrl == null)
        {
            throw new NullReferenceException(Resources.NoSystemConnection);
        }

        DigestAuthHttp client;

        lock (froniusHttpClientLockObject)
        {
            froniusHttpClient ??= new DigestAuthHttp(InverterConnection ?? throw new ArgumentNullException(null, @"No inverter connection"));
            client = froniusHttpClient;
        }

        var nextAllowedCall = lastSolarApiCall.AddSeconds(.2) - DateTime.UtcNow;

        if (nextAllowedCall.Ticks > 0)
        {
            await Task.Delay(nextAllowedCall, token).ConfigureAwait(false);
        }

        return client;
    }

    private async Task<(T, JToken)> GetJsonResponse<T>(string request, bool useUnofficialApi = false, CancellationToken token = default) where T : BaseResponse, new()
    {
        var requestString = $"{(useUnofficialApi ? string.Empty : "solar_api/v1/")}{request}";
        var (jsonString, status) = await GetFroniusStringResponse(requestString, token: token);

        if (status != HttpStatusCode.OK)
        {
            throw new HttpRequestException(string.Format(Resources.InverterCommReadError, status), null, status);
        }

        return await Task.Run(() =>
        {
            var headToken = JObject.Parse(jsonString)["Head"] ?? throw new InvalidDataException(Resources.IncorrectData);
            var statusToken = headToken["Status"] ?? throw new InvalidDataException(Resources.IncorrectData);

            var result = new T
            {
                StatusCode = statusToken["Code"]?.Value<int>() ?? throw new InvalidDataException(Resources.IncorrectData),
                Timestamp = (headToken["Timestamp"]?.Value<DateTime>() ?? DateTime.UnixEpoch).ToUniversalTime(),
                Reason = statusToken["Reason"]?.Value<string>() ?? string.Empty,
                UserMessage = statusToken["UserMessage"]?.Value<string>() ?? string.Empty
            };

            if (result.StatusCode != 0)
            {
                throw new Gen24Exception(result.StatusCode, result.Reason, result.UserMessage, requestString);
            }

            var data = JObject.Parse(jsonString)["Body"]?["Data"] ?? throw new InvalidDataException(Resources.IncorrectData);
            return (result, data);
        }, token).ConfigureAwait(false);
    }

    public override string ToString() => InverterConnection?.BaseUrl ?? "---";
}
