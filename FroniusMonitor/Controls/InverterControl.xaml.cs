using System.Reflection;

namespace De.Hochstaetter.FroniusMonitor.Controls;

public enum InverterDisplayMode
{
    AcPhaseVoltage,
    AcPhaseVoltageGauge,
    AcLineVoltage,
    AcCurrent,
    AcPowerActive,
    AcPowerActiveGauge,
    AcPowerApparent,
    AcPowerReactive,
    AcPowerFactor,
    DcPowerGauge,
    DcVoltage,
    DcCurrent,
    DcPower,
    DcRelativePower,
    MpptComparison,
    EnergyInverter,
    EnergyRectifier,
    EnergyStorage,
    EnergySolar,
    More,
    MoreEfficiency,
    MoreTemperatures,
    MoreFans,
    MoreVersions,
    MoreOp,
}

public partial class InverterControl
{
    private readonly IDataCollectionService? dataCollectionService = IoC.TryGetRegistered<IDataCollectionService>();
    private IGen24Service? gen24Service;
    private IServiceProvider provider = IoC.Injector!;

    private static readonly IReadOnlyList<InverterDisplayMode> acModes =
    [
        InverterDisplayMode.AcPowerActiveGauge,
        InverterDisplayMode.AcPowerActive,
        InverterDisplayMode.AcPowerApparent,
        InverterDisplayMode.AcPowerReactive,
        InverterDisplayMode.AcPowerFactor,
        InverterDisplayMode.AcCurrent,
        InverterDisplayMode.AcPhaseVoltageGauge,
        InverterDisplayMode.AcPhaseVoltage,
        InverterDisplayMode.AcLineVoltage,
    ];

    private static readonly IReadOnlyList<InverterDisplayMode> dcModes =
    [
        InverterDisplayMode.DcPowerGauge,
        InverterDisplayMode.DcPower,
        InverterDisplayMode.DcRelativePower,
        InverterDisplayMode.MpptComparison,
        InverterDisplayMode.DcCurrent,
        InverterDisplayMode.DcVoltage,
    ];

    private static readonly IReadOnlyList<InverterDisplayMode> moreModes =
    [
        InverterDisplayMode.MoreEfficiency,
        InverterDisplayMode.More,
        InverterDisplayMode.MoreTemperatures,
        InverterDisplayMode.MoreFans,
        InverterDisplayMode.MoreOp,
        InverterDisplayMode.MoreVersions
    ];

    private static readonly IReadOnlyList<InverterDisplayMode> energyModes =
    [
        InverterDisplayMode.EnergySolar,
        InverterDisplayMode.EnergyInverter,
        InverterDisplayMode.EnergyRectifier,
        InverterDisplayMode.EnergyStorage,
    ];

    private int currentAcIndex, currentDcIndex, currentMoreIndex, energyIndex;
    private bool isInStandByChange;
    private string? lastStatusCode;
    private DateTime lastStandbySwitchUpdate = DateTime.UnixEpoch;
    private bool isInStartingAnimation;

    private static readonly ColorAnimation startingAnimation = new()
    {
        To = Colors.AntiqueWhite,
        From = Colors.LightGray,
        AutoReverse = true,
        RepeatBehavior = RepeatBehavior.Forever,
        Duration = TimeSpan.FromSeconds(1.5),
    };

    #region Dependency Properties

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
    (
        nameof(Mode), typeof(InverterDisplayMode), typeof(InverterControl),
        new PropertyMetadata(InverterDisplayMode.DcPowerGauge, (d, _) => ((InverterControl)d).OnModeChanged())
    );

    public InverterDisplayMode Mode
    {
        get => (InverterDisplayMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty IsSecondaryProperty = DependencyProperty.Register
    (
        nameof(IsSecondary), typeof(bool), typeof(InverterControl),
        new PropertyMetadata((d, _) => ((InverterControl)d).OnIsSecondaryChanged())
    );

    public bool IsSecondary
    {
        get => (bool)GetValue(IsSecondaryProperty);
        set => SetValue(IsSecondaryProperty, value);
    }

    public static readonly DependencyProperty VersionsProperty = DependencyProperty.Register
    (
        nameof(Versions), typeof(Gen24Versions), typeof(InverterControl)
    );

    public Gen24Versions? Versions
    {
        get => (Gen24Versions?)GetValue(VersionsProperty);
        set => SetValue(VersionsProperty, value);
    }

    public static readonly DependencyProperty SensorsProperty = DependencyProperty.Register
    (
        nameof(Sensors), typeof(Gen24Sensors), typeof(InverterControl)
    );

    public Gen24Sensors? Sensors
    {
        get => (Gen24Sensors?)GetValue(SensorsProperty);
        set => SetValue(SensorsProperty, value);
    }

    public static readonly DependencyProperty ConfigProperty = DependencyProperty.Register
    (
        nameof(Config), typeof(Gen24Config), typeof(InverterControl)
    );

    public Gen24Config? Config
    {
        get => (Gen24Config?)GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    #endregion

    public InverterControl()
    {
        gen24Service = dataCollectionService?.Gen24Service;
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (dataCollectionService != null)
            {
                dataCollectionService.NewDataReceived += NewDataReceived;
            }
        };

        Unloaded += (_, _) =>
        {
            if (dataCollectionService != null)
            {
                dataCollectionService.NewDataReceived -= NewDataReceived;
            }
        };
    }

    private void OnModeChanged() => NewDataReceived(this, new SolarDataEventArgs(dataCollectionService?.HomeAutomationSystem));

    private void OnIsSecondaryChanged()
    {
        gen24Service = IsSecondary switch
        {
            false => dataCollectionService?.Gen24Service,
            true => dataCollectionService?.Gen24Service2,
        };

        provider = IsSecondary switch
        {
            false => dataCollectionService!.Container,
            true => dataCollectionService!.Container2,
        };
    }

    private async void NewDataReceived(object? sender, SolarDataEventArgs e)
    {
        Gen24Sensors? gen24Sensors = null!;
        Gen24Config? gen24Config = null!;

        Dispatcher.Invoke(() =>
        {
            gen24Sensors = IsSecondary ? e.HomeAutomationSystem?.Gen24Sensors2 : e.HomeAutomationSystem?.Gen24Sensors;
            gen24Config = IsSecondary ? e.HomeAutomationSystem?.Gen24Config2 : e.HomeAutomationSystem?.Gen24Config;
        });

        var inverter = gen24Sensors?.Inverter;
        var dataManager = gen24Sensors?.DataManager;
        var sitePowerFlow = e.HomeAutomationSystem?.SitePowerFlow;

        if
        (
            !isInStandByChange &&
            ((DateTime.UtcNow - lastStandbySwitchUpdate).TotalMinutes > 5 || lastStatusCode != gen24Sensors?.InverterStatus?.StatusCode) &&
            gen24Service != null
        )
        {
            try
            {
                var standByStatus = await gen24Service.GetInverterStandByStatus().ConfigureAwait(false);
                _ = Dispatcher.InvokeAsync(() => StandByButton.IsChecked = !standByStatus!.IsStandBy);
                lastStandbySwitchUpdate = DateTime.UtcNow;
            }
            catch
            {
                // if it does not update, we don't care
            }
        }

        lastStatusCode = gen24Sensors?.InverterStatus?.StatusCode;

        _ = Dispatcher.InvokeAsync(() =>
        {
            var cache = gen24Sensors?.Cache;
            var gen24Common = IsSecondary ? e.HomeAutomationSystem?.Gen24Config2?.InverterSettings : e.HomeAutomationSystem?.Gen24Config?.InverterSettings;

            if (cache == null && inverter == null && dataManager == null)
            {
                return;
            }

            if (gen24Sensors?.InverterStatus?.StatusCode is "STATE_STARTUP")
            {
                if (!isInStartingAnimation)
                {
                    var brush = new SolidColorBrush(Colors.LightGray);
                    BackgroundProvider.Background = brush;
                    BackgroundProvider.Background.BeginAnimation(SolidColorBrush.ColorProperty, startingAnimation);
                    isInStartingAnimation = true;
                }
            }
            else
            {
                if (isInStartingAnimation)
                {
                    BackgroundProvider.BeginAnimation(SolidColorBrush.ColorProperty, null);
                }

                isInStartingAnimation = false;
                BackgroundProvider.Background = gen24Sensors?.InverterStatus?.ToBrush() ?? Brushes.LightGray;
            }

            InverterModelName.Text = $"{gen24Config.Versions?.ModelName ?? "---"} ({gen24Sensors?.InverterStatus?.StatusMessageCaption ?? Loc.Unknown})";
            InverterName.Text = gen24Common?.SystemName ?? "---";

            Enum.GetNames<InverterDisplayMode>().Apply(enumName =>
            {
                if (GetType().GetField(enumName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(this) is FrameworkElement element)
                {
                    element.Visibility = Mode.ToString() == enumName ? Visibility.Visible : Visibility.Collapsed;
                }
            });

            switch (Mode)
            {
                case InverterDisplayMode.DcRelativePower:
                case InverterDisplayMode.DcPower:
                    DcPower.Visibility = Visibility.Visible;
                    DcPowerHeadline.Text = Loc.DcPower + (Mode == InverterDisplayMode.DcRelativePower ? " %" : string.Empty);
                    DcPowerFirstGauge.ShowPercent = Mode == InverterDisplayMode.DcRelativePower;

                    switch (Mode)
                    {
                        case InverterDisplayMode.DcPower:
                            DcPowerAggregateGauge.Label = Loc.Sum;
                            DcPowerAggregateGauge.Value = gen24Sensors?.Cache?.SolarPowerSum ?? 0;
                            DcPowerAggregateGauge.Maximum = gen24Config.InverterSettings?.Mppt?.WattPeakTotal ?? 10000;
                            DcPowerAggregateGauge.Minimum = 0;
                            DcPowerAggregateGauge.GaugeColors = Gauge.HighIsBad;
                            DcPowerAggregateGauge.StringFormat = "N0";
                            DcPowerAggregateGauge.UnitName = "W";
                            break;

                        case InverterDisplayMode.DcRelativePower:
                            DcPowerAggregateGauge.Value = gen24Sensors?.Cache?.Solar2Power / gen24Sensors?.Cache?.Solar1Power * 100 ?? 0;
                            DcPowerAggregateGauge.Minimum = 50;
                            DcPowerAggregateGauge.Maximum = 150;
                            DcPowerAggregateGauge.StringFormat = "N1";
                            DcPowerAggregateGauge.UnitName = "%";
                            DcPowerAggregateGauge.Label = "2/1";
                            DcPowerAggregateGauge.GaugeColors = Gauge.LowIsBad;
                            break;
                    }

                    break;

                case InverterDisplayMode.MoreEfficiency:
                    MoreEfficiencyLoss.Value = gen24Sensors?.PowerFlow?.PowerLoss ?? 0;
                    MoreEfficiencyEfficiency.Value = gen24Sensors?.PowerFlow?.Efficiency ?? 0;
                    MoreEfficiencySelfConsumption.Value = Math.Max(Math.Min(-sitePowerFlow?.LoadPowerCorrected / sitePowerFlow?.InverterAcPower ?? 0, 1), 0);
                    MoreEfficiencySelfSufficiency.Value = sitePowerFlow?.LoadPowerCorrected > 0 ? 1 : Math.Max(Math.Min(-sitePowerFlow?.InverterAcPower / sitePowerFlow?.LoadPowerCorrected ?? 0, 1d), 0);
                    break;
            }
        });
    }

    private void CycleMode(IReadOnlyList<InverterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnAcClicked(object sender, RoutedEventArgs e) => CycleMode(acModes, ref currentAcIndex);

    private void OnDcClicked(object sender, RoutedEventArgs e) => CycleMode(dcModes, ref currentDcIndex);

    private void OnEnergyClicked(object sender, RoutedEventArgs e) => CycleMode(energyModes, ref energyIndex);

    private void OnMoreClicked(object sender, RoutedEventArgs e) => CycleMode(moreModes, ref currentMoreIndex);

    private async void OnStandByClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { IsChecked: not null } toggleButton || gen24Service is null)
        {
            return;
        }

        try
        {
            isInStandByChange = true;

            if (!toggleButton.IsChecked.Value && MessageBox.Show(Loc.StandbyWarning, Loc.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                await gen24Service.RequestInverterStandBy(!toggleButton.IsChecked.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Loc.CouldNotChangeStandBy, ex.Message), Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            lastStandbySwitchUpdate = DateTime.UnixEpoch;
            isInStandByChange = false;
        }
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        IoC.Get<MainWindow>().GetView<InverterSettingsView>(provider).Focus();
    }

    private void OnEnergyFlowClicked(object sender, RoutedEventArgs e)
    {
        IoC.Get<MainWindow>().GetView<SelfConsumptionOptimizationView>(provider).Focus();
    }

    private void OnModbusClicked(object sender, RoutedEventArgs e)
    {
        IoC.Get<MainWindow>().GetView<ModbusView>(provider).Focus();
    }

    private void OnEventLogClicked(object sender, RoutedEventArgs e)
    {
        IoC.Get<MainWindow>().GetView<EventLogView>(provider).Focus();
    }

    private void OnDetailsClicked(object sender, RoutedEventArgs e)
    {
        IoC.Get<MainWindow>().GetView<InverterDetailsView>(provider).Focus();
    }
}
