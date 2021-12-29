using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.Fronius.Services
{
    public class SolarSystemService : BindableBase, ISolarSystemService
    {
        private readonly IWebClientService webClientService;
        private Timer? timer;
        private int updateSemaphore;

        public event EventHandler<SolarDataEventArgs>? NewDataReceived; 

        public SolarSystemService(IWebClientService webClientService)
        {
            this.webClientService = webClientService;
        }

        private SolarSystem? solarSystem;

        public SolarSystem? SolarSystem
        {
            get => solarSystem;
            private set => Set(ref solarSystem, value);
        }

        public async Task Start(InverterConnection connection)
        {
            if (timer == null || SolarSystem == null)
            {
                Stop();
            }

            SolarSystem = await CreateSolarSystem(connection).ConfigureAwait(false);
            timer = new Timer(TimerElapsed, null, 0, 1000);
        }

        private string lastConnectionError = string.Empty;

        public string LastConnectionError
        {
            get => lastConnectionError;
            set => Set(ref lastConnectionError, value);
        }

        private bool isConnected;

        public bool IsConnected
        {
            get => isConnected;
            set => Set(ref isConnected, value);
        }

        public void Stop()
        {
            timer?.Dispose();
            timer = null;
        }

        private async Task<SolarSystem> CreateSolarSystem(InverterConnection connection)
        {
            var result = new SolarSystem();
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
                }

                foreach (var device in deviceGroup)
                {
                    var group = new DeviceGroup {DeviceClass = deviceGroup.Key};

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
                    }

                    result.DeviceGroups.Add(group);
                }
            }

            await Task.Run(() => NewDataReceived?.Invoke(this, new SolarDataEventArgs(result))).ConfigureAwait(false);
            return result;
        }

        public async void TimerElapsed(object? _)
        {
            if (SolarSystem == null || Interlocked.Exchange(ref updateSemaphore, 1) != 0)
            {
                return;
            }

            try
            {
                SolarSystem.PowerFlow = await webClientService.GetPowerFlow().ConfigureAwait(false);

                var storageDevices = await webClientService.GetStorageDevices().ConfigureAwait(false);

                foreach (var storage in SolarSystem.Storages)
                {
                    var newStorage = storageDevices.Storages.SingleOrDefault(s => s.Id == storage.Id);

                    if (newStorage != null)
                    {
                        storage.Data = newStorage.Data;
                    }
                }

                var inverterDevices = await webClientService.GetInverters().ConfigureAwait(false);

                foreach (var inverter in SolarSystem.Inverters)
                {
                    var newInverter = inverterDevices.Inverters.SingleOrDefault(i => i.Id == inverter.Id);

                    if (newInverter == null)
                    {
                        continue;
                    }

                    inverter.ThreePhasesData = newInverter.ThreePhasesData;
                    inverter.Data = newInverter.Data;
                }

                var meterDevices = await webClientService.GetMeterDevices().ConfigureAwait(false);

                foreach (var smartMeter in SolarSystem.Meters)
                {
                    var newSmartMeter = meterDevices.SmartMeters.SingleOrDefault(m => m.Id == smartMeter.Id);

                    if (newSmartMeter != null)
                    {
                        smartMeter.Data = newSmartMeter.Data;
                    }
                }

                IsConnected = true;
                await Task.Run(() => NewDataReceived?.Invoke(this, new SolarDataEventArgs(SolarSystem))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastConnectionError = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                Interlocked.Exchange(ref updateSemaphore, 0);
            }
        }
    }
}
