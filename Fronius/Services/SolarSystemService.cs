using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.Fronius.Services;

public class SolarSystemService : BindableBase, ISolarSystemService
{
    private readonly IWebClientService webClientService;
    private readonly IWattPilotService wattPilotService;
    private Timer? timer;
    private int updateSemaphore;
    private int fritzBoxCounter, froniusCounter;
    private int suspendFritzBoxCounter;
    private static int QueueSize => 1;
    private const int FritzBoxUpdateRate = 3;

    public event EventHandler<SolarDataEventArgs>? NewDataReceived;

    public SolarSystemService(IWebClientService webClientService, IWattPilotService wattPilotService, IToshibaAirConditionService acService, SynchronizationContext context)
    {
        this.webClientService = webClientService;
        this.wattPilotService = wattPilotService;
        AcService = acService;
        PowerFlowQueue = new Queue<Gen24PowerFlow>(QueueSize + 1);
        SwitchableDevices = new BindableCollection<ISwitchable>(context);
    }

    public BindableCollection<ISwitchable> SwitchableDevices { get; }

    public IToshibaAirConditionService AcService { get; }

    private SolarSystem? solarSystem;

    public SolarSystem? SolarSystem
    {
        get => solarSystem;
        private set => Set(ref solarSystem, value);
    }

    private WebConnection? wattPilotConnection;

    public WebConnection? WattPilotConnection
    {
        get => wattPilotConnection;
        set => Set(ref wattPilotConnection, value);
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

    public int FroniusUpdateRate { get; set; }

    public Queue<Gen24PowerFlow> PowerFlowQueue { get; }
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
        NotifyOfPropertyChange(nameof(PowerLossAvg));
        NotifyOfPropertyChange(nameof(Efficiency));
    }

    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    public async Task Start(WebConnection? inverterConnection, WebConnection? fritzBoxConnection, WebConnection? wattPilotConnection)
    {
        Stop();
        WattPilotConnection = wattPilotConnection;
        PowerFlowQueue.Clear();
        NotifyPowerQueueChanged();
        SolarSystem = await CreateSolarSystem(inverterConnection, fritzBoxConnection).ConfigureAwait(false);
        await Task.Run(() => NewDataReceived?.Invoke(this, new SolarDataEventArgs(SolarSystem))).ConfigureAwait(false);
        _ = await webClientService.GetEventDescription("BYD2-46").ConfigureAwait(false);
        _ = await webClientService.GetConfigString("BATTERIES", "BAT_M0_SOC_MIN");
        timer = new Timer(TimerElapsed, null, 0, 1000);
    }

    public void Stop()
    {
        timer?.Dispose();
        timer = null;
        WattPilotConnection = null;
    }

    public void InvalidateFritzBox()
    {
        fritzBoxCounter = 0;
        timer?.Change(0, 1000);
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

    private async ValueTask<SolarSystem> CreateSolarSystem(WebConnection? inverterConnection, WebConnection? fritzBoxConnection)
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

        var fritzBoxTask = TryGetFritzBoxData();
        var wattPilotTask = TryStartWattPilot();
        var toshibaAcTask = TryStartToshibaAc();
        var inverterTask = GetInverterDataFromSolarApi();

        try
        {
            await Task.WhenAll(fritzBoxTask, wattPilotTask, inverterTask, toshibaAcTask).ConfigureAwait(false);
        }
        catch (AggregateException) { }

        result.WattPilot = wattPilotTask.IsCompletedSuccessfully ? wattPilotTask.Result : null;
        result.FritzBox = fritzBoxTask.IsCompletedSuccessfully ? fritzBoxTask.Result : null;
        return result;

        async Task GetInverterDataFromSolarApi()
        {
            try
            {
                result.Versions = Gen24Versions.Parse((await webClientService.GetFroniusJsonResponse("status/version").ConfigureAwait(false)).Token);
                result.Components = Gen24Components.Parse((await webClientService.GetFroniusJsonResponse("components/").ConfigureAwait(false)).Token);
                result.Gen24System = await webClientService.GetFroniusData(result.Components).ConfigureAwait(false);

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

                IsConnected = true;
            }
            catch
            {
                IsConnected = false;
                result.Gen24System = null;
                result.Versions = null;
            }
        }
    }

    private async Task<FritzBoxDeviceList?> TryGetFritzBoxData()
    {
        try
        {
            var devices = await webClientService.GetFritzBoxDevices().ConfigureAwait(false);

            await Task.Run(() =>
            {
                var powerConsumers = devices.PowerConsumers.Cast<FritzBoxDevice>().ToList();
                var switchableDevices = SwitchableDevices.OfType<FritzBoxDevice>().ToList();
                var devicesToRemove = switchableDevices.Where(s => !powerConsumers.Select(f => f.Id).Contains(s.Id)).ToList();
                var devicesToAdd = powerConsumers.Where(p => !switchableDevices.Select(f => f.Id).Contains(p.Id)).ToList();
                var devicesToUpdate = switchableDevices.Where(s => powerConsumers.Select(p => p.Id).Contains(s.Id)).ToList();

                foreach (var oldDevice in devicesToUpdate)
                {
                    var newDevice = powerConsumers.Single(p => p.Id == oldDevice.Id);

                    //foreach (var propertyInfo in typeof(FritzBoxDevice).GetProperties().Where(p => p is { CanWrite: true, CanRead: true }))
                    //{
                    //    var value = propertyInfo.GetValue(oldDevice);
                    //    propertyInfo.SetValue(newDevice, value);
                    //    oldDevice.Refresh();
                    //}
                    var index = SwitchableDevices.IndexOf(oldDevice);
                    SwitchableDevices[index] = newDevice;
                }

                SwitchableDevices.RemoveRange(devicesToRemove);
                SwitchableDevices.AddRange(devicesToAdd);
            }).ConfigureAwait(false);

            return devices;
        }
        catch
        {
            fritzBoxCounter = 0;
            throw;
        }
    }

    private async Task<WattPilot?> TryStartWattPilot()
    {
        if (wattPilotService.Connection == null && WattPilotConnection != null)
        {
            await wattPilotService.Start(WattPilotConnection).ConfigureAwait(false);
        }
        else if (WattPilotConnection == null)
        {
            await wattPilotService.Stop();
        }

        return wattPilotService.WattPilot;
    }

    private async Task TryStartToshibaAc()
    {
        if (!AcService.IsRunning)
        {
            await AcService.Start().ConfigureAwait(false);

            if (!AcService.IsRunning)
            {
                return;
            }

            SwitchableDevices.RemoveRange(SwitchableDevices.OfType<ToshibaAcMappingDevice>().ToList());
            SwitchableDevices.AddRange(AcService.AllDevices!.SelectMany(d => d.Devices));
        }
    }

    private async Task<Gen24System?> TryGetGen24System()
    {
        try
        {
            if (SolarSystem?.Components == null) return null;
            return await webClientService.GetFroniusData(SolarSystem.Components).ConfigureAwait(false);
        }
        catch
        {
            froniusCounter = 0;
            throw;
        }
    }

    private async void TimerElapsed(object? _)
    {
        if (Interlocked.Exchange(ref updateSemaphore, 1) != 0)
        {
            return;
        }

        var settings = IoC.Get<SettingsBase>();

        try
        {
            bool newSolarData;
            bool newFritzBoxData;

            if (!settings.HaveToshibaAc)
            {
                await AcService.Stop().ConfigureAwait(false);
                SwitchableDevices.RemoveRange(SwitchableDevices.OfType<ToshibaAcMappingDevice>().ToList());
            }

            if (SolarSystem?.PrimaryInverter == null || SolarSystem.Versions == null || SolarSystem.Components == null)
            {
                SolarSystem = await CreateSolarSystem(null, null).ConfigureAwait(false);
                newSolarData = true;
                newFritzBoxData = true;
            }
            else
            {
                var gen24Task = froniusCounter++ % FroniusUpdateRate == 0 ? TryGetGen24System() : Task.FromResult<Gen24System?>(null);

                if (webClientService.FritzBoxConnection == null && SwitchableDevices.Any(d => d is FritzBoxDevice))
                {
                    SwitchableDevices.RemoveRange(SwitchableDevices.OfType<FritzBoxDevice>().ToList());
                }

                var fritzBoxTask = suspendFritzBoxCounter <= 0 && webClientService.FritzBoxConnection != null &&
                                   fritzBoxCounter++ % FritzBoxUpdateRate == 0
                    ? TryGetFritzBoxData()
                    : Task.FromResult<FritzBoxDeviceList?>(null);

                var wattPilotTask = TryStartWattPilot();

                var toshibaAcTask = TryStartToshibaAc();

                try
                {
                    await Task.WhenAll(gen24Task, fritzBoxTask, wattPilotTask, toshibaAcTask).ConfigureAwait(false);
                }
                catch (AggregateException) { }

                SolarSystem.WattPilot = wattPilotTask.IsCompletedSuccessfully ? wattPilotTask.Result ?? SolarSystem.WattPilot : SolarSystem.WattPilot;
                SolarSystem.FritzBox = fritzBoxTask.IsCompletedSuccessfully ? fritzBoxTask.Result ?? SolarSystem.FritzBox : null;
                SolarSystem.Gen24System = gen24Task.IsCompletedSuccessfully ? gen24Task.Result ?? SolarSystem.Gen24System : null;
                newFritzBoxData = fritzBoxTask is {IsCompletedSuccessfully: true, Result: { }};
                newSolarData = gen24Task is {IsCompletedSuccessfully: true, Result: { }};
            }


            if (newSolarData && SolarSystem.Gen24System?.PowerFlow != null)
            {
                PowerFlowQueue.Enqueue(SolarSystem.Gen24System.PowerFlow);

                if (PowerFlowQueue.Count > QueueSize)
                {
                    PowerFlowQueue.Dequeue();
                }

                NotifyPowerQueueChanged();
            }

            IsConnected = true;
            if (newFritzBoxData || newSolarData) await Task.Run(() => NewDataReceived?.Invoke(this, new SolarDataEventArgs(SolarSystem))).ConfigureAwait(false);
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
