using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class DashboardViewModel(IWebClientService webClient, IUpdateService updateService) : ViewModelBase
{
    public IUpdateService UpdateService => updateService;

    [ObservableProperty]
    public partial bool ColorAllTicks { get; set; } = true;

    [ObservableProperty]
    public partial HaColor LoadPowerColor { get; set; } = 0xff807000;

    [ObservableProperty]
    public partial HaColor GridPowerColor { get; set; } = HaColors.LightGray;

    [ObservableProperty]
    public partial HaColor GridPowerColorL1 { get; set; } = HaColors.LightGray;

    [ObservableProperty]
    public partial HaColor GridPowerColorL2 { get; set; } = HaColors.LightGray;

    [ObservableProperty]
    public partial HaColor GridPowerColorL3 { get; set; } = HaColors.LightGray;

    [ObservableProperty]
    public partial HaColor StorageColor { get; set; } = HaColors.LightGray;

    /// <summary>
    /// Recalculates the power flow colors from the current <see cref="IUpdateService.SitePowerFlow"/> and the theme colors passed in.
    /// </summary>
    /// <remarks>
    /// The theme colors are supplied by the view, because looking them up requires the UI framework.
    /// The view must also call this method whenever the theme or the site power flow changes.
    /// </remarks>
    /// <param name="gridColor">The color representing power coming from or going to the grid.</param>
    /// <param name="solarColor">The color representing power produced by the solar panels.</param>
    /// <param name="storageColor">The color representing power coming from or going to the battery.</param>
    public void UpdatePowerFlowColors(HaColor gridColor, HaColor solarColor, HaColor storageColor)
    {
        var incomingSolarPower = double.Max(0, UpdateService.SitePowerFlow.SolarPower);
        var incomingGridPower = double.Max(0, UpdateService.SitePowerFlow.GridPowerCorrected);
        var incomingStoragePower = double.Max(0, UpdateService.SitePowerFlow.StoragePower);
        var totalIncomingPower = incomingStoragePower + incomingGridPower + incomingSolarPower;

        double r = 0, g = 0, b = 0;

        if (totalIncomingPower > 0)
        {
            r = (incomingSolarPower / totalIncomingPower) * solarColor.R + (incomingStoragePower / totalIncomingPower) * storageColor.R + (incomingGridPower / totalIncomingPower) * gridColor.R;
            g = (incomingSolarPower / totalIncomingPower) * solarColor.G + (incomingStoragePower / totalIncomingPower) * storageColor.G + (incomingGridPower / totalIncomingPower) * gridColor.G;
            b = (incomingSolarPower / totalIncomingPower) * solarColor.B + (incomingStoragePower / totalIncomingPower) * storageColor.B + (incomingGridPower / totalIncomingPower) * gridColor.B;
        }

        LoadPowerColor = HaColor.FromRgb(Round(r), Round(g), Round(b));
        GridPowerColor = UpdateService.SitePowerFlow.GridPowerCorrected < 0 ? LoadPowerColor : gridColor;
        GridPowerColorL1 = UpdateService.SmartMeter?.ActivePowerL1 < 0 ? LoadPowerColor : gridColor;
        GridPowerColorL2 = UpdateService.SmartMeter?.ActivePowerL2 < 0 ? LoadPowerColor : gridColor;
        GridPowerColorL3 = UpdateService.SmartMeter?.ActivePowerL3 < 0 ? LoadPowerColor : gridColor;
        StorageColor = UpdateService.SitePowerFlow.StoragePower < 0 ? LoadPowerColor : storageColor;

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
