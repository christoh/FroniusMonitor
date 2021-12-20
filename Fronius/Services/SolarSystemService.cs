using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Services
{
    public class SolarSystemService : ISolarSystemService
    {
        private readonly IWebClientService webClientService;

        public SolarSystemService(IWebClientService webClientService)
        {
            this.webClientService = webClientService;
        }


        public async Task<SolarSystem> CreateSolarSystem(InverterConnection connection)
        {
            var solarSystem = new SolarSystem();
            webClientService.InverterConnection = connection;
            InverterDevices? inverterDevices = null;
            StorageDevices? storageDevices = null;
            SmartMeterDevices? smartMeterDevices = null;


            foreach (var deviceGroup in (await webClientService.GetDevices().ConfigureAwait(false)).Devices.AsParallel().GroupBy(d => d.DeviceClass))
            {
                switch (deviceGroup.Key)
                {
                    case DeviceClass.Inverter:
                        inverterDevices = await webClientService.GetInverters().ConfigureAwait(false);
                        break;
                    case DeviceClass.Storage:
                        storageDevices = await webClientService.GetStorageDevices().ConfigureAwait(false);
                        break;
                    case DeviceClass.Meter:
                        smartMeterDevices = await webClientService.GetMeterDevices().ConfigureAwait(false);
                        break;
                    default:
                        break;
                }

                foreach (var device in deviceGroup)
                {
                    var group = new DeviceGroup { DeviceClass = deviceGroup.Key };

                    switch (deviceGroup.Key)
                    {
                        case DeviceClass.Inverter:
                            group.Devices.Add(inverterDevices?.Inverters.SingleOrDefault(i => i.Id == device.Id) ?? device);
                            break;
                        case DeviceClass.Storage:
                            group.Devices.Add(storageDevices?.Storages.SingleOrDefault(s => s.Id == device.Id) ?? device);
                            break;
                        case DeviceClass.Meter:
                            group.Devices.Add(smartMeterDevices?.SmartMeters.SingleOrDefault(s => s.Id == device.Id) ?? device);
                            break;
                        default:
                            break;
                    }

                    solarSystem.DeviceGroups.Add(group);
                }
            }

            return solarSystem;
        }
    }
}
