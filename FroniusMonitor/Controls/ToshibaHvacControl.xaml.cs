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
        { 0x2c, new[] { ToshibaHvacMeritFeaturesA.Silent2, ToshibaHvacMeritFeaturesA.Silent1, ToshibaHvacMeritFeaturesA.Eco, ToshibaHvacMeritFeaturesA.None, ToshibaHvacMeritFeaturesA.HighPower, } },
        { 0x3c, new[] { ToshibaHvacMeritFeaturesA.Silent2, ToshibaHvacMeritFeaturesA.Silent1, ToshibaHvacMeritFeaturesA.Eco, ToshibaHvacMeritFeaturesA.None, ToshibaHvacMeritFeaturesA.HighPower, } },
    };

    private static readonly IReadOnlyDictionary<byte, IList<ToshibaHvacSwingMode>> swingModeDictionary = new Dictionary<byte, IList<ToshibaHvacSwingMode>>
    {
        {
            0x2c, new[]
            {
                ToshibaHvacSwingMode.Off,
                ToshibaHvacSwingMode.Vertical,
                ToshibaHvacSwingMode.Fixed1,
                ToshibaHvacSwingMode.Fixed2,
                ToshibaHvacSwingMode.Fixed3,
                ToshibaHvacSwingMode.Fixed4,
                ToshibaHvacSwingMode.Fixed5,
            }
        },
        {
            0x3c, new[]
            {
                ToshibaHvacSwingMode.Off,
                ToshibaHvacSwingMode.Vertical,
            }
        },
    };

    private CancellationTokenSource? enablerTokenSource;
    private readonly object tokenLock = new();
    private string? awaitedMessage;

    public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register
    (
        nameof(Device), typeof(ToshibaHvacMappingDevice), typeof(ToshibaHvacControl),
        new PropertyMetadata((d, e) => ((ToshibaHvacControl)d).OnDeviceChanged(e))
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

                menuItem.Click += (_, _) => ChangeTemperatureAbsolute((sbyte)menuItem.Tag);
                menuItem.Loaded += (_, _) => menuItem.IsChecked = Device.State.TargetTemperatureCelsius.HasValue && (sbyte)menuItem.Tag == Device.State.TargetTemperatureCelsius.Value;
                TargetTemperature.ContextMenu.Items.Add(menuItem);
            }

            CreateContextMenuForButtonSelection(HvacFanSpeedButton, fanSpeeds, nameof(HvacFanSpeedButton.FanSpeed));
            Unloaded += (_, _) => SolarSystemService.HvacService.LiveDataReceived -= OnAnswerReceived;
        }
    }

    public ISolarSystemService SolarSystemService { get; } = null!;

    private void OnDeviceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (meritFeatureADictionary.TryGetValue((byte)(Device.MeritFeature >> 8), out var featureList))
        {
            CreateContextMenuForButtonSelection(HvacMeritFeatureAButton, featureList, nameof(HvacMeritFeatureAButton.MeritFeaturesA));
        }

        if (swingModeDictionary.TryGetValue((byte)(Device.MeritFeature >> 8), out var swingModes))
        {
            CreateContextMenuForButtonSelection(HvacSwingModeButton, swingModes, nameof(HvacSwingModeButton.SwingMode));
        }
    }

    private static void CreateContextMenuForButtonSelection<TElement, TData>(TElement element, IList<TData> list, string propertyName)
        where TElement : FrameworkElement, new()
        where TData : struct
    {
        if (element.ContextMenu is not { Items: { } items })
        {
            return;
        }

        items.Clear();

        for (var i = 0; i < list.Count; i++)
        {
            var newItem = new TElement();
            typeof(TElement).GetProperty(propertyName)!.SetValue(newItem, list[i]);
            items.Add(newItem);

            if (i < items.Count - 1)
            {
                items.Add(new Separator());
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
        Dispatcher.BeginInvoke(() => IsEnabled = true);
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

    private void OnTemperatureUpClicked(object sender, RoutedEventArgs e) => ChangeTemperatureRelative(1);

    private void OnTemperatureDownClicked(object sender, RoutedEventArgs e) => ChangeTemperatureRelative(-1);

    private void ChangeTemperatureRelative(sbyte amount)
    {
        var newTemperature = (sbyte)(Device.State.TargetTemperatureCelsius + amount ?? 22);
        ChangeTemperatureAbsolute(newTemperature);
    }

    private void ChangeTemperatureAbsolute(sbyte temperatureCelsius)
    {
        temperatureCelsius = temperatureCelsius switch { > 30 => 30, < 17 => 17, _ => temperatureCelsius, };

        if (Device.State.TargetTemperatureCelsius != temperatureCelsius)
        {
            SendCommand(new ToshibaHvacStateData { TargetTemperatureCelsius = temperatureCelsius });
        }
    }

    private void OnPowerLimitClicked(object sender, RoutedEventArgs e) => OnIndexedValueClicked(powerLimits, nameof(ToshibaHvacStateData.PowerLimit));

    private void OnFanSpeedClicked(object sender, RoutedEventArgs e) => OnIndexedValueClicked(fanSpeeds, nameof(ToshibaHvacStateData.FanSpeed));

    private void OnMeritFeaturesAClicked(object sender, RoutedEventArgs e) => OnFeatureDependentIndexedValueClicked(meritFeatureADictionary, nameof(ToshibaHvacStateData.MeritFeaturesA));

    private void OnFanSpeedContextMenuItemClicked(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemClicked(sender, nameof(ToshibaHvacStateData.FanSpeed), nameof(HvacFanSpeedButton.FanSpeed));
    }

    private void OnFanSpeedContextMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemLoaded(sender, nameof(ToshibaHvacStateData.FanSpeed), nameof(HvacFanSpeedButton.FanSpeed));
    }

    private void OnPowerLimitContextMenuItemClicked(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemClicked(sender, nameof(ToshibaHvacStateData.PowerLimit));
    }

    private void OnPowerLimitContextMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemLoaded(sender, nameof(ToshibaHvacStateData.PowerLimit));
    }

    private void OnMeritFeatureAContextMenuItemClicked(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemClicked(sender, nameof(ToshibaHvacStateData.MeritFeaturesA), nameof(HvacMeritFeatureAButton.MeritFeaturesA));
    }

    private void OnMeritFeatureAContextMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemLoaded(sender, nameof(ToshibaHvacStateData.MeritFeaturesA), nameof(HvacMeritFeatureAButton.MeritFeaturesA));
    }

    private void OnWifiLedStatusClicked(object sender, RoutedEventArgs e)
    {
        var newStatus = Device.State.WifiLedStatus == ToshibaHvacWifiLedStatus.On ? ToshibaHvacWifiLedStatus.Off : ToshibaHvacWifiLedStatus.On;
        SendCommand(new ToshibaHvacStateData { WifiLedStatus = newStatus });
    }

    private void OnSwingModeClicked(object sender, RoutedEventArgs e) => OnFeatureDependentIndexedValueClicked(swingModeDictionary, nameof(ToshibaHvacStateData.SwingMode));

    private void OnSwingModeContextMenuItemClicked(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemClicked(sender, nameof(ToshibaHvacStateData.SwingMode), nameof(HvacSwingModeButton.SwingMode));
    }

    private void OnSwingModeContextMenuItemLoaded(object sender, RoutedEventArgs e)
    {
        OnButtonContextMenuItemLoaded(sender, nameof(ToshibaHvacStateData.SwingMode), nameof(HvacSwingModeButton.SwingMode));
    }

    private void OnButtonContextMenuItemClicked(object sender, string devicePropertyName, string buttonPropertyName = "")
    {
        var newState = new ToshibaHvacStateData();

        typeof(ToshibaHvacStateData).GetProperty(devicePropertyName)?.SetValue(newState, sender switch
        {
            MenuItem { Header: DependencyObject header } => header.GetType().GetProperty(buttonPropertyName)!.GetValue(header),
            MenuItem { Tag: { } tagValue } => tagValue,
            _ => throw new InvalidOperationException(),
        });

        SendCommand(newState);
    }

    private void OnButtonContextMenuItemLoaded(object sender, string devicePropertyName, string buttonPropertyName = "")
    {
        var value = typeof(ToshibaHvacStateData).GetProperty(devicePropertyName)!.GetValue(Device.State);

        ((MenuItem)sender).IsChecked = sender switch
        {
            MenuItem { Header: DependencyObject header } => header.GetType().GetProperty(buttonPropertyName)?.GetValue(header)?.Equals(value) ?? false,
            MenuItem { Tag: { } tagValue } => tagValue.Equals(value),
            _ => false,
        };
    }

    private void OnFeatureDependentIndexedValueClicked<T>(IReadOnlyDictionary<byte, IList<T>> dictionary, string propertyName) where T : struct
    {
        var key = (byte)(Device.MeritFeature >> 8);

        if (dictionary.TryGetValue(key, out var list))
        {
            OnIndexedValueClicked(list, propertyName);
        }
    }

    private void OnIndexedValueClicked<T>(IList<T> list, string propertyName) where T : struct
    {
        var property = typeof(ToshibaHvacStateData).GetProperty(propertyName);
        var index = Math.Max(list.IndexOf((T)property!.GetValue(Device.State)!), -1);
        index = ++index % list.Count;
        var newState = new ToshibaHvacStateData();
        property.SetValue(newState, list[index]);
        SendCommand(newState);
    }
}
