using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.ToshibaAc;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace De.Hochstaetter.HomeAutomationClient.Services;

internal partial class UpdateService(IWebClientService webClient, IVisibilityService visibility, ILogger<UpdateService> logger) : BindableBase, IUpdateService
{
    private HubConnection? hubConnection;

    /// <summary>Opens the hub connection while somebody can see a window and drops it while nobody can; see <see cref="ConnectionGate"/>.</summary>
    private ConnectionGate? gate;

    /// <summary>Whether the user who logged in sees more than the inverters, which is what decides what <see cref="CatchUpAsync"/> asks for.</summary>
    private bool seesAll;

    public event EventHandler<SitePowerFlowUpdatedEventArgs>? SitePowerFlowUpdated;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowInverters), nameof(DetailDevices), nameof(DevicesWithSettings))]
    public partial ObservableCollection<KeyedGen24System> Inverters { get; set; } = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowPowerConsumers), nameof(DetailDevices), nameof(DevicesWithSettings))]
    public partial ObservableCollection<IKeyedDevice> AllPowerConsumers { get; set; } = [];

    [ObservableProperty]
    public partial List<KeyedWattPilotUpdate> WattPilotUpdates { get; set; } = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(DetailDevices))]
    public partial Gen24PowerMeter3P? SmartMeter { get; set; }

    [ObservableProperty]
    public partial Gen24Status? MeterStatus { get; set; }

    [ObservableProperty]
    public partial Gen24Config? PrimaryGen24Config { get; set; }

    [ObservableProperty]
    public partial Gen24System? BatteryGen24System { get; set; }

    [ObservableProperty]
    public partial Gen24PowerFlow SitePowerFlow { get; set; } = new();

    [ObservableProperty]
    public partial double SitePvPeakPower { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasEnergyData))]
    public partial EnergyChartData? EnergyChartData { get; set; }

    public bool HasEnergyData => EnergyChartData != null;

    public event EventHandler<EnergyChartData>? EnergyChartDataChanged;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasSolarWeb))]
    public partial SolarWebFirmwareStatus? SolarWebFirmwareStatus { get; set; }

    public bool HasSolarWeb => SolarWebFirmwareStatus != null;

    public event EventHandler<SolarWebFirmwareStatus>? SolarWebFirmwareStatusChanged;

    public IEnumerable<IKeyedDevice> DetailDevices
    {
        get
        {
            var result = Inverters.Cast<IKeyedDevice>();

            result = Inverters.Where(i => i.Device.Sensors?.PrimaryPowerMeter != null)
                .Aggregate(result, (current, keyedInverter) => current.Append(new KeyedDevice<Gen24PowerMeter3P> { Device = keyedInverter.Device.Sensors!.PrimaryPowerMeter!, Key = "SmartMeter" }));

            result = Inverters.Where(i => i.Device.Sensors?.Storage != null)
                .Aggregate(result, (current, keyedInverter) => current.Append(new KeyedDevice<Gen24Storage> { Device = keyedInverter.Device.Sensors!.Storage!, Key = Loc.Battery }));

            result = result.Concat(AllPowerConsumers.Where(c => c is KeyedWattPilot));

            return result;
        }
    }

    public IEnumerable<IKeyedDevice> DevicesWithSettings => Inverters.Concat(AllPowerConsumers.Where(c => c is KeyedWattPilot));

    public bool ShowInverters => Inverters.Count > 0;

    public bool ShowPowerConsumers => AllPowerConsumers.Count > 0;

    public async Task StartAsync(Roles roles)
    {
        // A guest sees the inverters only. The server would answer every other request with 403 and push nothing
        // else over the hub either; not asking spares the round trips and the error handling.
        seesAll = roles.SeesAllDevices();

        if (seesAll)
        {
            var wattPilotResult = await webClient.GetWattPilots();

            if (wattPilotResult.Payload is { } wattPilots)
            {
                wattPilots.Select(wp => new KeyedWattPilot { Device = wp.Value, Key = wp.Key }).Apply(w => AllPowerConsumers.Add(w));
            }
        }

        var gen24Result = await webClient.GetGen24Devices();

        if (gen24Result.Payload is { } gen24Systems)
        {
            Inverters = [.. gen24Systems.Select(i => new KeyedGen24System { Device = i.Value, Key = i.Key }).OrderBy(i => i.Device.Config?.InverterSettings?.SystemName ?? Loc.Unknown)];
            Inverters.Apply(OnInverterUpdateReceived);
        }

        if (seesAll)
        {
            var fritzBoxResult = await webClient.GetFritzBoxDevices();

            if (fritzBoxResult.Payload is { } fritzBoxDevices)
            {
                fritzBoxDevices.Where(fb => fb.Value.CanSwitch).Select(fb => new KeyedFritzBoxDevice { Device = fb.Value, Key = fb.Key }).Apply(f => AllPowerConsumers.Add(f));
            }

            var toshibaResult = await webClient.GetToshibaHvacDevices();

            if (toshibaResult.Payload is { } toshibaDevices)
            {
                toshibaDevices.Select(t => new KeyedToshibaHvac { Device = t.Value, Key = t.Key }).Apply(t => AllPowerConsumers.Add(t));
            }

            NotifyOfPropertyChange(nameof(ShowPowerConsumers));

            // 404 where the server collects no energy data, which is not an error: the menu then has no chart.
            var energyResult = await webClient.GetEnergyData();

            if (energyResult is { Status: HttpStatusCode.OK, Payload: { } energyData })
            {
                EnergyChartData = energyData;
            }

            // Not awaited: the server may have to log in to Solar.web first, which can take a minute, and the
            // dashboard must not wait for that. The method reports its own failures, so nothing is lost.
            _ = FetchSolarWebFirmwareStatusAsync();
        }

        var hubUri =IoC.TryGetRegistered<ICache>()?.Get<string>(CacheKeys.HubUri) ?? "http://www.example.com/hub";

        hubConnection = new HubConnectionBuilder()
            // The ticket, not the password: on the WebSocket transport SignalR can only pass this in the query
            // string, where it would land in every access log on the way. AccessTokenProvider is asked again on
            // every reconnect, so a fresh ticket is fetched each time rather than kept around.
            .WithUrl(hubUri, options => options.AccessTokenProvider = () => webClient.GetHubTicket())
            .WithAutomaticReconnect()
            .AddJsonProtocol(o =>
            {
                o.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                o.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.PayloadSerializerOptions.IgnoreReadOnlyProperties = true;
                o.PayloadSerializerOptions.IgnoreReadOnlyFields = true;
                o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            })
            .Build();

        // The handlers run on the thread SignalR delivers on and write straight through from there. The binding
        // system marshals for us, a bound collection as much as a bound property, so none of them dispatches.
        // Registered before the connection is opened: the server greets a new connection with every device it has,
        // and a message for a method nobody has registered yet is dropped.
        hubConnection.On<string, Gen24System>(nameof(Gen24System), OnGen24Update);
        hubConnection.On<string, FritzBoxDevice>(nameof(FritzBoxDevice), OnFritzBoxUpdate);
        hubConnection.On<string, WattPilot>(nameof(WattPilot), OnWattPilotUpdate);
        hubConnection.On<string, WattPilotUpdate>(nameof(WattPilotUpdate), OnWattPilotUpdateMessage);
        hubConnection.On<string, ToshibaHvacMappingDevice>(nameof(ToshibaHvacMappingDevice), OnToshibaHvacUpdate);
        hubConnection.On<string, EnergyChartData>(nameof(EnergyChartData), OnEnergyChartData);
        hubConnection.On<string, SolarWebFirmwareStatus>(nameof(SolarWebFirmwareStatus), OnSolarWebFirmwareStatus);

        // An automatic reconnect has a gap before it like any other, and the devices below were updated in it.
        hubConnection.Reconnected += OnReconnected;
        hubConnection.Closed += OnClosed;

        // The gate opens the connection now - the first connect fails here, into the caller's error handling, the
        // way it always did - and from then on closes and reopens it as the windows go out of sight and come back.
        var connection = hubConnection;
        gate = new ConnectionGate(visibility, connection.StartAsync, () => connection.StopAsync(), CatchUpAsync, logger);
        await gate.StartAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches what a dropped connection did not deliver. Two kinds of device push their state in different ways,
    /// and only one of them needs this:
    /// <list type="bullet">
    /// <item>The Gen24 inverters and the Fritz!Box devices are pushed whole on every poll, and the server greets a
    /// new connection with the whole of every device it has. Reconnecting is enough for them.</item>
    /// <item>The Wattpilots, the Toshiba air conditioners and the price data are pushed when something changes -
    /// the Wattpilot even as a delta of the properties that did. A change that fell into the gap is gone from the
    /// hub, so they are fetched over HTTP and put in place through the same handlers a push goes through.</item>
    /// </list>
    /// A guest is not asked for any of it: the server would answer 403, and a guest sees the inverters only.
    /// </summary>
    private async Task CatchUpAsync()
    {
        if (!seesAll)
        {
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Fetching the devices whose changes the hub does not repeat.");
        }

        var wattPilots = await webClient.GetWattPilots().ConfigureAwait(false);

        if (wattPilots is { Status: HttpStatusCode.OK, Payload: { } pilots })
        {
            pilots.Apply(pair => OnWattPilotUpdate(pair.Key, pair.Value));
        }

        var toshibaDevices = await webClient.GetToshibaHvacDevices().ConfigureAwait(false);

        if (toshibaDevices is { Status: HttpStatusCode.OK, Payload: { } hvacDevices })
        {
            hvacDevices.Apply(pair => OnToshibaHvacUpdate(pair.Key, pair.Value));
        }

        // 404 where the server collects no energy data, which is not an error; the chart then stays as it was.
        var energyData = await webClient.GetEnergyData().ConfigureAwait(false);

        if (energyData is { Status: HttpStatusCode.OK, Payload: { } data })
        {
            OnEnergyChartData(EnergyChartData.DeviceId, data);
        }

        // The firmware status is pushed only when it changes, so a reconnect reads it once.
        await FetchSolarWebFirmwareStatusAsync().ConfigureAwait(false);
    }

    private async Task OnReconnected(string? connectionId)
    {
        try
        {
            await CatchUpAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Catching up after the automatic reconnect failed.");
        }
    }

    /// <summary>
    /// A connection the automatic reconnect has given up on - the server was gone for longer than its retries
    /// last - would stay closed for good otherwise. The gate is told, and opens it again with its own patience as
    /// long as somebody looks. A close without an error is one the gate asked for itself.
    /// </summary>
    private Task OnClosed(Exception? error)
    {
        if (error != null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(error, "The hub connection was lost.");
            }

            gate?.NotifyDisconnected();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The counterpart of <see cref="StartAsync"/>: closes the hub connection first, so nothing it might still
    /// deliver races the clearing below, and then forgets every device. A second <see cref="StartAsync"/> - after
    /// logging back in, possibly as somebody else - therefore starts from nothing rather than from what the last
    /// session left behind.
    /// </summary>
    public async Task StopAsync()
    {
        if (gate != null)
        {
            await gate.StopAsync().ConfigureAwait(false);
            gate = null;
        }

        if (hubConnection != null)
        {
            hubConnection.Reconnected -= OnReconnected;
            hubConnection.Closed -= OnClosed;
            await hubConnection.DisposeAsync().ConfigureAwait(false);
            hubConnection = null;
        }

        Inverters = [];
        AllPowerConsumers = [];
        WattPilotUpdates = [];
        SmartMeter = null;
        MeterStatus = null;
        PrimaryGen24Config = null;
        BatteryGen24System = null;
        SitePowerFlow = new();
        SitePvPeakPower = 0;
        EnergyChartData = null;
        SolarWebFirmwareStatus = null;
    }

    /// <summary>
    /// The server sends the whole chart data on every change - a few kilobytes a few times a day. Replaced as one
    /// object rather than copied into place: nothing binds to the parts, the chart is rebuilt from the whole.
    /// </summary>
    private void OnEnergyChartData(string id, EnergyChartData data)
    {
        try
        {
            EnergyChartData = data;
            EnergyChartDataChanged?.Invoke(this, data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Updating the energy data failed.");
        }
    }

    /// <summary>
    /// Reads the firmware status over HTTP. 404 where the server has no Solar.web account, which is not an error: the
    /// menu then has no Solar.web chart. Any other failure is logged and nothing else: the next push or catch-up
    /// brings the status, and a Solar.web that is down must not take the client down with it.
    /// </summary>
    private async Task FetchSolarWebFirmwareStatusAsync()
    {
        try
        {
            var result = await webClient.GetSolarWebFirmwareStatus().ConfigureAwait(false);

            if (result is { Status: HttpStatusCode.OK, Payload: { } status })
            {
                OnSolarWebFirmwareStatus(SolarWebFirmwareStatus.DeviceId, status);
            }
            else if (result.Status != HttpStatusCode.NotFound)
            {
                logger.LogWarning("The Solar.web firmware status could not be read: {Status} {Detail}", result.Status, result.Detail);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reading the Solar.web firmware status failed.");
        }
    }

    /// <summary>The server sends the whole status whenever a component's firmware changes. Replaced as one object; the notice is worked out from the whole.</summary>
    private void OnSolarWebFirmwareStatus(string id, SolarWebFirmwareStatus status)
    {
        try
        {
            SolarWebFirmwareStatus = status;
            SolarWebFirmwareStatusChanged?.Invoke(this, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Updating the Solar.web firmware status failed.");
        }
    }

    public Task<WattPilotWriteResult> SetWattPilotSettings(string deviceId, WattPilot wanted, WattPilot loaded) =>
        Hub.InvokeAsync<WattPilotWriteResult>(nameof(SetWattPilotSettings), deviceId, wanted, loaded);

    public Task RebootWattPilot(string deviceId) => Hub.InvokeAsync(nameof(RebootWattPilot), deviceId);

    public Task<ToshibaHvacCommandResult> SendToshibaHvacCommand(string[] ids, ToshibaHvacStateData state) =>
        Hub.InvokeAsync<ToshibaHvacCommandResult>(nameof(SendToshibaHvacCommand), ids, state);

    /// <summary>
    /// The connection a client-to-server call goes over. It is only ever null before <see cref="StartAsync"/>, and
    /// the settings dialogs that call this are opened from a device list that connection delivered.
    /// </summary>
    private HubConnection Hub => hubConnection ?? throw new InvalidOperationException(Loc.NoSystemConnection);

    public async ValueTask DisposeAsync()
    {
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        _ = Task.Run(async () =>
        {
            if (hubConnection != null)
            {
                await hubConnection.DisposeAsync();
            }
        });

        GC.SuppressFinalize(this);
    }

    ~UpdateService() => Dispose();

    private async Task OnWattPilotUpdateMessage(string id, WattPilotUpdate update)
    {
        try
        {
            var keyedQueue = WattPilotUpdates.FirstOrDefault(q => q.Key == id);

            if (keyedQueue == null)
            {
                keyedQueue = new KeyedWattPilotUpdate { Key = id, Device = new ConcurrentQueue<WattPilotUpdate>() };
                WattPilotUpdates.Add(keyedQueue);
            }

            keyedQueue.Device.Enqueue(update);

            var existingDevice = AllPowerConsumers.OfType<KeyedWattPilot>().FirstOrDefault(i => i.Device.SerialNumber == update.SerialNumber);

            if (existingDevice == null)
            {
                var pilots = await webClient.GetWattPilots().ConfigureAwait(false);

                if (pilots is { Status: HttpStatusCode.OK, Payload: { } wattPilots })
                {
                    var currentWattPilots = AllPowerConsumers.OfType<KeyedWattPilot>().ToArray();
                    currentWattPilots.Apply(w => AllPowerConsumers.Remove(w));
                    wattPilots.Select(wp => new KeyedWattPilot { Device = wp.Value, Key = wp.Key }).Apply(w => AllPowerConsumers.Add(w));
                }

                NotifyOfPropertyChange(nameof(ShowPowerConsumers));
                return;
            }

            while (!keyedQueue.Device.IsEmpty)
            {
                if (keyedQueue.Device.TryDequeue(out var result))
                {
                    existingDevice.Device.UpdateFromJson(result.JsonMessage);
                }
                else
                {
                    throw new InvalidOperationException("Cannot read update queue");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Updating Wattpilot failed.");
        }
    }

    private void OnWattPilotUpdate(string id, WattPilot wattPilot)
    {
        try
        {
            var existingDevice = AllPowerConsumers.OfType<KeyedWattPilot>().FirstOrDefault(i => i.Key == id);

            if (existingDevice == null)
            {
                AllPowerConsumers.Add(new KeyedWattPilot { Device = wattPilot, Key = id });
                NotifyOfPropertyChange(nameof(ShowPowerConsumers));
            }
            else
            {
                existingDevice.Device.CopyFrom(wattPilot);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private void OnGen24Update(string id, Gen24System gen24System)
    {
        try
        {
            gen24System.Sensors?.GeneratePowerFlow();

            var inverter = Inverters.FirstOrDefault(i => i.Key == id);

            if (inverter == null)
            {
                inverter = new KeyedGen24System { Key = id, Device = gen24System };

                Inverters = [.. Inverters.Append(inverter).OrderBy(i => i.Device.Config?.InverterSettings?.SystemName)];
                NotifyOfPropertyChange(nameof(ShowInverters));
            }
            else
            {
                inverter.Device.CopyFrom(gen24System);
            }

            OnInverterUpdateReceived(inverter);
        }
        catch
        {
            // Ignore errors
        }
    }

    private void OnInverterUpdateReceived(KeyedDevice<Gen24System> inverter)
    {
        if (inverter.Device.Sensors is { PrimaryPowerMeter: not null })
        {
            PrimaryGen24Config = inverter.Device.Config;
            MeterStatus = inverter.Device.Sensors.MeterStatus;
            SmartMeter = inverter.Device.Sensors.PrimaryPowerMeter;
        }

        if (inverter.Device.Sensors is { Storage: not null })
        {
            BatteryGen24System = inverter.Device;
        }

        try
        {
            SitePowerFlow.IsNotifying = false;
            SitePowerFlow.SolarPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.SolarPower ?? 0);
            SitePowerFlow.GridPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.GridPower ?? 0);
            SitePowerFlow.StoragePower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.StoragePower ?? 0);
            SitePowerFlow.LoadPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.LoadPower ?? 0);
            SitePowerFlow.InverterAcPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.InverterAcPower ?? 0);
            SitePvPeakPower = Inverters.Sum(i => i.Device.Config?.InverterSettings?.Mppt?.Mppt1?.WattPeak + i.Device.Config?.InverterSettings?.Mppt?.Mppt2?.WattPeak ?? 0);
        }
        finally
        {
            SitePowerFlow.Refresh(true);
        }

        SitePowerFlowUpdated?.Invoke(this, new SitePowerFlowUpdatedEventArgs(inverter, SitePowerFlow));
    }

    /// <summary>
    /// The server sends the whole device on every change (there is no delta message as for the Wattpilot; the
    /// device is small). An instance the dashboard already shows takes the values in place, so the control bound
    /// to it and the view model subscribed to its state keep their object.
    /// </summary>
    private void OnToshibaHvacUpdate(string id, ToshibaHvacMappingDevice device)
    {
        try
        {
            var existing = AllPowerConsumers.OfType<KeyedToshibaHvac>().FirstOrDefault(d => d.Key == id);

            if (existing == null)
            {
                AllPowerConsumers.Add(new KeyedToshibaHvac { Key = id, Device = device });
                NotifyOfPropertyChange(nameof(ShowPowerConsumers));
            }
            else
            {
                existing.Device.CopyFrom(device);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Updating Toshiba HVAC {Id} failed.", id);
        }
    }

    private void OnFritzBoxUpdate(string id, FritzBoxDevice fritzBoxDevice)
    {
        if (!fritzBoxDevice.CanSwitch)
        {
            return;
        }

        var updateDevice = AllPowerConsumers.OfType<KeyedFritzBoxDevice>().FirstOrDefault(f => f.Key == id);

        if (updateDevice == null)
        {
            AllPowerConsumers.Add(new KeyedFritzBoxDevice { Key = id, Device = fritzBoxDevice });
            NotifyOfPropertyChange(nameof(ShowPowerConsumers));
        }
        else
        {
            updateDevice.Device.CopyFrom(fritzBoxDevice);
        }
    }
}
