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

    public async Task<SystemDevices> GetDevices()
    {
        var (result, dataToken) = await GetJsonResponse<SystemDevices>("GetActiveDeviceInfo.cgi?DeviceClass=System").ConfigureAwait(false);

        return await Task.Run(() =>
        {
            foreach (var typeProperty in dataToken.OfType<JProperty>())
            {
                if (!Enum.TryParse<DeviceClass>(typeProperty.Name, out var deviceClass))
                {
                    deviceClass = DeviceClass.Unknown;
                }

                foreach (var (key, value) in typeProperty.Children<JObject>().Select(d => d.Value<JObject>() ?? throw new InvalidDataException(Resources.IncorrectData)).Single())
                {
                    var deviceInfo = new DeviceInfo
                    {
                        DeviceClass = deviceClass,
                        Id = int.Parse(key, NumberStyles.Integer, CultureInfo.InvariantCulture),
                        SerialNumber = value?["Serial"]?.Value<string>()?.Trim(),
                        DeviceType = value?["DT"]?.Value<int>() ?? ~0
                    };

                    result.Devices.Add(deviceInfo);
                }
            }

            return result;
        }).ConfigureAwait(false);
    }

    public async Task<InverterDevices> GetInverters()
    {
        var (result, dataToken) = await GetJsonResponse<InverterDevices>("GetInverterInfo.cgi").ConfigureAwait(false);

        foreach (var device in dataToken.OfType<JProperty>())
        {
            var id = int.Parse(device.Name, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var (data, common) = await GetJsonResponse<BaseResponse>($"GetInverterRealtimeData.cgi?Scope=Device&DataCollection=CommonInverterData&DeviceId={id}").ConfigureAwait(false);
            var (threePhasesData, threePhases) = await GetJsonResponse<BaseResponse>($"GetInverterRealtimeData.cgi?Scope=Device&DataCollection=3PInverterData&DeviceId={id}").ConfigureAwait(false);
            var inverter = await ParseInverter(id, device, common, threePhases, data, threePhasesData).ConfigureAwait(false);
            result.Inverters.Add(inverter);
        }

        return result;
    }

    public async Task<SmartMeterDevices> GetMeterDevices()
    {
        var (result, dataToken) = await GetJsonResponse<SmartMeterDevices>("GetMeterRealtimeData.cgi").ConfigureAwait(false);

        return await Task.Run(() =>
        {
            foreach (var device in dataToken.OfType<JProperty>())
            {
                var meterToken = device.Value;
                var detailsToken = device.Value["Details"] ?? throw new InvalidDataException(Resources.IncorrectData);
                var data = ParseSmartMeterData(meterToken);

                var smartMeter = new SmartMeter
                {
                    DeviceType = -1,
                    Id = int.Parse(device.Name, NumberStyles.Integer, CultureInfo.InvariantCulture),
                    Manufacturer = detailsToken["Manufacturer"]?.Value<string>() ?? string.Empty,
                    Model = detailsToken["Model"]?.Value<string>() ?? string.Empty,
                    SerialNumber = detailsToken["Serial"]?.Value<string>() ?? string.Empty,
                    MeterLocationCurrent = (int)Math.Round(meterToken["Meter_Location_Current"]?.Value<double>() ?? -1, MidpointRounding.AwayFromZero),
                    Data = data,
                };

                result.SmartMeters.Add(smartMeter);
            }

            return result;
        }).ConfigureAwait(false);
    }

    public async Task<StorageDevices> GetStorageDevices()
    {
        var (result, dataToken) = await GetJsonResponse<StorageDevices>("GetStorageRealtimeData.cgi?Scope=System").ConfigureAwait(false);

        return await Task.Run(() =>
        {
            foreach (var device in dataToken.OfType<JProperty>())
            {
                var storageToken = device.Value["Controller"] ?? throw new InvalidDataException(Resources.IncorrectData);
                var detailsToken = storageToken["Details"] ?? throw new InvalidDataException(Resources.IncorrectData);

                var data = new StorageData
                {
                    Manufacturer = detailsToken["Manufacturer"]?.Value<string>() ?? string.Empty,
                    MaximumCapacityWattHours = storageToken["Capacity_Maximum"]?.Value<double>() ?? double.NaN,
                    Current = storageToken["Current_DC"]?.Value<double>() ?? double.NaN,
                    DesignedCapacityWattHours = storageToken["DesignedCapacity"]?.Value<double>() ?? double.NaN,
                    IsEnabled = storageToken["Enable"]?.Value<bool>() ?? true,
                    StateOfCharge = (storageToken["StateOfCharge_Relative"]?.Value<double>() ?? double.NaN) / 100,
                    StatusBatteryCell = (int)(storageToken["Status_BatteryCell"]?.Value<double>() ?? -1),
                    TemperatureCelsius = storageToken["Temperature_Cell"]?.Value<double>() ?? double.NaN,
                    StorageTimestamp = DateTime.UnixEpoch.AddSeconds(storageToken["TimeStamp"]?.Value<double>() ?? 0),
                    Voltage = storageToken["Voltage_DC"]?.Value<double>() ?? double.NaN,
                };

                var storage = new Storage
                {
                    DeviceType = -1,
                    Id = int.Parse(device.Name, NumberStyles.Integer, CultureInfo.InvariantCulture),
                    SerialNumber = detailsToken["Serial"]?.Value<string>()?.Trim(),
                    Model = detailsToken["Model"]?.Value<string>() ?? string.Empty,
                    Data = data,
                };

                result.Storages.Add(storage);
            }

            return result;
        }).ConfigureAwait(false);
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<PowerFlow> GetPowerFlow()
    {
        var (response, dataToken) = await GetJsonResponse<BaseResponse>("GetPowerFlowRealtimeData.fcgi").ConfigureAwait(false);

        var site = dataToken["Site"];

        var result = new PowerFlow
        {
            Timestamp = response.Timestamp,
            Reason = response.Reason,
            UserMessage = response.UserMessage,
            StatusCode = response.StatusCode,
            Version = dataToken["Version"]?.Value<int>(),
            BackupMode = site?["BackupMode"]?.Value<bool?>(),
            StorageStandby = site?["BatteryStandby"]?.Value<bool?>(),
            DayEnergyWattHours = site?["E_Day"]?.Value<double?>(),
            TotalEnergyWattHours = site?["E_Total"]?.Value<double?>(),
            YearEnergyWattHours = site?["E_Year"]?.Value<double?>(),
            StoragePower = site?["P_Akku"]?.Value<double?>(),
            GridPower = site?["P_Grid"]?.Value<double?>(),
            LoadPower = site?["P_Load"]?.Value<double?>(),
            SolarPower = site?["P_PV"]?.Value<double?>(),
            Autonomy = site?["rel_Autonomy"]?.Value<double?>() / 100,
            SelfConsumption = site?["rel_SelfConsumption"]?.Value<double?>() / 100,

            MeterLocation = site?["Meter_Location"]?.Value<string?>() switch
            {
                "grid" => MeterLocation.Grid,
                "load" => MeterLocation.Load,
                null => null,
                _ => MeterLocation.Unknown,
            },

            SiteType = (SiteType?)gen24JsonService.ReadEnum(typeof(SiteType), site?["Mode"]?.Value<string?>()),
        };

        return result;
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

    private static SmartMeterData ParseSmartMeterData(JToken meterToken)
    {
        return new SmartMeterData
        {
            L1Current = meterToken["Current_AC_Phase_1"]?.Value<double>() ?? double.NaN,
            L2Current = meterToken["Current_AC_Phase_2"]?.Value<double>() ?? double.NaN,
            L3Current = meterToken["Current_AC_Phase_3"]?.Value<double>() ?? double.NaN,
            TotalCurrent = meterToken["Current_AC_Sum"]?.Value<double>() ?? double.NaN,
            IsEnabled = meterToken["Enable"]?.Value<bool>() ?? true,
            ReactiveEnergyConsumedWattHours = meterToken["EnergyReactive_VArAC_Sum_Consumed"]?.Value<double>() ?? double.NaN,
            ReactiveEnergyProducedWattHours = meterToken["EnergyReactive_VArAC_Sum_Produced"]?.Value<double>() ?? double.NaN,
            Frequency = meterToken["Frequency_Phase_Average"]?.Value<double>() ?? double.NaN,
            L1ApparentPower = meterToken["PowerApparent_S_Phase_1"]?.Value<double>() ?? double.NaN,
            L2ApparentPower = meterToken["PowerApparent_S_Phase_2"]?.Value<double>() ?? double.NaN,
            L3ApparentPower = meterToken["PowerApparent_S_Phase_3"]?.Value<double>() ?? double.NaN,
            TotalApparentPower = meterToken["PowerApparent_S_Sum"]?.Value<double>() ?? double.NaN,
            RealEnergyAbsoluteMinusWattHours = meterToken["EnergyReal_WAC_Minus_Absolute"]?.Value<double>() ?? double.NaN,
            RealEnergyAbsolutePlusWattHours = meterToken["EnergyReal_WAC_Plus_Absolute"]?.Value<double>() ?? double.NaN,
            RealEnergyConsumedWattHours = meterToken["EnergyReal_WAC_Sum_Consumed"]?.Value<double>() ?? double.NaN,
            RealEnergyProducedWattHours = meterToken["EnergyReal_WAC_Sum_Produced"]?.Value<double>() ?? double.NaN,
            L1PowerFactor = meterToken["PowerFactor_Phase_1"]?.Value<double>() ?? double.NaN,
            L2PowerFactor = meterToken["PowerFactor_Phase_2"]?.Value<double>() ?? double.NaN,
            L3PowerFactor = meterToken["PowerFactor_Phase_3"]?.Value<double>() ?? double.NaN,
            TotalPowerFactor = meterToken["PowerFactor_Sum"]?.Value<double>() ?? double.NaN,
            L1ReactivePower = meterToken["PowerReactive_Q_Phase_1"]?.Value<double>() ?? double.NaN,
            L2ReactivePower = meterToken["PowerReactive_Q_Phase_2"]?.Value<double>() ?? double.NaN,
            L3ReactivePower = meterToken["PowerReactive_Q_Phase_3"]?.Value<double>() ?? double.NaN,
            TotalReactivePower = meterToken["PowerReactive_Q_Sum"]?.Value<double>() ?? double.NaN,
            L1RealPower = meterToken["PowerReal_P_Phase_1"]?.Value<double>() ?? double.NaN,
            L2RealPower = meterToken["PowerReal_P_Phase_2"]?.Value<double>() ?? double.NaN,
            L3RealPower = meterToken["PowerReal_P_Phase_3"]?.Value<double>() ?? double.NaN,
            TotalRealPower = meterToken["PowerReal_P_Sum"]?.Value<double>() ?? double.NaN,
            MeterTimestamp = DateTime.UnixEpoch.AddSeconds(meterToken["TimeStamp"]?.Value<double>() ?? 0),
            IsVisible = meterToken["IsVisible"]?.Value<bool>() ?? true,
            L1L2Voltage = meterToken["Voltage_AC_PhaseToPhase_12"]?.Value<double>() ?? double.NaN,
            L2L3Voltage = meterToken["Voltage_AC_PhaseToPhase_23"]?.Value<double>() ?? double.NaN,
            L3L1Voltage = meterToken["Voltage_AC_PhaseToPhase_31"]?.Value<double>() ?? double.NaN,
            L1Voltage = meterToken["Voltage_AC_Phase_1"]?.Value<double>() ?? double.NaN,
            L2Voltage = meterToken["Voltage_AC_Phase_2"]?.Value<double>() ?? double.NaN,
            L3Voltage = meterToken["Voltage_AC_Phase_3"]?.Value<double>() ?? double.NaN,
        };
    }

    private static async Task<Inverter> ParseInverter(int id, JProperty device, JToken common, JToken threePhases, BaseResponse data, BaseResponse threePhasesData) => await Task.Run(() =>
    {
        var deviceValues = device.Value;

        var threePhasesDataNew = new ThreePhasesData
        {
            L1Current = threePhases["IAC_L1"]?["Value"]?.Value<double?>(),
            L2Current = threePhases["IAC_L2"]?["Value"]?.Value<double?>(),
            L3Current = threePhases["IAC_L3"]?["Value"]?.Value<double?>(),
            L1Voltage = threePhases["UAC_L1"]?["Value"]?.Value<double?>(),
            L2Voltage = threePhases["UAC_L2"]?["Value"]?.Value<double?>(),
            L3Voltage = threePhases["UAC_L3"]?["Value"]?.Value<double?>(),
            Timestamp = threePhasesData.Timestamp,
            StatusCode = threePhasesData.StatusCode,
            Reason = threePhasesData.Reason,
            UserMessage = threePhasesData.Reason,
        };

        var dataNew = new InverterData
        {
            ErrorCode = common["DeviceStatus"]?["ErrorCode"]?.Value<int>() ?? ~0,
            TotalEnergyWattHours = common["TOTAL_ENERGY"]?["Value"]?.Value<double?>(),
            YearEnergyWattHours = common["YEAR_ENERGY"]?["Value"]?.Value<double?>(),
            DayEnergyWattHours = common["DAY_ENERGY"]?["Value"]?.Value<double?>(),
            Status = (InverterStatus)(common["DeviceStatus"]?["StatusCode"]?.Value<int>() ?? -1),
            CurrentString1 = common["IDC"]?["Value"]?.Value<double?>(),
            CurrentString2 = common["IDC_2"]?["Value"]?.Value<double?>(),
            CurrentString3 = common["IDC_3"]?["Value"]?.Value<double?>(),
            VoltageString1 = common["UDC"]?["Value"]?.Value<double?>(),
            VoltageString2 = common["UDC_2"]?["Value"]?.Value<double?>(),
            VoltageString3 = common["UDC_3"]?["Value"]?.Value<double?>(),
            Frequency = common["FAC"]?["Value"]?.Value<double?>(),
            TotalVoltage = common["UAC"]?["Value"]?.Value<double?>(),
            TotalCurrent = common["IAC"]?["Value"]?.Value<double?>(),
            AcPowerWatts = common["PAC"]?["Value"]?.Value<double?>(),
            AbsolutePowerToLoad = common["SAC"]?["Value"]?.Value<double?>(),
            Timestamp = data.Timestamp,
            StatusCode = data.StatusCode,
            Reason = data.Reason,
            UserMessage = data.Reason,
        };

        return new Inverter
        {
            Id = id,
            CustomName = deviceValues["CustomName"]?.Value<string>(),
            DeviceType = deviceValues["DT"]?.Value<int>() ?? ~0,
            MaxPvPowerWatts = deviceValues["PVPower"]?.Value<double>() ?? double.NaN,
            Show = deviceValues["Show"]?.Value<bool>() ?? true,
            SerialNumber = deviceValues["UniqueID"]?.Value<string>(),
            Data = dataNew,
            ThreePhasesData = threePhasesDataNew,
        };
    }).ConfigureAwait(false);

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
            {"username", FritzBoxConnection.UserName},
            {"response", response}
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
        {358, new[] {180, 112, 54}},
        {35, new[] {214, 140, 72}},
        {52, new[] {153, 102, 51}},
        {92, new[] {123, 79, 38}},
        {120, new[] {160, 82, 38}},
        {160, new[] {145, 84, 41}},
        {195, new[] {179, 118, 59}},
        {212, new[] {169, 110, 56}},
        {225, new[] {204, 135, 67}},
        {266, new[] {169, 110, 54}},
        {296, new[] {140, 92, 46}},
        {335, new[] {180, 107, 51}},
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
                new HttpClientHandler {ServerCertificateCustomValidationCallback = (_, _, _, _) => true,}
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
}
