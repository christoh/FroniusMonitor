using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using De.Hochstaetter.Fronius.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Timer = System.Threading.Timer;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class UiDemoViewModel(IWebClientService webClient) : ViewModelBase, IAsyncDisposable, IDisposable
{
    public class KeyedInverter
    {
        public required string Key { get; init; }

        public required Gen24System Inverter { get; init; }
    }

    public class KeyedFritzBoxDevice
    {
        public required string Key { get; init; }
        public required FritzBoxDevice Device { get; init; }
    }

    private Timer? timer;
    private HubConnection? connection;

    [ObservableProperty]
    public partial bool ColorAllTicks { get; set; } = true;

    [ObservableProperty]
    public partial ObservableCollection<KeyedInverter> Inverters { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<KeyedFritzBoxDevice> FritzBoxDevices { get; set; } = [];

    [ObservableProperty]
    public partial Gen24PowerMeter3P? SmartMeter { get; set; }

    [ObservableProperty]
    public partial Gen24Status? MeterStatus { get; set; }

    [ObservableProperty]
    public partial Gen24Config? Gen24Config { get; set; }

    [ObservableProperty]
    public partial Gen24System? BatteryGen24System { get; set; }

    public bool ShowInverters => Inverters.Count > 0;

    public bool ShowPowerConsumers => FritzBoxDevices.Count > 0;

    public override async Task Initialize()
    {
        BusyText = Loc.ConnectingToHas;
        await base.Initialize();
        timer = new(TimerElapsed, null, 0, 1000);

        var hubUri = IoC.TryGetRegistered<ICache>()?.Get<string>(CacheKeys.HubUri) ?? "http://www.example.com/hub";

        connection = new HubConnectionBuilder()
            .WithUrl(new Uri("http://localhost:4711/hub"))
            .WithAutomaticReconnect()
            .AddJsonProtocol(o =>
            {
                o.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                o.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.PayloadSerializerOptions.IgnoreReadOnlyProperties = true;
                o.PayloadSerializerOptions.IgnoreReadOnlyFields = true;
            })
            .Build();

        await connection.StartAsync();

        connection.On<string, Gen24System>(nameof(Gen24System), OnGen24Update);

        connection.On<string, FritzBoxDevice>(nameof(FritzBoxDevice), OnFritzBoxUpdate);
    }

    public async ValueTask DisposeAsync()
    {
        if (timer != null)
        {
            await timer.DisposeAsync();
        }

        if (connection != null)
        {
            await connection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        timer?.Dispose();

        _ = Task.Run(async () =>
        {
            if (connection != null)
            {
                await connection.DisposeAsync();
            }
        });

        GC.SuppressFinalize(this);
    }

    ~UiDemoViewModel() => Dispose();


    private void OnFritzBoxUpdate(string id, FritzBoxDevice fritzBoxDevice)
    {

    }

    private void OnGen24Update(string id, Gen24System gen24System)
    {

    }

    private async void TimerElapsed(object? state)
    {
        try
        {
            var invertersTask = webClient.GetGen24Devices();
            var fritzBoxTask = webClient.GetFritzBoxDevices();

            var inverters = (await invertersTask.ConfigureAwait(false)).Payload;

            if (inverters != null)
            {
                foreach (var inverter in Inverters)
                {
                    if (inverter.Inverter.Sensors is { Storage: not null, PrimaryPowerMeter: not null })
                    {
                        Gen24Config = inverter.Inverter.Config;
                        MeterStatus = inverter.Inverter.Sensors.MeterStatus;
                        SmartMeter = inverter.Inverter.Sensors.PrimaryPowerMeter;
                        BatteryGen24System = inverter.Inverter;
                    }

                    var updatedInverter = inverters.FirstOrDefault(i => i.Key == inverter.Key);

                    if (updatedInverter.Key != null)
                    {
                        inverter.Inverter.CopyFrom(updatedInverter.Value);
                        inverters.Remove(updatedInverter);
                    }
                }

                foreach (var inverter in inverters)
                {
                    _ = Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Inverters.Add(new KeyedInverter { Key = inverter.Key, Inverter = inverter.Value });
                        NotifyOfPropertyChange(nameof(ShowInverters));
                    });
                }
            }

            var fritzBoxDevices = (await fritzBoxTask.ConfigureAwait(false)).Payload;

            if (fritzBoxDevices != null)
            {
                foreach (var fritzBoxDevice in FritzBoxDevices)
                {
                    var updatedFritzBoxDevice = fritzBoxDevices.FirstOrDefault(i => i.Key == fritzBoxDevice.Key);

                    if (updatedFritzBoxDevice.Key != null)
                    {
                        fritzBoxDevice.Device.CopyFrom(updatedFritzBoxDevice.Value);
                        fritzBoxDevices.Remove(updatedFritzBoxDevice);
                    }
                }

                foreach (var fritzBoxDevice in fritzBoxDevices.Where(f => f.Value.CanSwitch))
                {
                    FritzBoxDevices.Add(new KeyedFritzBoxDevice { Key = fritzBoxDevice.Key, Device = fritzBoxDevice.Value });
                    NotifyOfPropertyChange(nameof(ShowPowerConsumers));
                }
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            BusyText = null;
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
