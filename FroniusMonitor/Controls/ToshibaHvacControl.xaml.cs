﻿namespace De.Hochstaetter.FroniusMonitor.Controls;

public partial class ToshibaHvacControl
{
    private static readonly IList<ToshibaHvacFanSpeed> fanSpeeds = new[]
    {
        ToshibaHvacFanSpeed.Manual1, ToshibaHvacFanSpeed.Manual2, ToshibaHvacFanSpeed.Manual3,
        ToshibaHvacFanSpeed.Manual4, ToshibaHvacFanSpeed.Manual5,
        ToshibaHvacFanSpeed.Auto, ToshibaHvacFanSpeed.Quiet,
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
    private readonly object tokenLock = new();
    private string? awaitedMessage;

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register
    (
        nameof(Device), typeof(ToshibaHvacMappingDevice), typeof(ToshibaHvacControl)
    );

    public ToshibaHvacMappingDevice Device
    {
        get => (ToshibaHvacMappingDevice)GetValue(DeviceProperty);
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

        Unloaded += (_, _) => SolarSystemService.AcService.LiveDataReceived -= OnAnswerReceived;
    }

    private void OnStateChanged()
    {
        lock (tokenLock)
        {
            enablerTokenSource?.Cancel();
            enablerTokenSource?.Dispose();
            enablerTokenSource = null;
        }

        SolarSystemService.AcService.LiveDataReceived -= OnAnswerReceived;

        Dispatcher.BeginInvoke(() =>
        {
            PowerButton.IsChecked = Device.State.IsTurnedOn;
            IsEnabled = true;
        });
    }

    private async void SendCommand(ToshibaHvacStateData stateData)
    {
        IsEnabled = false;
        SolarSystemService.AcService.LiveDataReceived += OnAnswerReceived;
        awaitedMessage = await SolarSystemService.AcService.SendDeviceCommand(stateData, Device).ConfigureAwait(false);

        lock (tokenLock)
        {
            enablerTokenSource = new CancellationTokenSource();
            _ = Task.Delay(TimeSpan.FromSeconds(10), enablerTokenSource.Token).ContinueWith(_ => OnStateChanged(), enablerTokenSource.Token);
        }
    }

    private void OnAnswerReceived(object? sender, ToshibaHvacAzureSmMobileCommand command)
    {
        if (command.MessageId == awaitedMessage)
        {
            OnStateChanged();
        }
    }

    private void OnPowerClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaHvacStateData {IsTurnedOn = !Device.State.IsTurnedOn});

    private void OnModeClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaHvacStateData {Mode = ((HvacButton)sender).Mode});

    private void OnFanSpeedClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(fanSpeeds.IndexOf(Device.State.FanSpeed), 0);
        index = ++index % fanSpeeds.Count;
        SendCommand(new ToshibaHvacStateData {FanSpeed = fanSpeeds[index]});
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

        SendCommand(new ToshibaHvacStateData {TargetTemperatureCelsius = newTemperature});
    }

    private void OnPowerLimitClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(powerLimits.IndexOf(Device.State.PowerLimit), 0);
        index = ++index % powerLimits.Count;
        SendCommand(new ToshibaHvacStateData {PowerLimit = powerLimits[index]});
    }

    private void OnMeritFeaturesAClicked(object sender, RoutedEventArgs e)
    {
        var key = (byte)(Device.MeritFeature >> 8);

        if (meritFeatureADictionary.ContainsKey(key))
        {
            var features = meritFeatureADictionary[key];
            var index = Math.Max(0, features.IndexOf(Device.State.MeritFeaturesA));
            index = ++index % features.Count;
            SendCommand(new ToshibaHvacStateData {MeritFeaturesA = features[index]});
        }
    }
}
