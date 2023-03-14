using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class ToshibaAcControl
{
    private static IList<ToshibaAcFanSpeed> fanSpeeds = new[]
    {
        ToshibaAcFanSpeed.Manual1, ToshibaAcFanSpeed.Manual2, ToshibaAcFanSpeed.Manual3,
        ToshibaAcFanSpeed.Manual4, ToshibaAcFanSpeed.Manual5,
        ToshibaAcFanSpeed.Auto, ToshibaAcFanSpeed.Quiet,
    };

    private readonly ISolarSystemService solarSystemService = null!;
    private CancellationTokenSource? enablerTokenSource;

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register
    (
        nameof(Device), typeof(ToshibaAcMappingDevice), typeof(ToshibaAcControl),
        new PropertyMetadata((d, e) => ((ToshibaAcControl)d).OnDeviceChanged(e))
    );

    public ToshibaAcMappingDevice Device
    {
        get => (ToshibaAcMappingDevice)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public ToshibaAcControl()
    {
        InitializeComponent();

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            solarSystemService = IoC.Get<ISolarSystemService>();
        }

        Unloaded += (_, _) => Device.State.PropertyChanged -= OnStateChanged;
    }

    private void OnDeviceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ToshibaAcMappingDevice oldDevice)
        {
            oldDevice.State.PropertyChanged -= OnStateChanged;
        }

        if (e.NewValue is ToshibaAcMappingDevice newDevice)
        {
            newDevice.State.PropertyChanged += OnStateChanged;
        }
    }

    private void OnStateChanged(object? sender = null, PropertyChangedEventArgs? e = null) => Dispatcher.InvokeAsync(() =>
    {
        enablerTokenSource?.Cancel();
        enablerTokenSource?.Dispose();
        enablerTokenSource = null;
        return IsEnabled = true;
    });

    private void SendCommand(Func<ToshibaAcStateData> func)
    {
        IsEnabled = false;
        var stateData = func();
        solarSystemService.AcService.SendDeviceCommand(stateData, Device);
        enablerTokenSource = new CancellationTokenSource();
        Task.Delay(TimeSpan.FromSeconds(10), enablerTokenSource.Token).ContinueWith(_ => OnStateChanged(), enablerTokenSource.Token);
    }

    private void OnPowerClicked(object sender, RoutedEventArgs e) => SendCommand(() => new ToshibaAcStateData { IsTurnedOn = !Device.State.IsTurnedOn });

    private void OnModeClicked(object sender, RoutedEventArgs e) => SendCommand(() => new ToshibaAcStateData { Mode = ((HvacButton)sender).Mode });

    private void OnFanSpeedClicked(object sender, RoutedEventArgs e)
    {
        var index = fanSpeeds.IndexOf(Device.State.FanSpeed);

        if (index < 0)
        {
            return;
        }

        index = ++index % fanSpeeds.Count;

        SendCommand(() => new ToshibaAcStateData { FanSpeed = fanSpeeds[index] });
    }
}
