﻿namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class ToshibaHvacControl
{
    private static readonly IList<ToshibaAcFanSpeed> fanSpeeds = new[]
    {
        ToshibaAcFanSpeed.Manual1, ToshibaAcFanSpeed.Manual2, ToshibaAcFanSpeed.Manual3,
        ToshibaAcFanSpeed.Manual4, ToshibaAcFanSpeed.Manual5,
        ToshibaAcFanSpeed.Auto, ToshibaAcFanSpeed.Quiet,
    };

    // ReSharper disable once UseUtf8StringLiteral
#pragma warning disable IDE0230 
    private static readonly IList<byte> powerLimits = new[] {(byte)50, (byte)75, (byte)100,};
#pragma warning restore IDE0230

    private static readonly IReadOnlyDictionary<byte, IList<ToshibaHvacMeritFeaturesA>> meritFeatureADictionary = new Dictionary<byte, IList<ToshibaHvacMeritFeaturesA>>
    {
        {0x3c, new[] {ToshibaHvacMeritFeaturesA.None, ToshibaHvacMeritFeaturesA.Eco, ToshibaHvacMeritFeaturesA.HighPower, ToshibaHvacMeritFeaturesA.Silent2, ToshibaHvacMeritFeaturesA.Silent1}}
    };

    private CancellationTokenSource? enablerTokenSource;

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register
    (
        nameof(Device), typeof(ToshibaAcMappingDevice), typeof(ToshibaHvacControl),
        new PropertyMetadata((d, e) => ((ToshibaHvacControl)d).OnDeviceChanged(e))
    );

    public ToshibaAcMappingDevice Device
    {
        get => (ToshibaAcMappingDevice)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public ISolarSystemService SolarSystemService { get; } = null!;

    public ToshibaHvacControl()
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
        IsEnabled = true;
    });

    private void SendCommand(ToshibaAcStateData stateData)
    {
        IsEnabled = false;
        SolarSystemService.AcService.SendDeviceCommand(stateData, Device);
        enablerTokenSource = new CancellationTokenSource();
        Task.Delay(TimeSpan.FromSeconds(10), enablerTokenSource.Token).ContinueWith(_ => OnStateChanged(), enablerTokenSource.Token);
    }

    private void OnPowerClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaAcStateData {IsTurnedOn = !Device.State.IsTurnedOn});

    private void OnModeClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaAcStateData {Mode = ((HvacButton)sender).Mode});

    private void OnFanSpeedClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(fanSpeeds.IndexOf(Device.State.FanSpeed), 0);
        index = ++index % fanSpeeds.Count;
        SendCommand(new ToshibaAcStateData {FanSpeed = fanSpeeds[index]});
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

        SendCommand(new ToshibaAcStateData {TargetTemperatureCelsius = newTemperature});
    }

    private void OnPowerLimitClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(powerLimits.IndexOf(Device.State.PowerLimit), 0);
        index = ++index % powerLimits.Count;
        SendCommand(new ToshibaAcStateData {PowerLimit = powerLimits[index]});
    }

    private void OnMeritFeaturesAClicked(object sender, RoutedEventArgs e)
    {
        var key = (byte)(Device.MeritFeature >> 8);

        if (meritFeatureADictionary.ContainsKey(key))
        {
            var features = meritFeatureADictionary[key];
            var index = Math.Max(0, features.IndexOf(Device.State.MeritFeaturesA));
            index = ++index % features.Count;
            SendCommand(new ToshibaAcStateData {MeritFeaturesA = features[index]});
        }
    }
}
