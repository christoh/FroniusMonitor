namespace De.Hochstaetter.HomeAutomationClient.Controls;

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


public partial class InverterControl : DeviceControlBase
{
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

    public static readonly StyledProperty<Gen24System> InverterProperty = AvaloniaProperty.Register<InverterControl, Gen24System>(nameof(Inverter));

    public Gen24System Inverter
    {
        get => GetValue(InverterProperty);
        set => SetValue(InverterProperty, value);
    }

    public static readonly StyledProperty<ICommand?> StandbyCommandProperty = AvaloniaProperty.Register<InverterControl, ICommand?>(nameof(StandbyCommand));

    public ICommand? StandbyCommand
    {
        get => GetValue(StandbyCommandProperty);
        set => SetValue(StandbyCommandProperty, value);
    }

    //public static readonly StyledProperty<bool> IsStandbyProperty = AvaloniaProperty.Register<InverterControl, bool>(nameof(IsStandby));

    //public bool IsStandby
    //{
    //    get => GetValue(IsStandbyProperty);
    //    set => SetValue(IsStandbyProperty, value);
    //}

    public static readonly StyledProperty<bool> ColorAllTicksProperty = AvaloniaProperty.Register<InverterControl, bool>(nameof(ColorAllTicks));

    public bool ColorAllTicks
    {
        get => GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }

    public static readonly StyledProperty<InverterDisplayMode> ModeProperty = AvaloniaProperty.Register<InverterControl, InverterDisplayMode>(nameof(Mode), InverterDisplayMode.DcPowerGauge);

    public InverterDisplayMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public InverterControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.Property.Name)
        {
            case nameof(Inverter):
                if (e.OldValue is INotifyPropertyChanged oldDevice)
                {
                    oldDevice.PropertyChanged -= OnInverterPropertyChanged;
                }

                if (e.NewValue is INotifyPropertyChanged newDevice)
                {
                    newDevice.PropertyChanged += OnInverterPropertyChanged;
                    OnInverterPropertyChanged(Inverter, new PropertyChangedEventArgs(string.Empty));
                }
                break;
        }
    }
    
    private void OnInverterPropertyChanged(object? sender, PropertyChangedEventArgs e)
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

    protected override void ChangeOuter()
    {
        BackgroundProvider.Background = Inverter?.Sensors?.InverterStatus?.StatusCode switch
        {
            "STATE_ERROR" => OuterFault,
            "STATE_RUNNING" => OuterRunning,
            "STATE_WARNING" => OuterWarning,
            "STATE_STARTUP" => OuterStartup,
            _ => OuterOther,
        };
    }

    protected override void ChangeInner()
    {
        InnerBackgroundProvider.Background = Inverter?.Sensors?.InverterStatus?.StatusCode switch
        {
            "STATE_ERROR" => InnerFault,
            "STATE_RUNNING" => InnerRunning,
            "STATE_WARNING" => InnerWarning,
            "STATE_STARTUP" => InnerStartup,
            _ => InnerOther,
        };
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

    private void OnStandbyClicked(object? sender, RoutedEventArgs e)
    {
        if (StandbyCommand?.CanExecute(DeviceKey) is true)
        {
            StandbyCommand?.Execute(DeviceKey);
        }
    }
}