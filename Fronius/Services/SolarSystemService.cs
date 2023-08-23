using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.Fronius.Services;

public class SolarSystemService : BindableBase, ISolarSystemService
{
    private readonly IWebClientService webClientService;
    private readonly IWebClientService webClientService2;
    private readonly IWattPilotService wattPilotService;
    private readonly SettingsBase settings;
    private Timer? timer;
    private int updateSemaphore;
    private int fritzBoxCounter, froniusCounter;
    private int suspendFritzBoxCounter;
    private static int QueueSize => 1;
    private const int FritzBoxUpdateRate = 3;

    public event EventHandler<SolarDataEventArgs>? NewDataReceived;

    public SolarSystemService(SettingsBase settings, IWebClientService webClientService, IWattPilotService wattPilotService, IToshibaHvacService acService, SynchronizationContext context)
    {
        this.webClientService = webClientService;
        this.wattPilotService = wattPilotService;
        this.settings = settings;
        HvacService = acService;
        PowerFlowQueue = new Queue<Gen24PowerFlow>(QueueSize + 1);
        SwitchableDevices = new BindableCollection<ISwitchable>(context);
        var scope = IoC.Injector?.CreateScope();

        if (scope != null)
        {
            webClientService2 = scope.ServiceProvider.GetService(typeof(IWebClientService)) as IWebClientService ?? throw new NullReferenceException($"{nameof(IWebClientService)} not registered");
        }
    }

    public BindableCollection<ISwitchable> SwitchableDevices { get; }

    public IToshibaHvacService HvacService { get; }

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

    public IList<SmartMeterCalibrationHistoryItem> SmartMeterHistory { get; private set; } = new List<SmartMeterCalibrationHistoryItem>();

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

    public Task<IList<SmartMeterCalibrationHistoryItem>> ReadCalibrationHistory() => Task.Run(() =>
    {
        if (string.IsNullOrWhiteSpace(settings.DriftFileName) || !File.Exists(settings.DriftFileName))
        {
            return new List<SmartMeterCalibrationHistoryItem>();
        }

        var serializer = new XmlSerializer(typeof(List<SmartMeterCalibrationHistoryItem>));
        using var stream = new FileStream(settings.DriftFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        return serializer.Deserialize(stream) as IList<SmartMeterCalibrationHistoryItem> ?? new List<SmartMeterCalibrationHistoryItem>();
    });

    public Task<IList<SmartMeterCalibrationHistoryItem>> AddCalibrationHistoryItem(double consumedEnergyOffset, double producedEnergyOffset) => Task.Run(() =>
    {
        if (string.IsNullOrWhiteSpace(settings.DriftFileName))
        {
            return SmartMeterHistory;
        }

        var directoryName = Path.GetDirectoryName(settings.DriftFileName);

        if (!Directory.Exists(Path.GetDirectoryName(settings.DriftFileName)))
        {
            try
            {
                Directory.CreateDirectory(directoryName!);
            }
            catch
            {
                return SmartMeterHistory;
            }
        }

        var newItem = new SmartMeterCalibrationHistoryItem
        {
            CalibrationDate = DateTime.UtcNow,
            ConsumedOffset = consumedEnergyOffset,
            ProducedOffset = producedEnergyOffset,
            EnergyRealConsumed = SolarSystem?.Gen24System?.PrimaryPowerMeter?.EnergyRealConsumed ?? double.NaN,
            EnergyRealProduced = SolarSystem?.Gen24System?.PrimaryPowerMeter?.EnergyRealProduced ?? double.NaN,
        };

        SmartMeterHistory.Add(newItem);

        if (SmartMeterHistory.Count > 50)
        {
            SmartMeterHistory.RemoveAt(1);
        }

        var serializer = new XmlSerializer(typeof(List<SmartMeterCalibrationHistoryItem>));
        using var stream = new FileStream(settings.DriftFileName, FileMode.Create, FileAccess.Write, FileShare.None);

        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = new string(' ', 3),
            NewLineChars = Environment.NewLine,
        });

        serializer.Serialize(writer, SmartMeterHistory as List<SmartMeterCalibrationHistoryItem> ?? SmartMeterHistory.ToList());

        return SmartMeterHistory;
    });

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
        SmartMeterHistory = await ReadCalibrationHistory().ConfigureAwait(false);

        if (inverterConnection != null)
        {
            webClientService.InverterConnection = inverterConnection;
            var inverter2Connection = (WebConnection)inverterConnection.Clone();
            inverter2Connection.BaseUrl = "https://solar2.hochstaetter.de";
            webClientService2.InverterConnection=inverter2Connection;
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
        catch (TaskCanceledException) { }

        result.WattPilot = wattPilotTask.IsCompletedSuccessfully ? wattPilotTask.Result : null;
        result.FritzBox = fritzBoxTask.IsCompletedSuccessfully ? fritzBoxTask.Result : null;
        return result;

        async Task GetInverterDataFromSolarApi()
        {
            try
            {
                result.Versions = Gen24Versions.Parse((await webClientService.GetFroniusJsonResponse("status/version").ConfigureAwait(false)).Token);
                result.Components = Gen24Components.Parse((await webClientService.GetFroniusJsonResponse("components/").ConfigureAwait(false)).Token);
                result.Versions2 = Gen24Versions.Parse((await webClientService2.GetFroniusJsonResponse("status/version").ConfigureAwait(false)).Token);
                result.Components2 = Gen24Components.Parse((await webClientService2.GetFroniusJsonResponse("components/").ConfigureAwait(false)).Token);
                result.Gen24System = await webClientService.GetFroniusData(result.Components).ConfigureAwait(false);
                result.Gen24System2 = await webClientService2.GetFroniusData(result.Components2).ConfigureAwait(false);
                result.Gen24Common = Gen24Common.Parse((await webClientService.GetFroniusJsonResponse("config/common").ConfigureAwait(false)).Token);
                result.Gen24Common2 = Gen24Common.Parse((await webClientService2.GetFroniusJsonResponse("config/common").ConfigureAwait(false)).Token);

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
                var devicesToRemove = switchableDevices.ExceptBy(powerConsumers.Select(p => p.Id), p => p.Id).ToList();
                var devicesToAdd = powerConsumers.ExceptBy(switchableDevices.Select(p => p.Id), p => p.Id).ToList();
                var devicesToUpdate = switchableDevices.IntersectBy(powerConsumers.Select(p => p.Id), p => p.Id).ToList();

                foreach (var oldDevice in devicesToUpdate)
                {
                    var newDevice = powerConsumers.Single(p => p.Id == oldDevice.Id);
                    oldDevice.CopyFrom(newDevice);
                }


                if (devicesToRemove.Count > 0)
                {
                    SwitchableDevices.RemoveRange(devicesToRemove);
                }

                if (devicesToAdd.Count > 0)
                {
                    SwitchableDevices.AddRange(devicesToAdd);
                }
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
        if (!HvacService.IsRunning)
        {
            await HvacService.Start().ConfigureAwait(false);

            if (!HvacService.IsRunning)
            {
                return;
            }

            SwitchableDevices.RemoveRange(SwitchableDevices.OfType<ToshibaHvacMappingDevice>().ToList());
            SwitchableDevices.AddRange(HvacService.AllDevices!.SelectMany(d => d.Devices));
        }
    }

    private async Task<Gen24System?> TryGetGen24System(IWebClientService? localWebClientService, Gen24Components? components)
    {
        if (localWebClientService is null || components is null)
        {
            return null;
        }

        try
        {
            if (SolarSystem?.Components == null) return null;
            return await localWebClientService.GetFroniusData(components).ConfigureAwait(false);
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

        try
        {
            bool newSolarData;
            bool newFritzBoxData;

            if (!settings.HaveToshibaAc)
            {
                await HvacService.Stop().ConfigureAwait(false);
                SwitchableDevices.RemoveRange(SwitchableDevices.OfType<ToshibaHvacMappingDevice>().ToList());
            }

            if (SolarSystem?.PrimaryInverter == null || SolarSystem.Versions == null || SolarSystem.Components == null)
            {
                SolarSystem = await CreateSolarSystem(null, null).ConfigureAwait(false);
                newSolarData = true;
                newFritzBoxData = true;
            }
            else
            {
                var gen24Task = froniusCounter++ % FroniusUpdateRate == 0 ? TryGetGen24System(webClientService,SolarSystem.Components) : Task.FromResult<Gen24System?>(null);
                var gen24Task2 = froniusCounter++ % FroniusUpdateRate == 0 ? TryGetGen24System(webClientService2,SolarSystem.Components2) : Task.FromResult<Gen24System?>(null);

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
                    await Task.WhenAll(gen24Task2, gen24Task, fritzBoxTask, wattPilotTask, toshibaAcTask).ConfigureAwait(false);
                }
                catch (TaskCanceledException) { }

                SolarSystem.WattPilot = wattPilotTask.IsCompletedSuccessfully ? wattPilotTask.Result ?? SolarSystem.WattPilot : SolarSystem.WattPilot;
                SolarSystem.FritzBox = fritzBoxTask.IsCompletedSuccessfully ? fritzBoxTask.Result ?? SolarSystem.FritzBox : null;
                SolarSystem.Gen24System = gen24Task.IsCompletedSuccessfully ? gen24Task.Result ?? SolarSystem.Gen24System : null;
                SolarSystem.Gen24System2 = gen24Task2.IsCompletedSuccessfully ? gen24Task2.Result ?? SolarSystem.Gen24System2 : null;
                newFritzBoxData = fritzBoxTask is { IsCompletedSuccessfully: true, Result: { } };
                newSolarData = gen24Task is { IsCompletedSuccessfully: true, Result: { } };
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
