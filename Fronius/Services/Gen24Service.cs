namespace De.Hochstaetter.Fronius.Services
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class Gen24Service(IGen24JsonService gen24JsonService) : BindableBase, IGen24Service
    {
        private DigestAuthHttp? froniusHttpClient;
        private DateTime lastSolarApiCall = DateTime.UtcNow.AddSeconds(-4);
        private JObject? invariantConfigToken, localConfigToken;
        private JObject? localUiToken, invariantUiToken;
        private JObject? localEventToken, invariantEventToken;
        private JObject? localChannelToken, invariantChannelToken;

        public WebConnection? Connection
        {
            get;
            set => Set(ref field, value, () =>
            {
                lock (froniusHttpClientLockObject)
                {
                    froniusHttpClient?.Dispose();
                    froniusHttpClient = null;
                }
            });
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

        public async Task<IReadOnlyDictionary<Guid, Gen24ConnectedInverter>> GetConnectedDevices(bool doScan)
        {
            var uriString = $"commands/SystemPowerControl/GetDeviceStatus?doScan={(doScan ? "true" : "false")}";
            var (token, statusCode) = await GetFroniusJsonResponse(uriString).ConfigureAwait(false);

            if (token["success"]?.Value<bool>() is not true)
            {
                throw new Gen24Exception(0, "Could not get connected inverters", "success is not true", uriString);
            }

            var resultToken = token["resultData"] ?? throw new Gen24Exception(0, "Could not get connected inverters", "No resultData present", uriString);

            var devices = new List<Gen24ConnectedInverter>();

            if (resultToken["autodetectedControlledDevices"] is JObject autoDetectedControlledDevicesToken)
            {
                GetControlledDevices(autoDetectedControlledDevicesToken, true);
            }

            if (resultToken["staticControlledDevices"] is JObject staticControlledDevices)
            {
                GetControlledDevices(staticControlledDevices, false);
            }

            return devices.ToImmutableDictionary(d => d.Id);

            void GetControlledDevices(JObject devicesToken, bool isAutoDetected)
            {
                foreach (var propertyToken in devicesToken.Children<JProperty>())
                {
                    var inverter = gen24JsonService.ReadFroniusData<Gen24ConnectedInverter>(propertyToken.Value);
                    inverter.IsAutoDetected = isAutoDetected;
                    inverter.Id = Guid.Parse(propertyToken.Name);
                    devices.Add(inverter);
                }
            }
        }

        [SuppressMessage("ReSharper", "CommentTypo")]
        public async Task<Gen24Sensors> GetFroniusData(Gen24Components components, CancellationToken token = default)
        {
            var gen24Sensors = new Gen24Sensors();

            var (_, dataToken) = await GetJsonResponse<BaseResponse>("components/readable", true, token: token).ConfigureAwait(false);
            gen24Sensors.Inverter = gen24JsonService.ReadFroniusData<Gen24Inverter>(dataToken[components.Groups["Inverter"].FirstOrDefault() ?? "1"]);

            if (components.Groups.TryGetValue("BatteryManagementSystem", out var storages))
            {
                var storageGroupId = storages.FirstOrDefault() ?? "16580608";
                var storageToken = dataToken[storageGroupId];
                var nameplate = JObject.Parse(storageToken?["attributes"]?["nameplate"]?.Value<string>() ?? "{}");
                gen24Sensors.Storage = gen24JsonService.ReadFroniusData<Gen24Storage>(storageToken);
                gen24Sensors.Storage.MinimumStateOfCharge = (nameplate["min_soc"]?.Value<int>() ?? 0) / 100d;
                gen24Sensors.Storage.MaximumStateOfCharge = (nameplate["max_soc"]?.Value<int>() ?? 100) / 100d;
                gen24Sensors.Storage.GroupId = uint.Parse(storageGroupId);
            }

            if (components.Groups.TryGetValue("Application", out var applications))
            {
                var restrictions = applications
                    .Select(key => dataToken[key])
                    .FirstOrDefault(t => t?["attributes"]?["PowerRestrictionControllerVersion"] != null);

                gen24Sensors.Restrictions = gen24JsonService.ReadFroniusData<Gen24Restrictions>(restrictions);
            }

            if (components.Groups.TryGetValue("PowerMeter", out var powerMeters))
            {
                foreach (var powerMeter in powerMeters)
                {
                    var meter = dataToken[powerMeter];
                    var gen24PowerMeter = gen24JsonService.ReadFroniusData<Gen24PowerMeter3P>(meter);
                    gen24PowerMeter.GroupId = uint.Parse(powerMeter);
                    gen24Sensors.Meters.Add(gen24PowerMeter);
                }
            }

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
                        if (gen24Sensors.PrimaryPowerMeter?.GroupId is not null && status.Id == gen24Sensors.PrimaryPowerMeter?.GroupId)
                        {
                            gen24Sensors.MeterStatus = status;
                        }

                        break;
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
            (localConfigToken, invariantConfigToken) = await EnsureText("app/assets/i18n/WeblateTranslations/config",
                localConfigToken, invariantConfigToken, token).ConfigureAwait(false);

            return GetLocalizedString(localConfigToken, invariantConfigToken, path);
        }

        public async Task<string> GetChannelString(string key, CancellationToken token = default)
        {
            (localChannelToken, invariantChannelToken) = await EnsureText("app/assets/i18n/WeblateTranslations/channels",
                localChannelToken, invariantChannelToken, token).ConfigureAwait(false);

            return GetLocalizedString(localChannelToken, invariantChannelToken, key);
        }

        public async Task<string> GetEventDescription(string code, CancellationToken token = default)
        {
            (localEventToken, invariantEventToken) = await EnsureText("app/assets/i18n/StateCodeTranslations",
                localEventToken, invariantEventToken, token).ConfigureAwait(false);

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
            while (Connection?.BaseUrl == null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), token).ConfigureAwait(false);
            }

            try
            {
                if (i == null)
                {
                    await Task.Run(async () =>
                    {
                        i = JObject.Parse((await GetFroniusStringResponse($"{baseUrl}/en.json", token: token)
                            .ConfigureAwait(false)).JsonString);
                    }, token).ConfigureAwait(false);
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
                        await Task.Run(async () =>
                        {
                            l = JObject.Parse((await GetFroniusStringResponse($"{baseUrl}/{CultureInfo
                                .CurrentUICulture.TwoLetterISOLanguageName}.json", token: token).ConfigureAwait(false)).JsonString);
                        }, token).ConfigureAwait(false);
                    }
                }
                catch
                {
                    //l = new JObject();
                }
            }

            return (l, i);
        }

        private readonly object froniusHttpClientLockObject = new();

        public async ValueTask<(string JsonString, HttpStatusCode StatusCode)> GetFroniusStringResponse(string request,
            JToken? jToken = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
        {
            var client = await GetFroniusHttpClient(token);
            var result = await client.GetString(request, jToken?.ToString(), allowedStatusCodes, token).ConfigureAwait(false);
            lastSolarApiCall = DateTime.UtcNow;
            return result;
        }

        public async ValueTask<(JToken Token, HttpStatusCode StatusCode)> GetFroniusJsonResponse(string request,
            JToken? jToken = null, IEnumerable<HttpStatusCode>? allowedStatusCodes = null, CancellationToken token = default)
        {
            var client = await GetFroniusHttpClient(token);
            var result = await client.GetJsonToken(request, jToken, allowedStatusCodes, token).ConfigureAwait(false);
            lastSolarApiCall = DateTime.UtcNow;
            return result;
        }

        public async ValueTask<T?> SendFroniusCommand<T>(string request, JToken? jToken = null, CancellationToken token = default) where T : Gen24NoResultCommand, new()
        {
            var client = await GetFroniusHttpClient(token);
            var (result, statusCode) = await client.GetJsonToken(request, jToken, new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest }, token).ConfigureAwait(false);

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

        public ValueTask<Gen24StandByStatus?> GetInverterStandByStatus(CancellationToken token = default) =>
            SendFroniusCommand<Gen24StandByStatus>("commands/StandbyState", token: token);

        public async ValueTask RequestInverterStandBy(bool isStandBy, CancellationToken token = default)
        {
            var jToken = JObject.Parse($"{{\"requestState\": {(isStandBy ? "0" : "1")}}}");
            await SendFroniusCommand<Gen24NoResultCommand>("commands/StandbyRequestState", jToken, token).ConfigureAwait(false);
        }

        private async Task<DigestAuthHttp> GetFroniusHttpClient(CancellationToken token = default)
        {
            if (Connection?.BaseUrl == null)
            {
                throw new NullReferenceException(Resources.NoSystemConnection);
            }

            DigestAuthHttp client;

            lock (froniusHttpClientLockObject)
            {
                froniusHttpClient ??= new DigestAuthHttp(Connection ?? throw new ArgumentNullException(null, @"No inverter connection"));
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

        public override string ToString() => Connection?.BaseUrl ?? "---";
    }
}
