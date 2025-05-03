using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public partial class PowerConsumer : DeviceControlBase
{
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

    public static readonly StyledProperty<object?> SwitchCommandParameterProperty = AvaloniaProperty.Register<PowerConsumer, object?>(nameof(SwitchCommandParameter));

    public object? SwitchCommandParameter
    {
        get => GetValue(SwitchCommandParameterProperty);
        set => SetValue(SwitchCommandParameterProperty, value);
    }

    public PowerConsumer()
    {
        InitializeComponent();
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
        // Do nothing
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
            if (sender is not ToggleButton button || SwitchCommand?.CanExecute(SwitchCommandParameter) is not true)
            {
                return;
            }

            SwitchCommand.Execute(SwitchCommandParameter);
        }
        catch (Exception ex)
        {
            await ex.Show().ConfigureAwait(false);
        }
    }
}