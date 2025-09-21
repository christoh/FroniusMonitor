using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class UiDemoViewModel(IWebClientService webClient) : ViewModelBase, IAsyncDisposable, IDisposable
{
    public class KeyedDevice<T>
    {
        public required string Key { get; init; }
        public required T Device { get; init; }
    }


    private HubConnection? hubConnection;

    [ObservableProperty]
    public partial bool ColorAllTicks { get; set; } = true;

    [ObservableProperty]
    public partial ObservableCollection<KeyedDevice<Gen24System>> Inverters { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<KeyedDevice<FritzBoxDevice>> FritzBoxDevices { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<KeyedDevice<WattPilot>> WattPilots { get; set; } = [];

    [ObservableProperty]
    public partial Gen24PowerMeter3P? SmartMeter { get; set; }

    [ObservableProperty]
    public partial Gen24Status? MeterStatus { get; set; }

    [ObservableProperty]
    public partial Gen24Config? Gen24Config { get; set; }

    [ObservableProperty]
    public partial Gen24System? BatteryGen24System { get; set; }

    [ObservableProperty]
    public partial Gen24PowerFlow SitePowerFlow { get; set; } = new();

    [ObservableProperty]
    public partial double SitePvPeakPower { get; set; }

    public bool ShowInverters => Inverters.Count > 0;

    public bool ShowPowerConsumers => FritzBoxDevices.Count > 0;

    public override async Task Initialize()
    {
        try
        {
            BusyText = Loc.ConnectingToHas;
            await base.Initialize();

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

            await hubConnection.StartAsync();
            hubConnection.On<string, Gen24System>(nameof(Gen24System), OnGen24Update);
            hubConnection.On<string, FritzBoxDevice>(nameof(FritzBoxDevice), OnFritzBoxUpdate);
            hubConnection.On<string, WattPilot>(nameof(WattPilot), OnWattPilotUpdate);
            hubConnection.On<string, WattPilotUpdate>(nameof(WattPilotUpdate), OnWattPilotUpdateMessage);
        }
        finally
        {
            BusyText = null;
        }
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

    ~UiDemoViewModel() => Dispose();

    private void OnWattPilotUpdateMessage(string id, WattPilotUpdate update)
    {
        
    }

    private void OnWattPilotUpdate(string id, WattPilot wattPilot)
    {
        try
        {
            var existingDevice = WattPilots.FirstOrDefault(i => i.Key == id);

            if (existingDevice == null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    WattPilots.Add(new KeyedDevice<WattPilot> { Device = wattPilot, Key = id });
                });
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
                inverter = new KeyedDevice<Gen24System> { Key = id, Device = gen24System };

                Dispatcher.UIThread.Invoke(() =>
                {
                    Inverters = new(Inverters.Append(inverter).OrderBy(i => i.Device.Config?.InverterSettings?.SystemName));
                    NotifyOfPropertyChange(nameof(ShowInverters));
                });
            }
            else
            {
                inverter.Device.CopyFrom(gen24System);
            }

            if (inverter.Device.Sensors is { Storage: not null, PrimaryPowerMeter: not null })
            {
                Gen24Config = inverter.Device.Config;
                MeterStatus = inverter.Device.Sensors.MeterStatus;
                SmartMeter = inverter.Device.Sensors.PrimaryPowerMeter;
                BatteryGen24System = inverter.Device;
            }

            SitePowerFlow.SolarPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.SolarPower ?? 0);
            SitePowerFlow.GridPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.GridPower ?? 0);
            SitePowerFlow.StoragePower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.StoragePower ?? 0);
            SitePowerFlow.LoadPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.LoadPower ?? 0);
            SitePowerFlow.InverterAcPower = Inverters.Sum(i => i.Device.Sensors?.PowerFlow?.InverterAcPower ?? 0);
            SitePvPeakPower = Inverters.Sum(i => (i.Device.Config?.InverterSettings?.Mppt?.Mppt1?.WattPeak + i.Device.Config?.InverterSettings?.Mppt?.Mppt2?.WattPeak) ?? 0);
        }
        catch
        {
            // Ignore errors
        }
    }

    private void OnFritzBoxUpdate(string id, FritzBoxDevice fritzBoxDevice)
    {
        if (!fritzBoxDevice.CanSwitch)
        {
            return;
        }

        var updateDevice = FritzBoxDevices.FirstOrDefault(f => f.Key == id);

        if (updateDevice == null)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                FritzBoxDevices = new(FritzBoxDevices.Append(new KeyedDevice<FritzBoxDevice> { Key = id, Device = fritzBoxDevice }).OrderBy(d => d.Device.DisplayName));
                NotifyOfPropertyChange(nameof(ShowPowerConsumers));
            });
        }
        else
        {
            updateDevice.Device.CopyFrom(fritzBoxDevice);
        }
    }

    [RelayCommand]
    private Task SetBrightness(ValueChangeCommandParameter<double> change) => TaskExceptionHandler(async () =>
    {
        if (change.Key is not string key || Math.Abs(change.NewValue - change.OldValue) < .000001)
        {
            return;
        }

        BusyText = string.Empty;
        var keyedDevice = FritzBoxDevices.First(d => d.Key == key);
        var result = await webClient.SetDeviceBrightness(key, change.NewValue);

        if (result.Status is not HttpStatusCode.OK)
        {
            if (keyedDevice.Device.LevelControl != null)
            {
                keyedDevice.Device.LevelControl.Level = change.NewValue;
                keyedDevice.Device.Refresh();
                keyedDevice.Device.LevelControl.Level = change.OldValue;
                keyedDevice.Device.Refresh();
            }

            await ShowHttpError(result);
        }
    });

    [RelayCommand]
    private Task SwitchDevice(string key) => TaskExceptionHandler(async () =>
    {
        BusyText = string.Empty;
        var keyedDevice = FritzBoxDevices.First(d => d.Key == key);
        ISwitchable device = keyedDevice.Device;
        var isTurnedOn = device.IsTurnedOn;
        var result = await webClient.SwitchDevice(key, device.IsTurnedOn is not true);

        if (result.Status is not HttpStatusCode.OK || result.Exception != null)
        {
            if (keyedDevice.Device.SimpleSwitch != null)
            {
                keyedDevice.Device.SimpleSwitch.IsTurnedOn = !isTurnedOn;
                keyedDevice.Device.Refresh();
                keyedDevice.Device.SimpleSwitch.IsTurnedOn = isTurnedOn;
                keyedDevice.Device.Refresh();
            }

            await ShowHttpError(result);
        }
    });

    [RelayCommand]
    private Task SetColorTemperature(ValueChangeCommandParameter<double> change) => TaskExceptionHandler(async () =>
    {
        if (change.Key is not string key || Math.Abs(change.NewValue - change.OldValue) < .001)
        {
            return;
        }

        BusyText = string.Empty;
        var keyedDevice = FritzBoxDevices.First(d => d.Key == key);
        var result = await webClient.SetColorTemperature(key, change.NewValue);

        if (result.Status is not HttpStatusCode.OK)
        {
            if (keyedDevice.Device.Color != null)
            {
                keyedDevice.Device.Color.TemperatureKelvin = change.NewValue;
                keyedDevice.Device.Refresh();
                keyedDevice.Device.Color.TemperatureKelvin = change.OldValue;
                keyedDevice.Device.Refresh();
            }

            await ShowHttpError(result);
        }
    });

    [RelayCommand]
    private Task SetHue(ValueChangeCommandParameter<double> change) => TaskExceptionHandler(async () =>
    {
        if (change.Key is not string key || Math.Abs(change.NewValue - change.OldValue) < .001)
        {
            return;
        }

        BusyText = string.Empty;
        var keyedDevice = FritzBoxDevices.First(d => d.Key == key);
        var result = await webClient.SetHsv(key, hueDegrees: change.NewValue);

        if (result.Status is not HttpStatusCode.OK)
        {
            if (keyedDevice.Device.Color != null)
            {
                keyedDevice.Device.Color.HueDegrees = change.NewValue;
                keyedDevice.Device.Refresh();
                keyedDevice.Device.Color.HueDegrees = change.OldValue;
                keyedDevice.Device.Refresh();
            }

            await ShowHttpError(result);
        }
    });

    [RelayCommand]
    private Task SetSaturation(ValueChangeCommandParameter<double> change) => TaskExceptionHandler(async () =>
    {
        if (change.Key is not string key || Math.Abs(change.NewValue - change.OldValue) < .001)
        {
            return;
        }

        BusyText = string.Empty;
        var keyedDevice = FritzBoxDevices.First(d => d.Key == key);
        var result = await webClient.SetHsv(key, saturation: change.NewValue);

        if (result.Status is not HttpStatusCode.OK)
        {
            if (keyedDevice.Device.Color != null)
            {
                keyedDevice.Device.Color.SaturationAbsolute = Math.Round(change.NewValue * 255, MidpointRounding.AwayFromZero);
                keyedDevice.Device.Refresh();
                keyedDevice.Device.Color.SaturationAbsolute = Math.Round(change.OldValue * 255, MidpointRounding.AwayFromZero);
                keyedDevice.Device.Refresh();
            }

            await ShowHttpError(result);
        }
    });

    //[RelayCommand]
    //private Task Standby(string key) => TaskExceptionHandler(async () =>
    //{
    //    var inverter = Inverters.First(i => i.Key == key).Inverter;

    //    if (inverter.Sensors?.StandByStatus is not null)
    //    {
    //        var oldStatus=inverter.Sensors.StandByStatus;
    //        var result = await webClient.RequestGen24StandBy(key, !inverter.Sensors.StandByStatus.IsStandBy);

    //        if (result.Status is not HttpStatusCode.OK)
    //        {
    //            inverter.Sensors.StandByStatus.IsStandBy = oldStatus.IsStandBy;
    //            await ShowHttpError(result);
    //            return;
    //        }

    //        inverter.Sensors.StandByStatus.IsStandBy = !oldStatus.IsStandBy;
    //    }
    //});

    [RelayCommand]
    private async Task ShowComplexDialog()
    {
        try
        {
            await new MessageBox
            {
                Title = "Test Dialog",
                Buttons = [Loc.Ok, Loc.Cancel],
                Text = "Hello, World! This is a test for a MessageBox that has some longer text items and everything still needs to look good and the text must properly wrap. Please also have a look at the following items.",
                ItemList =
                [
                    "Always prefer composition over inheritance",
                    "If you create an async method, you must ensure that you properly await it. Carefully choose between Task<T> and ValueTask<T> and don't forget to set ConfigureAwait() properly.",
                    "This is an additional bullet item.",
                ],
                TextBelowItemList = $"Did you understand everything? If not, press '{Loc.Cancel}'.",
                Icon = new WarningIcon(),
            }.Show();
        }
        catch
        {
            // ignore
        }
    }

    [RelayCommand]
    private async Task ShowSimpleDialog()
    {
        try
        {
            await new MessageBox
            {
                Title = Loc.Error,
                Text = string.Format(Loc.InverterCommReadError, "Atomkraftwerk 1"),
                Icon = new ErrorIcon(),
            }.Show();
        }
        catch
        {
            // async void must be caught to avoid unhandled exceptions
        }
    }
}
