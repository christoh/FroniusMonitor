namespace De.Hochstaetter.Fronius.Services;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class FritzBoxService : BindableBase, IFritzBoxService
{
    private string? fritzBoxSid;

    [ObservableProperty]
    public partial WebConnection? Connection { get; set; }


    public async ValueTask FritzBoxLogin(CancellationToken token = default)
    {
        if (Connection == null)
        {
            throw new NullReferenceException(Resources.NoFritzBoxConnection);
        }

        var document = await GetXmlResponse("login_sid.lua", token: token);
        var challenge = document.SelectSingleNode("/SessionInfo/Challenge")?.InnerText ?? throw new InvalidDataException("FritzBox did not supply challenge");
        var text = challenge + "-" + Connection.Password;
        var response = challenge + "-" + MD5.HashData(Encoding.Unicode.GetBytes(text)).Aggregate("", (current, next) => $"{current}{next:x2}");

        var dict = new Dictionary<string, string>
        {
            { "username", Connection.UserName },
            { "response", response },
        };

        document = await GetXmlResponse("login_sid.lua", dict, token);
        fritzBoxSid = document.SelectSingleNode("/SessionInfo/SID")?.InnerText ?? throw new UnauthorizedAccessException(Resources.AccessDenied);

        if (fritzBoxSid.All(c => c == '0'))
        {
            fritzBoxSid = null;
            throw new UnauthorizedAccessException(Resources.AccessDenied);
        }
    }

    public async Task<FritzBoxDeviceList> GetDevices(CancellationToken token = default)
    {
        await using var stream = await GetStreamResponse("webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos", token: token)
            .ConfigureAwait(false) ?? throw new InvalidDataException();

        var serializer = new XmlSerializer(typeof(FritzBoxDeviceList));
        var result = serializer.Deserialize(stream) as FritzBoxDeviceList ?? throw new InvalidDataException();
        result.Devices.Apply(d => d.FritzBoxService = this);
        return result;
    }

    public async ValueTask SwitchDevice(string ain, bool turnOn, CancellationToken token = default)
    {
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setswitcho{(turnOn ? "n" : "ff")}", token: token).ConfigureAwait(false);
    }


    public async ValueTask SetLevel(string ain, double level, CancellationToken token = default)
    {
        var byteLevel = Math.Max((byte)Math.Round(level * 255, MidpointRounding.AwayFromZero), (byte)2);
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);
        using var _ = await GetFritzBoxResponse($"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setlevel&level={byteLevel}", token: token).ConfigureAwait(false);
    }

    public async ValueTask SetColorTemperature(string ain, double temperatureKelvin, CancellationToken token = default)
    {
        var intTemperature = (int)Math.Round(temperatureKelvin, MidpointRounding.AwayFromZero);
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);

        using var _ = await GetFritzBoxResponse
        (
            $"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setcolortemperature&temperature={intTemperature}&duration=0",
            token: token
        ).ConfigureAwait(false);
    }

    public async ValueTask SetColor(string ain, double hueDegrees, double saturation, CancellationToken token = default)
    {
        var intHue = Math.Min(Math.Max(0, (int)Math.Round(hueDegrees)), 359);
        var intSaturation = Math.Min(Math.Max(0, (int)Math.Round(saturation * 255)), 255);
        ain = ain.Replace(" ", "", StringComparison.InvariantCulture);

        using var _ = await GetFritzBoxResponse
        (
            $"webservices/homeautoswitch.lua?ain={ain}&switchcmd=setunmappedcolor&hue={intHue}&saturation={intSaturation}&duration=0",
            token: token
        ).ConfigureAwait(false);
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
            if (Connection == null)
            {
                throw new NullReferenceException(Resources.NoFritzBoxConnection);
            }

            var requestString = $"{Connection.BaseUrl}/{request}{(fritzBoxSid == null || request.StartsWith("login_sid.lua") ? string.Empty : $"&sid={fritzBoxSid}")}";
            using var client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true });
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HomeAutomationClient/1.0");

            // ReSharper disable once PossibleMultipleEnumeration
            response = postVariables == null
                ? await client.GetAsync(requestString, token).ConfigureAwait(false)
                : await client.PostAsync(requestString, new FormUrlEncodedContent(postVariables), token).ConfigureAwait(false);

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
}