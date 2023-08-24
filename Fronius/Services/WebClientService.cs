using De.Hochstaetter.Fronius.Models.Gen24.Commands;
using De.Hochstaetter.Fronius.Models.Settings;

namespace De.Hochstaetter.Fronius.Services;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class WebClientService : BindableBase, IWebClientService
{
    private readonly IGen24JsonService gen24JsonService;
    private DigestAuthHttp? froniusHttpClient;
    private string? fritzBoxSid;
    private DateTime lastSolarApiCall = DateTime.UtcNow.AddSeconds(-4);
    private JObject? invariantConfigToken;
    private JObject? localConfigToken;
    private JObject? localUiToken;
    private JObject? invariantUiToken;
    private JObject? localEventToken;
    private JObject? invariantEventToken;

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

    public async Task<T> ReadGen24Entity<T>(string request) where T : new()
    {
        var token = (await GetFroniusJsonResponse(request).ConfigureAwait(false)).Token;
        return gen24JsonService.ReadFroniusData<T>(token);
    }

    public async Task<IOrderedEnumerable<Gen24Event>> GetFroniusEvents()
    {
        var eventList = new List<Gen24Event>(256);

        Parallel.ForEach
        (
            (await GetFroniusJsonResponse("status/events").ConfigureAwait(false)).Token,
            eventToken => { eventList.Add(gen24JsonService.ReadFroniusData<Gen24Event>(eventToken)); }
        );

        return eventList.OrderByDescending(e => e.EventTime);
    }

    [SuppressMessage("ReSharper", "CommentTypo")]
    public async Task<Gen24System> GetFroniusData(Gen24Components components)
    {
        var gen24System = new Gen24System();

        //try
        //{
        //    //var test1 = await GetFroniusStringResponse("config/powerlimits").ConfigureAwait(false); // None
        //    //var test2 = await GetFroniusStringResponse("config/ics").ConfigureAwait(false); // None
        //    //var test3 = (await GetFroniusJsonResponse("config/solarweb").ConfigureAwait(false)); // Read
        //    //var test4 = await GetFroniusStringResponse("config/emrs").ConfigureAwait(false); // Read/Write
        //    //var test5 = await GetFroniusStringResponse("config/meter").ConfigureAwait(false); // Read
        //    var test6 = (await GetFroniusJsonResponse("config/").ConfigureAwait(false)).Token;
        //    var test6String = test6.ToString();
        //    //var token = JObject.Parse(test3);
        //    //token["enableRemoteControl"] = true;
        //    //token.Remove("_connectionKeepAlive_meta");
        //    //token.Remove("_enableRemoteControl_meta");
        //    //var response = await GetFroniusStringResponse("config/solarweb", token).ConfigureAwait(false);
        //}
        //catch (Exception ex)
        //{
        //}

        var (token, _) = await GetFroniusJsonResponse("status/devices").ConfigureAwait(false);

        foreach (var statusToken in (JArray)token)
        {
            var status = gen24JsonService.ReadFroniusData<Gen24Status>(statusToken);

            switch (status.DeviceType)
            {
                case DeviceType.Inverter:
                    gen24System.InverterStatus = status;
                    break;

                case DeviceType.PowerMeter:
                    gen24System.MeterStatus = status;
                    break;
            }
        }

        var (_, dataToken) = await GetJsonResponse<BaseResponse>("components/readable", true).ConfigureAwait(false);
        gen24System.Inverter = gen24JsonService.ReadFroniusData<Gen24Inverter>(dataToken[components.Groups["Inverter"].FirstOrDefault() ?? "1"]);

        if (components.Groups.TryGetValue("BatteryManagementSystem", out var storages))
        {
            gen24System.Storage = gen24JsonService.ReadFroniusData<Gen24Storage>(dataToken[storages.FirstOrDefault() ?? "16580608"]);
        }

        var restrictions = components.Groups["Application"].Select(key => dataToken[key]).FirstOrDefault(t => t?["attributes"]?["PowerRestrictionControllerVersion"] != null);
        gen24System.Restrictions = gen24JsonService.ReadFroniusData<Gen24Restrictions>(restrictions);

        if (components.Groups.TryGetValue("PowerMeter", out var powerMeters))
        {
            foreach (var meter in powerMeters.Select(key => dataToken[key]))
            {
                var gen24PowerMeter = gen24JsonService.ReadFroniusData<Gen24PowerMeter3P>(meter);
                gen24System.Meters.Add(gen24PowerMeter);
            }
        }

        gen24System.DataManager = gen24JsonService.ReadFroniusData<Gen24DataManager>(dataToken[components.Groups["DataManager"].Single()]);
        gen24System.PowerFlow = gen24JsonService.ReadFroniusData<Gen24PowerFlow>(dataToken[components.Groups["Site"].Single()]);
        gen24System.Cache = gen24JsonService.ReadFroniusData<Gen24Cache>(dataToken[components.Groups["CACHE"].FirstOrDefault() ?? "393216"]);

        return gen24System;
    }

    public async Task<string> GetUiString(string category, string key)
    {
        (localUiToken, invariantUiToken) = await EnsureText("app/assets/i18n/WeblateTranslations/ui", localUiToken, invariantUiToken).ConfigureAwait(false);
        return GetCategoryKeyString(localUiToken, invariantUiToken, category, key);
    }

    public async Task<string> GetConfigString(string category, string key)
    {
        (localConfigToken, invariantConfigToken) = await EnsureText("app/assets/i18n/WeblateTranslations/config", localConfigToken, invariantConfigToken).ConfigureAwait(false);
        return GetCategoryKeyString(localConfigToken, invariantConfigToken, category, key);
    }

    public async Task<string> GetEventDescription(string code)
    {
        (localEventToken, invariantEventToken) = await EnsureText("app/assets/i18n/StateCodeTranslations", localEventToken, invariantEventToken).ConfigureAwait(false);
        return GetCategoryKeyString(localEventToken, invariantEventToken, "StateCodes", code);
    }

    private static string GetCategoryKeyString(JObject? localToken, JObject? invariantToken, string category, string key)
    {
        return localToken?[category]?[key]?.Value<string>() ?? invariantToken?[category]?[key]?.Value<string>() ?? $"{(category != "StateCodes" ? $"{category}." : string.Empty)}{key}";
    }

    private async Task<(JObject?, JObject?)> EnsureText(string baseUrl, JObject? l, JObject? i)
    {
        try
        {
            i ??= JObject.Parse((await GetFroniusStringResponse($"{baseUrl}/en.json").ConfigureAwait(false)).JsonString);
        }
        catch
        {
            //i ??= new JObject();
        }


        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "en")
        {
            try
            {
                l ??= JObject.Parse((await GetFroniusStringResponse($"{baseUrl}/{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.json").ConfigureAwait(false)).JsonString);
            }
            catch
            {
                //l = new JObject();
            }
        }

        return (l, i);
    }

    public async Task FritzBoxLogin()
    {
        if (FritzBoxConnection == null)
        {
            throw new NullReferenceException(Resources.NoFritzBoxConnection);
        }

        var document = await GetXmlResponse("login_sid.lua");
        var challenge = document.SelectSingleNode("/SessionInfo/Challenge")?.InnerText ?? throw new InvalidDataException("FritzBox did not supply challenge");
        var text = challenge + "-" + FritzBoxConnection.Password;
        var response = challenge + "-" + MD5.HashData(Encoding.Unicode.GetBytes(text)).Aggregate("", (current, next) => $"{current}{next:x2}");

        var dict = new Dictionary<string, string>
        {
            { "username", FritzBoxConnection.UserName },
            { "response", response }
        };

        document = await GetXmlResponse("login_sid.lua", dict);
        fritzBoxSid = document.SelectSingleNode("/SessionInfo/SID")?.InnerText ?? throw new UnauthorizedAccessException(Resources.AccessDenied);

        if (fritzBoxSid.All(c => c == '0'))
        {
            fritzBoxSid = null;
            throw new UnauthorizedAccessException(Resources.AccessDenied);
        }
    }

    public async Task<FritzBoxDeviceList> GetFritzBoxDevices()
    {
        await using var stream = await GetStreamResponse("webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos").ConfigureAwait(false) ?? throw new InvalidDataException();
        var serializer = new XmlSerializer(typeof(FritzBoxDeviceList));
        var result = serializer.Deserialize(stream) as FritzBoxDeviceList ?? throw new InvalidDataException();
        result.Devices.Apply(d => d.WebClientService = this);
        return result;
    }

    public async Task TurnOnFritzBoxDevice(string ain)
    {
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setswitchon").ConfigureAwait(false);
    }

    public async Task TurnOffFritzBoxDevice(string ain)
    {
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setswitchoff").ConfigureAwait(false);
    }

    public async Task SetFritzBoxLevel(string ain, double level)
    {
        var byteLevel = Math.Max((byte)Math.Round(level * 255, MidpointRounding.AwayFromZero), (byte)2);
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setlevel&level={byteLevel}").ConfigureAwait(false);
    }

    public async Task SetFritzBoxColorTemperature(string ain, double temperatureKelvin)
    {
        var intTemperature = (int)Math.Round(temperatureKelvin, MidpointRounding.AwayFromZero);
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setcolortemperature&temperature={intTemperature}&duration=0").ConfigureAwait(false);
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

    public async Task SetFritzBoxColor(string ain, double hueDegrees, double saturation)
    {
        var intHue = allowedFritzBoxColors.Keys.MinBy(k => Math.Min(Math.Abs(hueDegrees - k), Math.Abs(hueDegrees + 360 - k)));
        var intSaturation = allowedFritzBoxColors[intHue].MinBy(s => Math.Abs(s - saturation * 255));
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setcolor&hue={intHue}&saturation={intSaturation}&duration=0").ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> GetFritzBoxResponse(string request, IEnumerable<KeyValuePair<string, string>>? postVariables = null)
    {
        HttpResponseMessage response;

        if (fritzBoxSid == null && !request.StartsWith("login_sid.lua"))
        {
            await FritzBoxLogin().ConfigureAwait(false);
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
            response = postVariables == null ? await client.GetAsync(requestString).ConfigureAwait(false) : await client.PostAsync(requestString, new FormUrlEncodedContent(postVariables)).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                await FritzBoxLogin().ConfigureAwait(false);
                continue;
            }

            response.EnsureSuccessStatusCode();
            break;
        }

        return response;
    }

    private async Task<Stream> GetStreamResponse(string request, IEnumerable<KeyValuePair<string, string>>? postVariables = null)
    {
        var response = await GetFritzBoxResponse(request, postVariables).ConfigureAwait(false);
        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable IDE0051
    private async Task<string> GetStringResponse(string request)
    {
        var response = await GetFritzBoxResponse(request).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
#pragma warning restore IDE0051

    private async Task<XmlDocument> GetXmlResponse(string request, IEnumerable<KeyValuePair<string, string>>? postVariables = null)
    {
        await using var stream = await GetStreamResponse(request, postVariables).ConfigureAwait(false);
        var result = new XmlDocument();
        result.Load(stream);
        return result;
    }

    private readonly object froniusHttpClientLockObject = new();

    public async Task<(string JsonString, HttpStatusCode StatusCode)> GetFroniusStringResponse(string request, JToken? token = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null)
    {
        var client = await GetFroniusHttpClient();
        var result = await client.GetString(request, token?.ToString(), allowedStatusCodes).ConfigureAwait(false);
        lastSolarApiCall = DateTime.UtcNow;
        return result;
    }

    public async Task<(JToken Token, HttpStatusCode StatusCode)> GetFroniusJsonResponse(string request, JToken? token = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null)
    {
        var client = await GetFroniusHttpClient();
        var result = await client.GetJsonToken(request, token, allowedStatusCodes).ConfigureAwait(false);
        lastSolarApiCall = DateTime.UtcNow;
        return result;
    }

    public async Task<T?> SendFroniusCommand<T>(string request, JToken? token = null) where T : Gen24NoResultCommand, new()
    {
        var client = await GetFroniusHttpClient();
        var (result, statusCode) = await client.GetJsonToken(request, token, new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest, }).ConfigureAwait(false);

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

    private async Task<DigestAuthHttp> GetFroniusHttpClient()
    {
        if (InverterConnection?.BaseUrl == null)
        {
            throw new NullReferenceException(Resources.NoSystemConnection);
        }

        DigestAuthHttp client;

        lock (froniusHttpClientLockObject)
        {
            froniusHttpClient ??= new DigestAuthHttp(InverterConnection ?? throw new ArgumentNullException());
            client = froniusHttpClient;
        }

        var nextAllowedCall = lastSolarApiCall.AddSeconds(.2) - DateTime.UtcNow;

        if (nextAllowedCall.Ticks > 0)
        {
            await Task.Delay(nextAllowedCall).ConfigureAwait(false);
        }

        return client;
    }

    private async Task<(T, JToken)> GetJsonResponse<T>(string request, bool useUnofficialApi = false) where T : BaseResponse, new()
    {
        var requestString = $"{(useUnofficialApi ? string.Empty : "solar_api/v1/")}{request}";
        var (jsonString, status) = await GetFroniusStringResponse(requestString);

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
                throw new SolarException(result.StatusCode, result.Reason, result.UserMessage, requestString);
            }

            var data = JObject.Parse(jsonString)["Body"]?["Data"] ?? throw new InvalidDataException(Resources.IncorrectData);
            return (result, data);
        }).ConfigureAwait(false);
    }

    public override string ToString() => InverterConnection?.BaseUrl ?? "---";
}
