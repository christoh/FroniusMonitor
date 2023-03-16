using De.Hochstaetter.Fronius.Models.ToshibaAc;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class ToshibaAcControl
{
    private static readonly IList<ToshibaAcFanSpeed> fanSpeeds = new[]
    {
        ToshibaAcFanSpeed.Manual1, ToshibaAcFanSpeed.Manual2, ToshibaAcFanSpeed.Manual3,
        ToshibaAcFanSpeed.Manual4, ToshibaAcFanSpeed.Manual5,
        ToshibaAcFanSpeed.Auto, ToshibaAcFanSpeed.Quiet,
    };

    // ReSharper disable once UseUtf8StringLiteral
    private static readonly IList<byte> powerLimits = new[] { (byte)50, (byte)75, (byte)100, };

    private static IReadOnlyDictionary<byte, IReadOnlyList<ToshibaHvacMeritFeatureA>> meritFeatureADictionary = new Dictionary<byte, IReadOnlyList<ToshibaHvacMeritFeatureA>>
    {
        {0x3c, new[] {ToshibaHvacMeritFeatureA.None, ToshibaHvacMeritFeatureA.Eco, ToshibaHvacMeritFeatureA.HighPower, ToshibaHvacMeritFeatureA.Silent1, ToshibaHvacMeritFeatureA.Silent1}}
    };

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

    public ISolarSystemService SolarSystemService { get; } = null!;

    public ToshibaAcControl()
    {
        InitializeComponent();

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            SolarSystemService = IoC.Get<ISolarSystemService>();
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
        PowerButton.IsChecked = Device.State.IsTurnedOn;
        return IsEnabled = true;
    });

    private void SendCommand(ToshibaAcStateData stateData)
    {
        IsEnabled = false;
        SolarSystemService.AcService.SendDeviceCommand(stateData, Device);
        enablerTokenSource = new CancellationTokenSource();
        Task.Delay(TimeSpan.FromSeconds(10), enablerTokenSource.Token).ContinueWith(_ => OnStateChanged(), enablerTokenSource.Token);
    }

    private void OnPowerClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaAcStateData { IsTurnedOn = !Device.State.IsTurnedOn });

    private void OnModeClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaAcStateData { Mode = ((HvacButton)sender).Mode });

    private void OnFanSpeedClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(fanSpeeds.IndexOf(Device.State.FanSpeed), 0);
        index = ++index % fanSpeeds.Count;
        SendCommand(new ToshibaAcStateData { FanSpeed = fanSpeeds[index] });
    }

    private void OnTemperatureUpClicked(object sender, RoutedEventArgs e) => ChangeTemperature(1);

    private void OnTemperatureDownClicked(object sender, RoutedEventArgs e) => ChangeTemperature(-1);

    private void ChangeTemperature(sbyte amount)
    {
        var newTemperature = Math.Max(Math.Min((sbyte)30, (sbyte)(Device.State.TargetTemperatureCelsius! + amount)), (sbyte)17);

        if (newTemperature == Device.State.TargetTemperatureCelsius)
        {
            return;
        }

        SendCommand(new ToshibaAcStateData { TargetTemperatureCelsius = newTemperature });
    }

    private void OnPowerLimitClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(powerLimits.IndexOf(Device.State.PowerLimit), 0);
        index = ++index % powerLimits.Count;
        SendCommand(new ToshibaAcStateData { PowerLimit = powerLimits[index] });
    }
}
