using De.Hochstaetter.Fronius.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IWebClientService
    {
        InverterConnection? InverterConnection{get;set;}
        Task<SystemDevices> GetDevices();
        Task<InverterDevices> GetInverters();
        Task<StorageDevices> GetStorageDevices();
        Task<SmartMeterDevices> GetMeterDevices();
        Task<PowerFlow> GetPowerFlow();
    }
}
