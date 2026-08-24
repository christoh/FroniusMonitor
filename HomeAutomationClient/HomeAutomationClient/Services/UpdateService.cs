using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace De.Hochstaetter.HomeAutomationClient.Services;

internal partial class UpdateService(IWebClientService webClient) : BindableBase, IUpdateService
{
    private HubConnection? hubConnection;

    public event EventHandler<SitePowerFlowUpdatedEventArgs>? SitePowerFlowUpdated;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowInverters), nameof(DetailDevices))]
    public partial ObservableCollection<KeyedGen24System> Inverters { get; set; } = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowPowerConsumers), nameof(DetailDevices))]
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

    public bool ShowInverters => Inverters.Count > 0;

    public bool ShowPowerConsumers => AllPowerConsumers.Count > 0;

    public async Task StartAsync()
    {
        var wattPilotResult = await webClient.GetWattPilots();

        if (wattPilotResult.Payload is { } wattPilots)
        {
            wattPilots.Select(wp => new KeyedWattPilot { Device = wp.Value, Key = wp.Key }).Apply(w => AllPowerConsumers.Add(w));
        }

        var gen24Result = await webClient.GetGen24Devices();

        if (gen24Result.Payload is { } gen24Systems)
        {
            Inverters = [.. gen24Systems.Select(i => new KeyedGen24System { Device = i.Value, Key = i.Key }).OrderBy(i => i.Device.Config?.InverterSettings?.SystemName ?? Loc.Unknown)];
            Inverters.Apply(OnInverterUpdateReceived);
        }

        var fritzBoxResult = await webClient.GetFritzBoxDevices();

        if (fritzBoxResult.Payload is { } fritzBoxDevices)
        {
            fritzBoxDevices.Where(fb => fb.Value.CanSwitch).Select(fb => new KeyedFritzBoxDevice { Device = fb.Value, Key = fb.Key }).Apply(f => AllPowerConsumers.Add(f));
        }

        NotifyOfPropertyChange(nameof(ShowPowerConsumers));

        var hubUri = IoC.TryGetRegistered<ICache>()?.Get<string>(CacheKeys.HubUri) ?? "http://www.example.com/hub";

        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUri)
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

        await hubConnection.StartAsync().ConfigureAwait(false);
        hubConnection.On<string, Gen24System>(nameof(Gen24System), OnGen24Update);
        hubConnection.On<string, FritzBoxDevice>(nameof(FritzBoxDevice), OnFritzBoxUpdate);
        hubConnection.On<string, WattPilot>(nameof(WattPilot), OnWattPilotUpdate);
        hubConnection.On<string, WattPilotUpdate>(nameof(WattPilotUpdate), OnWattPilotUpdateMessage);
    }

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

    private async void OnWattPilotUpdateMessage(string id, WattPilotUpdate update)
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
                    var currentWattPilots = AllPowerConsumers.OfType<KeyedFritzBoxDevice>().ToArray();
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
        catch
        {
            // Ignore errors
        }
    }

    private void OnWattPilotUpdate(string id, WattPilot wattPilot)
    {
        try
        {
            var existingDevice = AllPowerConsumers.OfType<KeyedWattPilot>().FirstOrDefault(i => i.Key == id);

            if (existingDevice == null)
            {
                Dispatcher.UIThread.Invoke(() => { AllPowerConsumers.Add(new KeyedWattPilot { Device = wattPilot, Key = id }); });
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

                Dispatcher.UIThread.Invoke(() =>
                {
                    Inverters = [.. Inverters.Append(inverter).OrderBy(i => i.Device.Config?.InverterSettings?.SystemName)];
                    NotifyOfPropertyChange(nameof(ShowInverters));
                });
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

    private void OnFritzBoxUpdate(string id, FritzBoxDevice fritzBoxDevice)
    {
        if (!fritzBoxDevice.CanSwitch)
        {
            return;
        }

        var updateDevice = AllPowerConsumers.OfType<KeyedFritzBoxDevice>().FirstOrDefault(f => f.Key == id);

        if (updateDevice == null)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                AllPowerConsumers.Add(new KeyedFritzBoxDevice { Key = id, Device = fritzBoxDevice });
                NotifyOfPropertyChange(nameof(ShowPowerConsumers));
            });
        }
        else
        {
            updateDevice.Device.CopyFrom(fritzBoxDevice);
        }
    }
}
