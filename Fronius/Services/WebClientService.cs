using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using De.Hochstaetter.Fronius.Localization;
using System.Globalization;
using De.Hochstaetter.Fronius.Exceptions;

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

                    foreach (var device in typeProperty.Children<JObject>().Select(d => d.Value<JObject>() ?? throw new InvalidDataException(Resources.IncorrectData)).Single())
                    {
                        var deviceInfo = new DeviceInfo
                        {
                            DeviceClass = deviceClass,
                            Id = int.Parse(device.Key, NumberStyles.Integer, CultureInfo.InvariantCulture),
                            SerialNumber = device.Value?["Serial"]?.Value<string>()?.Trim(),
                            DeviceType = device.Value?["DT"]?.Value<int>() ?? ~0
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

            return await Task.Run(() =>
            {
                foreach (var device in dataToken.OfType<JProperty>())
                {
                    var inverter = ParseInverter(device);
                    result.Inverters.Add(inverter);
                }

                return result;
            }).ConfigureAwait(false);
        }

        private static Inverter ParseInverter(JProperty device)
        {
            var deviceValues = device.Value;

            return new Inverter
            {
                Id = int.Parse(device.Name, NumberStyles.Integer, CultureInfo.InvariantCulture),
                CustomName = deviceValues["CustomName"]?.Value<string>(),
                DeviceType = deviceValues["DT"]?.Value<int>() ?? ~0,
                ErrorCode = deviceValues["ErrorCode"]?.Value<int>() ?? ~0,
                MaxPvPowerWatts = deviceValues["PVPower"]?.Value<double>() ?? double.NaN,
                Show = deviceValues["Show"]?.Value<bool>() ?? true,
                Status = (InverterStatus)(deviceValues["StatusCode"]?.Value<int>() ?? 254),
                SerialNumber = deviceValues["UniqueID"]?.Value<string>()
            };
        }

        private async Task<(T, JToken)> GetResponse<T>(string request, string? debugString = null) where T : ResponseBase, new()
        {
            if (inverterConnection == null)
            {
                throw new NullReferenceException(Resources.NoSystemConnection);
            }

            string jsonString;
            var requestString = $"{inverterConnection.BaseUrl}/solar_api/v1/{request}";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(requestString).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                jsonString = debugString ?? await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            return await Task.Run(() =>
            {
                var headToken = JObject.Parse(jsonString)?["Head"] ?? throw new InvalidDataException(Resources.IncorrectData);
                var statusToken = headToken["Status"] ?? throw new InvalidDataException(Resources.IncorrectData);

                var result = new T
                {
                    StatusCode = statusToken["Code"]?.Value<int>() ?? throw new InvalidDataException(Resources.IncorrectData),
                    Timestamp = headToken["Timestamp"]?.Value<DateTime>() ?? DateTime.MinValue,
                    Reason = statusToken["Reason"]?.Value<string>() ?? string.Empty,
                    UserMessage = statusToken["UserMessage"]?.Value<string>() ?? string.Empty
                };

                if (result.StatusCode != 0)
                {
                    throw new SolarException(result.StatusCode, result.Reason, result.UserMessage, requestString);
                }

                var data = JObject.Parse(jsonString)?["Body"]?["Data"] ?? throw new InvalidDataException(Resources.IncorrectData);
                return (result, data);
            }).ConfigureAwait(false);
        }
    }
}
