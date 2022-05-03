using De.Hochstaetter.Fronius.Contracts;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.Fronius.Services;

public class SolarSystemService : BindableBase, ISolarSystemService
{
    private readonly IWebClientService webClientService;
    private Timer? timer;
    private int updateSemaphore;
    private int fritzBoxCounter, froniusCounter;
    private int suspendFritzBoxCounter;
    private const int QueueSize = 60;
    private const int FritzBoxUpdateRate = 3;
    private const int FroniusUpdateRate = 5;

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

    public Queue<Gen24PowerFlow> PowerFlowQueue { get; } = new(QueueSize + 1);
    private int? Count => PowerFlowQueue.Count == 0 ? null : PowerFlowQueue.Count;

    public double GridPowerSum => PowerFlowQueue.Sum(p => p.GridPower ?? 0);
    public double LoadPowerSum => PowerFlowQueue.Sum(p => p.LoadPower ?? 0);
    public double SolarPowerSum => PowerFlowQueue.Sum(p => p.SolarPower ?? 0);
    public double StoragePowerSum => PowerFlowQueue.Sum(p => p.StoragePower ?? 0);
    public double AcPowerSum => GridPowerSum + LoadPowerSum;
    public double DcPowerSum => SolarPowerSum + StoragePowerSum;
    public double PowerLossSum => AcPowerSum + DcPowerSum;
    public double? PowerLossAvg => PowerLossSum / Count;
    public double? Efficiency => -AcPowerSum / DcPowerSum;
    public double? GridPowerAvg => GridPowerSum / Count;
    public double? LoadPowerAvg => LoadPowerSum / Count;
    public double? StoragePowerAvg => StoragePowerSum / Count;
    public double? SolarPowerAvg => SolarPowerSum / Count;

    private void NotifyPowerQueueChanged()
    {
        NotifyOfPropertyChange(nameof(GridPowerSum));
        NotifyOfPropertyChange(nameof(LoadPowerSum));
        NotifyOfPropertyChange(nameof(SolarPowerSum));
        NotifyOfPropertyChange(nameof(StoragePowerSum));
        NotifyOfPropertyChange(nameof(GridPowerAvg));
        NotifyOfPropertyChange(nameof(LoadPowerAvg));
        NotifyOfPropertyChange(nameof(SolarPowerAvg));
        NotifyOfPropertyChange(nameof(StoragePowerAvg));
        NotifyOfPropertyChange(nameof(DcPowerSum));
        NotifyOfPropertyChange(nameof(AcPowerSum));
        NotifyOfPropertyChange(nameof(PowerLossSum));
        NotifyOfPropertyChange(nameof(Efficiency));
    }

    public async Task Start(WebConnection? inverterConnection, WebConnection? fritzBoxConnection)
    {
        if (timer == null || SolarSystem == null)
        {
            Stop();
        }

        PowerFlowQueue.Clear();
        NotifyPowerQueueChanged();
        SolarSystem = await CreateSolarSystem(inverterConnection, fritzBoxConnection).ConfigureAwait(false);
        timer = new Timer(TimerElapsed, null, 0, 1000);
    }

    public void Stop()
    {
        timer?.Dispose();
        timer = null;
    }

    public void SuspendPowerConsumers()
    {
        Interlocked.Increment(ref suspendFritzBoxCounter);
    }

    public void ResumePowerConsumers()
    {
        Interlocked.Decrement(ref suspendFritzBoxCounter);

        if (suspendFritzBoxCounter < 0)
        {
            Interlocked.Exchange(ref suspendFritzBoxCounter, 0);
        }
    }

    private async Task<SolarSystem> CreateSolarSystem(WebConnection? inverterConnection, WebConnection? fritzBoxConnection)
    {
        var result = new SolarSystem();

        if (inverterConnection != null)
        {
            webClientService.InverterConnection = inverterConnection;
        }

        if (fritzBoxConnection != null)
        {
            webClientService.FritzBoxConnection = fritzBoxConnection;
        }

        InverterDevices? inverterDevices = null;
        StorageDevices? storageDevices = null;
        SmartMeterDevices? smartMeterDevices = null;

        try
        {
            result.FritzBox = await webClientService.GetFritzBoxDevices().ConfigureAwait(false);
        }
        catch
        {
            result.FritzBox = null;
        }

        result.Gen24System = await webClientService.GetFroniusData().ConfigureAwait(false);

        foreach (var deviceGroup in (await webClientService.GetDevices().ConfigureAwait(false)).Devices.GroupBy(d => d.DeviceClass))
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
                }

                result.DeviceGroups.Add(group);
            }
        }

        return result;
    }

    public async void TimerElapsed(object? _)
    {
        if (Interlocked.Exchange(ref updateSemaphore, 1) != 0)
        {
            return;
        }

        try
        {
            if (froniusCounter++ % FroniusUpdateRate == 0)
            {
                if (SolarSystem == null)
                {
                    SolarSystem = await CreateSolarSystem(null, null).ConfigureAwait(false);
                }
                else
                {

                    SolarSystem.Gen24System = await webClientService.GetFroniusData().ConfigureAwait(false);


                    if (SolarSystem.Gen24System.PowerFlow != null)
                    {
                        PowerFlowQueue.Enqueue(SolarSystem.Gen24System.PowerFlow);

                        if (PowerFlowQueue.Count > QueueSize)
                        {
                            PowerFlowQueue.Dequeue();
                        }

                        NotifyPowerQueueChanged();
                    }

                    IsConnected = true;
                }
            }


            try
            {
                if (suspendFritzBoxCounter <= 0 && webClientService.FritzBoxConnection != null && SolarSystem != null && (SolarSystem.FritzBox == null || fritzBoxCounter++ % FritzBoxUpdateRate == 0))
                {
                    SolarSystem.FritzBox = await webClientService.GetFritzBoxDevices().ConfigureAwait(false);
                }
            }
            catch
            {
                fritzBoxCounter = 0;

                if (SolarSystem != null)
                {
                    SolarSystem.FritzBox = null;
                }
            }

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