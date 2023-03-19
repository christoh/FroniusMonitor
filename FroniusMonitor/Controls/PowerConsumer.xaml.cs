namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class PowerConsumer
{
    private static readonly ISolarSystemService solarSystemService = IoC.TryGet<ISolarSystemService>()!;

    #region Dependency Properties

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register
    (
        nameof(Device), typeof(IPowerConsumer1P), typeof(PowerConsumer),
        new PropertyMetadata(null, (d, e) => ((PowerConsumer)d).OnFritzBoxDeviceChanged(e))
    );

    public IPowerConsumer1P? Device
    {
        get => (IPowerConsumer1P?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    #endregion

    public PowerConsumer()
    {
        InitializeComponent();
    }

    private void OnFritzBoxDeviceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldDevice)
        {
            oldDevice.PropertyChanged -= OnDevicePropertyChanged;
        }

        if (e.NewValue is INotifyPropertyChanged newDevice)
        {
            newDevice.PropertyChanged += OnDevicePropertyChanged;
            OnDevicePropertyChanged(Device, new PropertyChangedEventArgs(string.Empty));
        }
    }

    private void OnDevicePropertyChanged(object? sender, PropertyChangedEventArgs e) => Dispatcher.InvokeAsync(() =>
    {
        if (e.PropertyName == string.Empty)
        {
            BackgroundProvider.Background = Device is not { IsPresent: true }
                ? Brushes.OrangeRed
                : Device.IsTurnedOn == null || Device.IsTurnedOn.Value
                    ? Brushes.AntiqueWhite
                    : Brushes.LightGray;
        }
    });

    private async void OnPowerButtonClick(object sender, RoutedEventArgs e)
    {
        if (Device is not {IsPresent: true, CanSwitch: true}) return;
        solarSystemService.SuspendPowerConsumers();

        try
        {
            await Device.TurnOnOff(!Device.IsTurnedOn.HasValue || !Device.IsTurnedOn.Value).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
        }
        finally
        {
            solarSystemService.ResumePowerConsumers();
        }
    }

    private async void OnDimLevelChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Device?.Level is not { } level || Math.Abs(e.NewValue - level) < .00001)
        {
            return;
        }

        await Device.SetLevel(e.NewValue).ConfigureAwait(false);
    }

    private async void OnColorTemperatureChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Device == null || Device.ColorTemperatureKelvin is { } colorTemperatureKelvin && Math.Abs(e.NewValue - colorTemperatureKelvin) < .00001)
        {
            return;
        }

        await Device.SetColorTemperature(e.NewValue).ConfigureAwait(false);
    }

    private async void OnHueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Device == null || Device.HueDegrees is { } hueDegrees && Math.Abs(e.NewValue - hueDegrees) < .00001)
        {
            return;
        }

        await Device.SetHsv(e.NewValue, Device?.Saturation ?? 1, Device?.Value ?? 1).ConfigureAwait(false);
    }

    private async void OnSaturationChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Device == null || Device.Saturation is { } saturation && Math.Abs(e.NewValue - saturation) < .00001)
        {
            return;
        }

        await Device.SetHsv(Device?.HueDegrees ?? 0, e.NewValue, Device?.Value ?? 1).ConfigureAwait(false);
    }
}
