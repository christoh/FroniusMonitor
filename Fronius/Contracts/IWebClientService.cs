using De.Hochstaetter.Fronius.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Models.Gen24;
using Newtonsoft.Json.Linq;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IWebClientService
    {
        WebConnection? InverterConnection { get; set; }
        WebConnection? FritzBoxConnection { get; set; }
        Task<Gen24System> GetFroniusData();
        Task<SystemDevices> GetDevices();
        Task<InverterDevices> GetInverters();
        Task<StorageDevices> GetStorageDevices();
        Task<SmartMeterDevices> GetMeterDevices();
        Task<PowerFlow> GetPowerFlow();
        Task FritzBoxLogin();
        Task<FritzBoxDeviceList> GetFritzBoxDevices();
        Task TurnOnFritzBoxDevice(string ain);
        Task TurnOffFritzBoxDevice(string ain);
        Task SetFritzBoxLevel(string ain, double level);
        Task SetFritzBoxColorTemperature(string ain, double temperatureKelvin);
        Task SetFritzBoxColor(string ain, double hueDegrees, double saturation);
        Task<IOrderedEnumerable<Gen24Event>> GetFroniusEvents();
        Task<T> ReadGen24Entity<T>(string request) where T : new();
        JToken GetUpdateToken<T>(T newEntity, T? oldEntity = default) where T : BindableBase;
    }
}
