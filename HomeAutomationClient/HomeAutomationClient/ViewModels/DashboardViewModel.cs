using Avalonia.Media.Immutable;
using De.Hochstaetter.HomeAutomationClient.Extensions;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class DashboardViewModel(IWebClientService webClient, IUpdateService updateService) : ViewModelBase
{
    public IUpdateService UpdateService => updateService;

    [ObservableProperty]
    public partial bool ColorAllTicks { get; set; } = true;

    [ObservableProperty]
    public partial ISolidColorBrush LoadPowerBrush { get; set; } = new ImmutableSolidColorBrush(Color.FromUInt32(0xff807000));

    [ObservableProperty]
    public partial ISolidColorBrush GridPowerBrush { get; set; } = new ImmutableSolidColorBrush(Colors.LightGray);

    [ObservableProperty]
    public partial ISolidColorBrush GridPowerBrushL1 { get; set; } = new ImmutableSolidColorBrush(Colors.LightGray);

    [ObservableProperty]
    public partial ISolidColorBrush GridPowerBrushL2 { get; set; } = new ImmutableSolidColorBrush(Colors.LightGray);

    [ObservableProperty]
    public partial ISolidColorBrush GridPowerBrushL3 { get; set; } = new ImmutableSolidColorBrush(Colors.LightGray);

    public override async Task Initialize()
    {
        await base.Initialize();
        UpdateService.SitePowerFlowUpdated += OnSitePowerFlowUpdated;
        Application.Current!.ActualThemeVariantChanged += OnThemeChanged;
    }

    private void OnSitePowerFlowUpdated(object? sender, SitePowerFlowUpdatedEventArgs e) => _ = Dispatcher.UIThread.InvokeAsync(() => OnThemeChanged());

    private void OnThemeChanged(object? sender = null, EventArgs? e = null)
    {
        var gridPowerBrush = Application.Current!.GetSolidColorBrush("PowerFlowGrid")!;
        var solarPowerBrush = Application.Current!.GetSolidColorBrush("PowerFlowSolar")!;
        var storagePowerBrush = Application.Current!.GetSolidColorBrush("PowerFlowBattery")!;

        var incomingSolarPower = double.Max(0, UpdateService.SitePowerFlow.SolarPower);
        var incomingGridPower = double.Max(0, UpdateService.SitePowerFlow.GridPowerCorrected);
        var incomingStoragePower = double.Max(0, UpdateService.SitePowerFlow.StoragePower);
        var totalIncomingPower = incomingStoragePower + incomingGridPower + incomingSolarPower;

        double r = 0, g = 0, b = 0;

        if (totalIncomingPower > 0)
        {
            r = (incomingSolarPower / totalIncomingPower) * solarPowerBrush.Color.R + (incomingStoragePower / totalIncomingPower) * storagePowerBrush.Color.R + (incomingGridPower / totalIncomingPower) * gridPowerBrush.Color.R;
            g = (incomingSolarPower / totalIncomingPower) * solarPowerBrush.Color.G + (incomingStoragePower / totalIncomingPower) * storagePowerBrush.Color.G + (incomingGridPower / totalIncomingPower) * gridPowerBrush.Color.G;
            b = (incomingSolarPower / totalIncomingPower) * solarPowerBrush.Color.B + (incomingStoragePower / totalIncomingPower) * storagePowerBrush.Color.B + (incomingGridPower / totalIncomingPower) * gridPowerBrush.Color.B;
        }

        LoadPowerBrush = new ImmutableSolidColorBrush(Color.FromRgb(Round(r), Round(g), Round(b)));
        GridPowerBrush = UpdateService.SitePowerFlow.GridPowerCorrected < 0 ? LoadPowerBrush : gridPowerBrush;
        GridPowerBrushL1 = UpdateService.SmartMeter?.ActivePowerL1 < 0 ? LoadPowerBrush : gridPowerBrush;
        GridPowerBrushL2 = UpdateService.SmartMeter?.ActivePowerL2 < 0 ? LoadPowerBrush : gridPowerBrush;
        GridPowerBrushL3 = UpdateService.SmartMeter?.ActivePowerL3 < 0 ? LoadPowerBrush : gridPowerBrush;

        return;

        static byte Round(double value)
        {
            return (byte)Math.Round(value, MidpointRounding.ToZero);
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
        var keyedDevice = UpdateService.AllPowerConsumers.OfType<KeyedFritzBoxDevice>().First(d => d.Key == key);
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
        var keyedDevice = UpdateService.AllPowerConsumers.OfType<KeyedFritzBoxDevice>().First(d => d.Key == key);
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
        var keyedDevice = UpdateService.AllPowerConsumers.OfType<KeyedFritzBoxDevice>().First(d => d.Key == key);
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
        var keyedDevice = UpdateService.AllPowerConsumers.OfType<KeyedFritzBoxDevice>().First(d => d.Key == key);
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
        var keyedDevice = UpdateService.AllPowerConsumers.OfType<KeyedFritzBoxDevice>().First(d => d.Key == key);
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
}
