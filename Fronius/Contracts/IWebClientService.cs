using De.Hochstaetter.Fronius.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IWebClientService
    {
        WebConnection? InverterConnection { get; set; }
        WebConnection? FritzBoxConnection { get; set; }
        Task<SystemDevices> GetDevices();
        Task<InverterDevices> GetInverters();
        Task<StorageDevices> GetStorageDevices();
        Task<SmartMeterDevices> GetMeterDevices();
        Task<PowerFlow> GetPowerFlow();
        Task FritzBoxLogin();
        Task<FritzBoxDeviceList> GetFritzBoxDevices();
    }
}
