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

namespace De.Hochstaetter.Fronius.Services
{
    public class WebClientService : BindableBase, IWebClientService
    {
        private InverterConnection inverterConnection = new() { BaseUrl = "http://192.168.44.10" };
        private HttpClient client = new();

        public InverterConnection InverterConnection
        {
            get => inverterConnection;

            set => Set(ref inverterConnection, value, () =>
            {
                client?.Dispose();
                client = new();
            });
        }

        public async Task<SystemDevices> GetDevices()
        {
            var result = new SystemDevices();

            var client = new HttpClient();
            var response = await client.GetAsync($"{inverterConnection.BaseUrl}/solar_api/v1/GetActiveDeviceInfo.cgi?DeviceClass=System").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JObject.Parse(jsonString)?["Body"]?["Data"];

            if (data == null)
            {
                throw new InvalidDataException(Resources.IncorrectData);
            }

            foreach (var typeToken in data)
            {
                if (typeToken is not JProperty typeProperty)
                {
                    continue;
                }


                if (!Enum.TryParse<DeviceClass>(typeProperty.Name, out var deviceClass))
                {
                    deviceClass = DeviceClass.Unknown;
                }


                foreach (var deviceObject in typeProperty.Children<JObject>().Select(d => d.Value<JObject>()))
                {
                    if (deviceObject == null)
                    {
                        continue;
                    }
                    foreach (var device in deviceObject)
                    {
                        var deviceInfo = new DeviceInfo();

                        if (!int.TryParse(device.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                        {
                            continue;
                        }

                        deviceInfo.DeviceClass = deviceClass;
                        deviceInfo.Id=id;
                        deviceInfo.SerialNumber=device.Value?["Serial"]?.Value<string>();

                        result.Devices.Add(deviceInfo);
                    }
                }
            }

            return result;
        }
    }
}
