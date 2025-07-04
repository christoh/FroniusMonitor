﻿namespace De.Hochstaetter.Fronius.Services
{
    public class DataCollectionService : BindableBase, IDataCollectionService
    {
        private readonly SettingsBase settings;
        private readonly IWattPilotService wattPilotService;

        private static readonly TimeSpan webRequestTimeOut = TimeSpan.FromSeconds(100000);
        private Timer? timer;
        private DateTime lastConfigUpdate = DateTime.UnixEpoch;
        private int updateSemaphore;
        private int fritzBoxCounter, froniusCounter;
        private int suspendFritzBoxCounter;
        private const int FritzBoxUpdateRate = 3;

        public event EventHandler<SolarDataEventArgs>? NewDataReceived;

        public DataCollectionService
        (
            SettingsBase settings,
            IGen24Service gen24Service,
            IFritzBoxService fritzBoxService,
            IWattPilotService wattPilotService,
            IToshibaHvacService hvacService,
            SynchronizationContext context
        )
        {
            Gen24Service = gen24Service;
            FritzBoxService = fritzBoxService;
            Container2 = IoC.Injector!.CreateScope().ServiceProvider;
            SwitchableDevices = new(context);
            HvacService = hvacService;
            this.settings = settings;
            this.wattPilotService = wattPilotService;

            if (settings.HaveTwoInverters)
            {
                Gen24Service2 = Container2.GetRequiredService<IGen24Service>();
            }
        }

        public IGen24Service Gen24Service { get; }
        public IFritzBoxService FritzBoxService { get; }

        public IServiceProvider Container => IoC.Injector!;

        public IServiceProvider Container2 { get; }

        public IGen24Service? Gen24Service2
        {
            get;
            set => Set(ref field, value);
        }

        public BindableCollection<ISwitchable> SwitchableDevices { get; }

        public IToshibaHvacService HvacService { get; }

        public HomeAutomationSystem? HomeAutomationSystem
        {
            get;
            private set => Set(ref field, value);
        }

        public WebConnection? WattPilotConnection
        {
            get;
            set => Set(ref field, value);
        }

        public string LastConnectionError
        {
            get;
            set => Set(ref field, value);
        } = string.Empty;

        public bool IsConnected
        {
            get;
            set => Set(ref field, value);
        }

        public int FroniusUpdateRate { get; set; }

        public IList<SmartMeterCalibrationHistoryItem> SmartMeterHistory { get; private set; } = [];

        public Task<IList<SmartMeterCalibrationHistoryItem>> ReadCalibrationHistory() => Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(settings.DriftFileName) || !File.Exists(settings.DriftFileName))
            {
                return [];
            }

            try
            {
                var serializer = new XmlSerializer(typeof(List<SmartMeterCalibrationHistoryItem>));
                using var stream = new FileStream(settings.DriftFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                return serializer.Deserialize(stream) as IList<SmartMeterCalibrationHistoryItem> ?? [];
            }
            catch (Exception)
            {
                return [];
            }
        });

        public async Task AddCalibrationHistoryItem(double consumedEnergyOffset, double producedEnergyOffset)
        {
            SmartMeterCalibrationHistoryItem? newItem = null;

            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(settings.DriftFileName))
                {
                    return;
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
                        return;
                    }
                }

                newItem = new SmartMeterCalibrationHistoryItem
                {
                    CalibrationDate = DateTime.UtcNow,
                    ConsumedOffset = consumedEnergyOffset,
                    ProducedOffset = producedEnergyOffset,
                    EnergyRealConsumed = HomeAutomationSystem?.Gen24Sensors?.PrimaryPowerMeter?.EnergyActiveConsumed ?? double.NaN,
                    EnergyRealProduced = HomeAutomationSystem?.Gen24Sensors?.PrimaryPowerMeter?.EnergyActiveProduced ?? double.NaN
                };
            }).ConfigureAwait(false);

            await AddCalibrationHistoryItem(newItem).ConfigureAwait(false);
        }

        public async Task AddCalibrationHistoryItem(SmartMeterCalibrationHistoryItem? newItem)
        {
            if (newItem == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                SmartMeterHistory.Add(newItem);

                var serializer = new XmlSerializer(typeof(List<SmartMeterCalibrationHistoryItem>));
                using var stream = new FileStream(settings.DriftFileName!, FileMode.Create, FileAccess.Write, FileShare.None);

                using var writer = XmlWriter.Create(stream, new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    IndentChars = new string(' ', 3),
                    NewLineChars = Environment.NewLine
                });

                serializer.Serialize(writer, SmartMeterHistory as List<SmartMeterCalibrationHistoryItem> ?? [.. SmartMeterHistory]);
            }).ConfigureAwait(false);
        }

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public async Task Start(WebConnection? gen24WebConnection, WebConnection? gen24WebConnection2, WebConnection? fritzBoxConnection, WebConnection? wattPilotConnection)
        {
            Stop();
            WattPilotConnection = wattPilotConnection;
            using var tokenSource = new CancellationTokenSource(webRequestTimeOut);

            try
            {
                HomeAutomationSystem = await CreateSolarSystem(gen24WebConnection, gen24WebConnection2, fritzBoxConnection, tokenSource.Token).ConfigureAwait(false);

                await Task.Run(() => NewDataReceived?.Invoke(this, new SolarDataEventArgs(HomeAutomationSystem)), tokenSource.Token).ConfigureAwait(false);
                _ = await Gen24Service.GetEventDescription("BYD2-46", tokenSource.Token).ConfigureAwait(false);
                _ = await Gen24Service.GetConfigString("BATTERIES.BAT_M0_SOC_MIN", tokenSource.Token).ConfigureAwait(false);
                _ = await Gen24Service.GetUiString("BATTERIES.BAT_M0_SOC_MIN", tokenSource.Token).ConfigureAwait(false);
                _ = await Gen24Service.GetChannelString("BATTERIES.BAT_M0_SOC_MIN", tokenSource.Token).ConfigureAwait(false);
            }
            catch
            {
                // No problem if we cannot get the data at first attempt
            }

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

        private async ValueTask<HomeAutomationSystem> CreateSolarSystem(WebConnection? gen24WebConnection, WebConnection? gen24WebConnection2, WebConnection? fritzBoxConnection, CancellationToken token)
        {
            var result = new HomeAutomationSystem();
            SmartMeterHistory = await ReadCalibrationHistory().ConfigureAwait(false);

            if (gen24WebConnection != null)
            {
                Gen24Service.Connection = gen24WebConnection;
            }

            if (Gen24Service2 != null && gen24WebConnection2 != null)
            {
                Gen24Service2.Connection = gen24WebConnection2;
            }

            if (fritzBoxConnection != null)
            {
                FritzBoxService.Connection = fritzBoxConnection;
            }

            var fritzBoxTask = TryGetFritzBoxData(token);
            var wattPilotTask = TryStartWattPilot();
            var toshibaAcTask = TryStartToshibaAc();
            var inverterTask = GetInverterData();

            try
            {
                await Task.WhenAll(fritzBoxTask, wattPilotTask, inverterTask, toshibaAcTask).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { }

            result.WattPilot = wattPilotTask.IsCompletedSuccessfully ? wattPilotTask.Result : null;
            result.FritzBox = fritzBoxTask.IsCompletedSuccessfully ? fritzBoxTask.Result : null;
            return result;

            async Task GetInverterData()
            {
                try
                {
                    await UpdateConfigData(result, token).ConfigureAwait(false);

                    var task1 = result.Gen24Config?.Components is not null ? Gen24Service.GetFroniusData(result.Gen24Config.Components, token) : Task.FromResult<Gen24Sensors?>(null)!;
                    var task2 = result.Gen24Config2?.Components is not null && Gen24Service2 is not null ? Gen24Service2.GetFroniusData(result.Gen24Config2.Components, token) : Task.FromResult<Gen24Sensors?>(null)!;
                    result.Gen24Sensors = await task1.ConfigureAwait(false);
                    result.Gen24Sensors2 = await task2.ConfigureAwait(false);
                    IsConnected = result.Gen24Config?.Components is not null;
                }
                catch
                {
                    IsConnected = false;
                    throw;
                }
            }
        }

        private async Task UpdateConfigData(HomeAutomationSystem result, CancellationToken token)
        {
            var task1 = GetConfigToken(Gen24Service, token);
            var task2 = GetConfigToken(Gen24Service2, token);

            result.Gen24Config = await task1.ConfigureAwait(false);
            result.Gen24Config2 = await task2.ConfigureAwait(false);

            lastConfigUpdate = DateTime.UtcNow;
        }

        private static async Task<Gen24Config?> GetConfigToken(IGen24Service? webClientService, CancellationToken token)
        {
            if (webClientService is null)
            {
                return null;
            }

            var versionsToken = (await webClientService.GetFroniusJsonResponse("api/status/version", token: token).ConfigureAwait(false)).Token;
            var componentsToken = (await webClientService.GetFroniusJsonResponse("api/components/", token: token).ConfigureAwait(false)).Token;
            var configToken = (await webClientService.GetFroniusJsonResponse("api/config/", token: token).ConfigureAwait(false)).Token;

#if DEBUG
            // ReSharper disable UnusedVariable
            var configString = configToken.ToString();
            var versionString = versionsToken.ToString();
            var componentsString = componentsToken.ToString();
            // ReSharper restore UnusedVariable
#endif

            return await Task.Run(() => Gen24Config.Parse(versionsToken, componentsToken, configToken), token).ConfigureAwait(false);
        }

        private async Task<FritzBoxDeviceList?> TryGetFritzBoxData(CancellationToken token)
        {
            try
            {
                if (FritzBoxService.Connection == null)
                {
                    return null;
                }

                var devices = await FritzBoxService.GetFritzBoxDevices(token).ConfigureAwait(false);

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
                }, token).ConfigureAwait(false);

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
            if (settings is { HaveToshibaAc: true, ShowToshibaAc: true } && !HvacService.IsRunning)
            {
                await HvacService.Start(settings.ToshibaAcConnection, settings.AzureDeviceIdString).ConfigureAwait(false);

                if (!HvacService.IsRunning)
                {
                    return;
                }

                SwitchableDevices.RemoveRange(SwitchableDevices.OfType<ToshibaHvacMappingDevice>().ToList());
                SwitchableDevices.AddRange(HvacService.AllDevices!.SelectMany(d => d.Devices));
            }
        }

        private async Task<Gen24Sensors?> TryGetGen24System(IGen24Service? gen24Service, Gen24Components? components, CancellationToken token)
        {
            if (gen24Service?.Connection is null || components is null)
            {
                return null;
            }

            try
            {
                return await gen24Service.GetFroniusData(components, token).ConfigureAwait(false);
            }
            catch
            {
                froniusCounter = 0;
                throw;
            }
        }

        [SuppressMessage("ReSharper", "AsyncVoidMethod", Justification = "The few lines outside of try-catch never throw")]
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

                using var tokenSource = new CancellationTokenSource(webRequestTimeOut);

                if (HomeAutomationSystem?.Gen24Config == null)
                {
                    HomeAutomationSystem = await CreateSolarSystem(null, null, null, tokenSource.Token).ConfigureAwait(false);
                    newSolarData = true;
                    newFritzBoxData = true;
                }
                else
                {
                    if ((DateTime.UtcNow - lastConfigUpdate).TotalMinutes > 5)
                    {
                        await UpdateConfigData(HomeAutomationSystem, tokenSource.Token).ConfigureAwait(false);
                    }

                    var gen24Task = froniusCounter % FroniusUpdateRate == 0 ? TryGetGen24System(Gen24Service, HomeAutomationSystem.Gen24Config!.Components, tokenSource.Token) : Task.FromResult<Gen24Sensors?>(null);
                    var gen24Task2 = froniusCounter++ % FroniusUpdateRate == 0 ? TryGetGen24System(Gen24Service2, HomeAutomationSystem.Gen24Config2?.Components, tokenSource.Token) : Task.FromResult<Gen24Sensors?>(null);

                    if (FritzBoxService.Connection == null && SwitchableDevices.Any(d => d is FritzBoxDevice))
                    {
                        SwitchableDevices.RemoveRange(SwitchableDevices.OfType<FritzBoxDevice>().ToList());
                    }

                    var fritzBoxTask = suspendFritzBoxCounter <= 0 && FritzBoxService.Connection != null &&
                                       fritzBoxCounter++ % FritzBoxUpdateRate == 0
                        ? TryGetFritzBoxData(tokenSource.Token)
                        : Task.FromResult<FritzBoxDeviceList?>(null);

                    var wattPilotTask = TryStartWattPilot();

                    var toshibaAcTask = TryStartToshibaAc();

                    try
                    {
                        await Task.WhenAll(gen24Task, gen24Task2, fritzBoxTask, wattPilotTask, toshibaAcTask).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException) { }

                    HomeAutomationSystem.WattPilot = wattPilotTask.IsCompletedSuccessfully ? wattPilotTask.Result ?? HomeAutomationSystem.WattPilot : HomeAutomationSystem.WattPilot;
                    HomeAutomationSystem.FritzBox = fritzBoxTask.IsCompletedSuccessfully ? fritzBoxTask.Result ?? HomeAutomationSystem.FritzBox : null;
                    HomeAutomationSystem.Gen24Sensors = gen24Task.IsCompletedSuccessfully ? gen24Task.Result ?? HomeAutomationSystem.Gen24Sensors : null;
                    HomeAutomationSystem.Gen24Sensors2 = gen24Task2.IsCompletedSuccessfully ? gen24Task2.Result ?? HomeAutomationSystem.Gen24Sensors2 : null;

                    var powerFlow = HomeAutomationSystem.Gen24Sensors?.PowerFlow;
                    var powerFlow2 = HomeAutomationSystem.Gen24Sensors2?.PowerFlow;

                    HomeAutomationSystem.SitePowerFlow = new Gen24PowerFlow
                    {
                        LoadPower = (powerFlow?.LoadPower ?? -powerFlow?.InverterAcPower ?? 0) + (powerFlow2?.LoadPower ?? -powerFlow2?.InverterAcPower ?? 0),
                        GridPower = (powerFlow?.GridPower ?? 0) + (powerFlow2?.GridPower ?? 0),
                        StoragePower = (powerFlow?.StoragePower ?? 0) + (powerFlow2?.StoragePower ?? 0),
                        SolarPower = (powerFlow?.SolarPower ?? 0) + (powerFlow2?.SolarPower ?? 0),
                        InverterAcPower = (powerFlow?.InverterAcPower ?? 0) + (powerFlow2?.InverterAcPower ?? 0),
                    };

                    newFritzBoxData = fritzBoxTask is { IsCompletedSuccessfully: true, Result: not null };
                    newSolarData = gen24Task is { IsCompletedSuccessfully: true, Result: not null } && gen24Task2 is { IsCompletedSuccessfully: true, Result: not null };
                }

                IsConnected = true;
                if (newFritzBoxData || newSolarData) await Task.Run(() => NewDataReceived?.Invoke(this, new SolarDataEventArgs(HomeAutomationSystem)), tokenSource.Token).ConfigureAwait(false);
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
//
