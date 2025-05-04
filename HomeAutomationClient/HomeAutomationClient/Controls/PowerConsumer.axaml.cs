namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class PowerConsumer : DeviceControlBase
{
    #region Avalonia properties

    public static readonly StyledProperty<IPowerConsumer1P?> DeviceProperty = AvaloniaProperty.Register<PowerConsumer, IPowerConsumer1P?>(nameof(Device));

    public IPowerConsumer1P? Device
    {
        get => GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public static readonly StyledProperty<ICommand?> SwitchCommandProperty = AvaloniaProperty.Register<PowerConsumer, ICommand?>(nameof(SwitchCommand));

    public ICommand? SwitchCommand
    {
        get => GetValue(SwitchCommandProperty);
        set => SetValue(SwitchCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> SetBrightnessCommandProperty = AvaloniaProperty.Register<PowerConsumer, ICommand?>(nameof(SetBrightnessCommand));

    public ICommand? SetBrightnessCommand
    {
        get => GetValue(SetBrightnessCommandProperty);
        set => SetValue(SetBrightnessCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> SetColorTemperatureCommandProperty = AvaloniaProperty.Register<PowerConsumer, ICommand?>(nameof(SetColorTemperatureCommand));

    public ICommand? SetColorTemperatureCommand
    {
        get => GetValue(SetColorTemperatureCommandProperty);
        set => SetValue(SetColorTemperatureCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> SetHueCommandProperty = AvaloniaProperty.Register<PowerConsumer, ICommand?>(nameof(SetHueCommand));

    public ICommand? SetHueCommand
    {
        get => GetValue(SetHueCommandProperty);
        set => SetValue(SetHueCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> SetSaturationCommandProperty = AvaloniaProperty.Register<PowerConsumer, ICommand?>(nameof(SetSaturationCommand));

    public ICommand? SetSaturationCommand
    {
        get => GetValue(SetSaturationCommandProperty);
        set => SetValue(SetSaturationCommandProperty, value);
    }

    #endregion

    public PowerConsumer()
    {
        InitializeComponent();
        DimSlider.AddHandler(PointerReleasedEvent, OnDimSliderReleased, RoutingStrategies.Tunnel);
        TemperatureSlider.AddHandler(PointerReleasedEvent, OnTemperatureSliderReleased, RoutingStrategies.Tunnel);
        HueSlider.AddHandler(PointerReleasedEvent, OnHueSliderReleased, RoutingStrategies.Tunnel);
        SaturationSlider.AddHandler(PointerReleasedEvent, OnSaturationSliderReleased, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.Property.Name)
        {
            case nameof(Device):
                if (e.OldValue is INotifyPropertyChanged oldDevice)
                {
                    oldDevice.PropertyChanged -= OnDevicePropertyChanged;
                }

                if (e.NewValue is INotifyPropertyChanged newDevice)
                {
                    newDevice.PropertyChanged += OnDevicePropertyChanged;
                    OnDevicePropertyChanged(Device, new PropertyChangedEventArgs(string.Empty));
                }

                break;
        }
    }

    protected override void ChangeInner()
    {
        // PowerConsumer has no inner border
    }

    protected override void ChangeOuter()
    {
        BackgroundProvider.Background = Device is not { IsPresent: true }
            ? OuterFault
            : Device.IsTurnedOn == null || Device.IsTurnedOn.Value
                ? OuterRunning
                : OuterOther;
    }

    private void OnDevicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == string.Empty)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ChangeOuter();
                ChangeInner();
            });
        }
    }

    private async void OnPowerButtonClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SwitchCommand?.CanExecute(DeviceKey) is not true)
            {
                return;
            }

            SwitchCommand.Execute(DeviceKey);
        }
        catch (Exception ex)
        {
            await ex.Show().ConfigureAwait(false);
        }
    }

    private void OnDimSliderReleased(object? sender, PointerReleasedEventArgs e)
    {
        ExecuteSliderReleasedCommand(sender, e, Device?.Level, SetBrightnessCommand);
    }

    private void OnTemperatureSliderReleased(object? sender, PointerReleasedEventArgs e)
    {
        ExecuteSliderReleasedCommand(sender, e, Device?.ColorTemperatureKelvin, SetColorTemperatureCommand);
    }

    private void OnHueSliderReleased(object? sender, PointerReleasedEventArgs e)
    {
        ExecuteSliderReleasedCommand(sender, e, Device?.HueDegrees, SetHueCommand);
    }

    private void OnSaturationSliderReleased(object? sender, PointerReleasedEventArgs e)
    {
        ExecuteSliderReleasedCommand(sender, e, Device?.Saturation, SetSaturationCommand);
    }

    private void ExecuteSliderReleasedCommand(object? sender, PointerReleasedEventArgs e, double? oldValue, ICommand? command)
    {
        if (e.Handled || sender is not Slider slider)
        {
            return;
        }

        var result = new ValueChangeCommandParameter<double>(DeviceKey, oldValue ?? double.NaN, slider.Value);

        if (command?.CanExecute(result) is true)
        {
            command.Execute(result);
        }
    }
}
