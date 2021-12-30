using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Exceptions;
using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.Fronius.Models;
using Newtonsoft.Json.Linq;

namespace De.Hochstaetter.Fronius.Services
{
    public class WebClientService : BindableBase, IWebClientService
    {
        private InverterConnection? inverterConnection;

        public InverterConnection? InverterConnection
        {
            get => inverterConnection;
            set => Set(ref inverterConnection, value);
        }

        public async Task<SystemDevices> GetDevices()
        {
            var (result, dataToken) = await GetResponse<SystemDevices>("GetActiveDeviceInfo.cgi?DeviceClass=System").ConfigureAwait(false);

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
            var (result, dataToken) = await GetResponse<InverterDevices>("GetInverterInfo.cgi").ConfigureAwait(false);

            foreach (var device in dataToken.OfType<JProperty>())
            {
                var id = int.Parse(device.Name, NumberStyles.Integer, CultureInfo.InvariantCulture);
                var (data, common) = await GetResponse<BaseResponse>($"GetInverterRealtimeData.cgi?Scope=Device&DataCollection=CommonInverterData&DeviceId={id}").ConfigureAwait(false);
                var (threePhasesData, threePhases) = await GetResponse<BaseResponse>($"GetInverterRealtimeData.cgi?Scope=Device&DataCollection=3PInverterData&DeviceId={id}").ConfigureAwait(false);
                var inverter = await ParseInverter(id, device, common, threePhases, data, threePhasesData).ConfigureAwait(false);
                result.Inverters.Add(inverter);
            }

            return result;
        }

        public async Task<SmartMeterDevices> GetMeterDevices()
        {
            var (result, dataToken) = await GetResponse<SmartMeterDevices>("GetMeterRealtimeData.cgi").ConfigureAwait(false);

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
            var (result, dataToken) = await GetResponse<StorageDevices>("GetStorageRealtimeData.cgi?Scope=System").ConfigureAwait(false);

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
            var (response, dataToken) = await GetResponse<BaseResponse>("GetPowerFlowRealtimeData.fcgi").ConfigureAwait(false);

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

                SiteType = site?["Mode"]?.Value<string?>() switch
                {
                    "produce-only" => SiteType.ProduceOnly,
                    "meter" => SiteType.Meter,
                    "vague-meter" => SiteType.VagueMeter,
                    "bidirectional" => SiteType.BiDirectional,
                    "ac-coupled" => SiteType.AcCoupled,
                    null => null,
                    _ => SiteType.Unknown,
                },
            };

            return result;
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
                IsVisible = meterToken["Visible"]?.Value<bool>() ?? true,
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

        private async Task<(T, JToken)> GetResponse<T>(string request, string? debugString = null) where T : BaseResponse, new()
        {
            if (InverterConnection == null)
            {
                throw new NullReferenceException(Resources.NoSystemConnection);
            }

            string jsonString;
            var requestString = $"{InverterConnection.BaseUrl}/solar_api/v1/{request}";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(requestString).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                jsonString = debugString ?? await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
}
