namespace De.Hochstaetter.FroniusMonitor.Controls;

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
    private static readonly IList<byte> powerLimits = new[] { (byte)50, (byte)75, (byte)100, };
#pragma warning restore IDE0230

    private static readonly IReadOnlyDictionary<byte, IList<ToshibaHvacMeritFeaturesA>> meritFeatureADictionary = new Dictionary<byte, IList<ToshibaHvacMeritFeaturesA>>
    {
        { 0x3c, new[] { ToshibaHvacMeritFeaturesA.Silent2, ToshibaHvacMeritFeaturesA.Silent1, ToshibaHvacMeritFeaturesA.Eco, ToshibaHvacMeritFeaturesA.None, ToshibaHvacMeritFeaturesA.HighPower, } }
    };

    private CancellationTokenSource? enablerTokenSource;
    private readonly object tokenLock = new();
    private string? awaitedMessage;

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register
    (
        nameof(Device), typeof(ToshibaHvacMappingDevice), typeof(ToshibaHvacControl),
        new PropertyMetadata((d, _) => ((ToshibaHvacControl)d).OnDeviceChanged())
    );

    public ToshibaHvacMappingDevice Device
    {
        get => (ToshibaHvacMappingDevice)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public ToshibaHvacControl()
    {
        InitializeComponent();

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            SolarSystemService = IoC.Get<ISolarSystemService>();
            TargetTemperature.ContextMenu = new ContextMenu();

            for (sbyte temperature = 30; temperature > 16; temperature--)
            {
                var menuItem = new MenuItem
                {
                    Header = $"{temperature:00} °C",
                    Tag = temperature,
                };

                menuItem.Click += (_, _) => { ChangeTemperatureAbsolute((sbyte)menuItem.Tag); };

                menuItem.Loaded += (_, _) => { menuItem.IsChecked = Device.State.TargetTemperatureCelsius.HasValue && (sbyte)menuItem.Tag == Device.State.TargetTemperatureCelsius.Value; };

                TargetTemperature.ContextMenu.Items.Add(menuItem);
            }

            Unloaded += (_, _) => SolarSystemService.HvacService.LiveDataReceived -= OnAnswerReceived;
        }
    }

    public ISolarSystemService SolarSystemService { get; } = null!;

    private void OnDeviceChanged()
    {
        HvacMeritFeatureAButton.ContextMenu?.Items.Clear();
        HvacFanSpeedButton.ContextMenu?.Items.Clear();

        if (HvacMeritFeatureAButton.ContextMenu is { Items: { } meritFeatureAItems } && meritFeatureADictionary.TryGetValue((byte)(Device.MeritFeature >> 8), out var featureList))
        {
            for (var i = 0; i < featureList.Count; i++)
            {
                meritFeatureAItems.Add(new HvacMeritFeatureAButton { MeritFeaturesA = featureList[i] });

                if (i < featureList.Count - 1)
                {
                    meritFeatureAItems.Add(new Separator());
                }
            }
        }

        if (HvacFanSpeedButton.ContextMenu is { Items: { } fanSpeedItems })
        {
            for (var i = 0; i < fanSpeeds.Count; i++)
            {
                fanSpeedItems.Add(new HvacFanSpeedButton { FanSpeed = fanSpeeds[i] });

                if (i < fanSpeeds.Count - 1)
                {
                    fanSpeedItems.Add(new Separator());
                }
            }
        }
    }

    private void OnStateChanged()
    {
        lock (tokenLock)
        {
            enablerTokenSource?.Cancel();
            enablerTokenSource?.Dispose();
            enablerTokenSource = null;
        }

        SolarSystemService.HvacService.LiveDataReceived -= OnAnswerReceived;

        Dispatcher.BeginInvoke(() =>
        {
            PowerButton.IsChecked = Device.State.IsTurnedOn;
            IsEnabled = true;
        });
    }

    private async void SendCommand(ToshibaHvacStateData stateData)
    {
        IsEnabled = false;
        SolarSystemService.HvacService.LiveDataReceived += OnAnswerReceived;
        awaitedMessage = await SolarSystemService.HvacService.SendDeviceCommand(stateData, Device).ConfigureAwait(false);

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

    private void OnPowerClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaHvacStateData { IsTurnedOn = !Device.State.IsTurnedOn });

    private void OnModeClicked(object sender, RoutedEventArgs e) => SendCommand(new ToshibaHvacStateData { Mode = ((HvacButton)sender).Mode });

    private void OnFanSpeedClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(fanSpeeds.IndexOf(Device.State.FanSpeed), 0);
        index = ++index % fanSpeeds.Count;
        SendCommand(new ToshibaHvacStateData { FanSpeed = fanSpeeds[index] });
    }

    private void OnTemperatureUpClicked(object sender, RoutedEventArgs e) => ChangeTemperatureRelative(1);

    private void OnTemperatureDownClicked(object sender, RoutedEventArgs e) => ChangeTemperatureRelative(-1);

    private void ChangeTemperatureRelative(sbyte amount)
    {
        var newTemperature = Math.Max(Math.Min((sbyte)30, (sbyte)(Device.State.TargetTemperatureCelsius! + amount)), (sbyte)17);

        if (newTemperature == Device.State.TargetTemperatureCelsius)
        {
            return;
        }

        SendCommand(new ToshibaHvacStateData { TargetTemperatureCelsius = newTemperature });
    }

    private void ChangeTemperatureAbsolute(sbyte temperatureCelsius)
    {
        if (Device.State.TargetTemperatureCelsius.HasValue)
        {
            ChangeTemperatureRelative(unchecked((sbyte)(temperatureCelsius - Device.State.TargetTemperatureCelsius.Value)));
        }
    }

    private void OnPowerLimitClicked(object sender, RoutedEventArgs e)
    {
        var index = Math.Max(powerLimits.IndexOf(Device.State.PowerLimit), 0);
        index = ++index % powerLimits.Count;
        SendCommand(new ToshibaHvacStateData { PowerLimit = powerLimits[index] });
    }

    private void OnMeritFeaturesAClicked(object sender, RoutedEventArgs e)
    {
        var key = (byte)(Device.MeritFeature >> 8);

        if (meritFeatureADictionary.ContainsKey(key))
        {
            var features = meritFeatureADictionary[key];
            var index = Math.Max(0, features.IndexOf(Device.State.MeritFeaturesA));
            index = ++index % features.Count;
            SendCommand(new ToshibaHvacStateData { MeritFeaturesA = features[index] });
        }
    }

    private void OnFanSpeedContextMenuClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Header: HvacFanSpeedButton button })
        {
            SendCommand(new ToshibaHvacStateData { FanSpeed = button.FanSpeed });
        }
    }

    private void OnFanSpeedContextMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Header: HvacFanSpeedButton button } menuItem)
        {
            menuItem.IsChecked = Device.State.FanSpeed == button.FanSpeed;
        }
    }

    private void OnPowerLimitContextMenuItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: byte powerLimit })
        {
            SendCommand(new ToshibaHvacStateData { PowerLimit = powerLimit });
        }
    }

    private void OnPowerLimitContextMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: byte powerLimit } menuItem)
        {
            menuItem.IsChecked = Device.State.PowerLimit == powerLimit;
        }
    }

    private void OnMeritFeatureAContextMenuItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Header: HvacMeritFeatureAButton button })
        {
            SendCommand(new ToshibaHvacStateData { MeritFeaturesA = button.MeritFeaturesA });
        }
    }

    private void OnMeritFeatureAContextMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Header: HvacMeritFeatureAButton button } menuItem)
        {
            menuItem.IsChecked = Device.State.MeritFeaturesA == button.MeritFeaturesA;
        }
    }
}
