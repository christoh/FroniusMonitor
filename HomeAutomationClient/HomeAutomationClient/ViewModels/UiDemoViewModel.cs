using System.Collections.ObjectModel;
using System.Diagnostics;
using De.Hochstaetter.Fronius.Models;
using Timer = System.Threading.Timer;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class UiDemoViewModel(IWebClientService webClient) : ViewModelBase, IDisposable
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

    public string Culture => Thread.CurrentThread.CurrentCulture.Name;

    public string ApiUri => IoC.GetRegistered<MainViewModel>().ApiUri;

    public string UiCulture => Thread.CurrentThread.CurrentUICulture.Name;

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
        BusyText = Resources.ConnectingToHas;
        await base.Initialize();
        timer = new(TimerElapsed, null, 0, 1000);
    }

    public void Dispose()
    {
        timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    ~UiDemoViewModel()
    {
        Dispose();
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
    private async Task SwitchDevice(string key)
    {
        try
        {
            BusyText = "Switching device...";
            var keyedDevice = FritzBoxDevices.First(d => d.Key == key);
            ISwitchable device = keyedDevice.Device;
            var isTurnedOn = device.IsTurnedOn;
            var result = await webClient.SwitchDevice(key, device.IsTurnedOn is not true);

            if ((result.Status is not null and not HttpStatusCode.OK)||result.Exception!=null)
            {
                if (device is FritzBoxDevice { SimpleSwitch:not null} fritzBoxDevice)
                {
                    fritzBoxDevice.SimpleSwitch.IsTurnedOn = isTurnedOn;
                    var index= FritzBoxDevices.IndexOf(keyedDevice);
                    FritzBoxDevices.Remove(keyedDevice);
                    FritzBoxDevices.Insert(index,keyedDevice);
                }

                if (result.Status is HttpStatusCode.Forbidden)
                {
                    await new MessageBox { Title = $"{Resources.HttpError} {(int)result.Status} ({result.Status})", Text = Resources.HttpForbidden, Buttons = [Resources.Ok], Icon = new ErrorIcon() }.Show();
                }
                else
                {
                    await new MessageBox
                    {
                        Title = result.Title ?? $"{Resources.HttpError} {(int)result.Status} ({result.Status})",
                        Text = result.Errors?.Count > 1?result.Detail:null,
                        ItemList = result.Errors?.Count < 1 ? null : result.Errors?.SelectMany(e => e.Value).ToList(),
                        Icon = new ErrorIcon(),
                        Buttons = [Resources.Ok],
                    }.Show();
                }
            }
        }
        catch (Exception ex)
        {
            BusyText = null;
            await ex.Show().ConfigureAwait(false);
        }
        finally
        {
            BusyText = null;
        }
    }

    [RelayCommand]
    private async Task Standby(Gen24System gen24System)
    {
        await new NotImplementedException("Coming soon").Show().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task ShowComplexDialog()
    {
        try
        {
            await new MessageBox
            {
                Title = "Test Dialog",
                Buttons = [Resources.Ok, Resources.Cancel],
                Text = "Hello, World! This is a test for a MessageBox that has some longer text items and everything still needs to look good and the text must properly wrap. Please also have a look at the following items.",
                ItemList =
                [
                    "Always prefer composition over inheritance",
                    "If you create an async method, you must ensure that you properly await it. Carefully choose between Task<T> and ValueTask<T> and don't forget to set ConfigureAwait() properly.",
                    "This is an additional bullet item.",
                ],
                TextBelowItemList = $"Did you understand everything? If not, press '{Resources.Cancel}'.",
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
                Title = Resources.Error,
                Text = string.Format(Resources.InverterCommReadError, "Atomkraftwerk 1"),
                Icon = new ErrorIcon(),
            }.Show();
        }
        catch
        {
            // async void must be caught to avoid unhandled exceptions
        }
    }
}
