using Avalonia.Interactivity;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

public enum MeterDisplayMode
{
    PowerActiveGauge,
    PowerActive,
    PowerApparent,
    PowerReactive,
    PowerFactor,
    PowerOutOfBalance,
    PhaseVoltage,
    PhaseVoltageGauge,
    LineVoltage,
    Current,
    CurrentOutOfBalance,
    CurrentOutOfBalanceGauge,
    More,
    MoreEnergy,
}


public partial class SmartMeterControl : DeviceControlBase
{
    private static readonly IReadOnlyList<MeterDisplayMode> powerModes =
    [
        MeterDisplayMode.PowerActiveGauge,
        MeterDisplayMode.PowerActive, MeterDisplayMode.PowerApparent,
        MeterDisplayMode.PowerReactive, MeterDisplayMode.PowerFactor,
        MeterDisplayMode.PowerOutOfBalance,
    ];

    private static readonly IReadOnlyList<MeterDisplayMode> voltageModes =
    [
        MeterDisplayMode.PhaseVoltageGauge, MeterDisplayMode.PhaseVoltage, MeterDisplayMode.LineVoltage
    ];

    private static readonly IReadOnlyList<MeterDisplayMode> currentModes =
    [
        MeterDisplayMode.Current, MeterDisplayMode.CurrentOutOfBalanceGauge, MeterDisplayMode.CurrentOutOfBalance
    ];

    private static readonly IReadOnlyList<MeterDisplayMode> moreModes =
    [
        MeterDisplayMode.MoreEnergy, MeterDisplayMode.More
    ];

    private int currentPowerModeIndex, currentVoltageModeIndex, currentCurrentIndex, currentMoreIndex;

    #region Avalonia Properties

    public static readonly StyledProperty<Gen24PowerMeter3P?> SmartMeterProperty = AvaloniaProperty.Register<SmartMeterControl, Gen24PowerMeter3P?>(nameof(SmartMeter));

    public Gen24PowerMeter3P? SmartMeter
    {
        get => GetValue(SmartMeterProperty);
        set => SetValue(SmartMeterProperty, value);
    }

    public static readonly StyledProperty<Gen24Config?> Gen24ConfigProperty = AvaloniaProperty.Register<SmartMeterControl, Gen24Config?>(nameof(Gen24Config));

    public Gen24Config? Gen24Config
    {
        get => GetValue(Gen24ConfigProperty);
        set => SetValue(Gen24ConfigProperty, value);
    }

    public static readonly StyledProperty<MeterDisplayMode> ModeProperty = AvaloniaProperty.Register<SmartMeterControl, MeterDisplayMode>(nameof(Mode));

    public MeterDisplayMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly StyledProperty<Gen24Status?> MeterStatusProperty = AvaloniaProperty.Register<SmartMeterControl, Gen24Status?>(nameof(MeterStatus));

    public Gen24Status? MeterStatus
    {
        get => GetValue(MeterStatusProperty);
        set => SetValue(MeterStatusProperty, value);
    }

    public static readonly StyledProperty<bool> ColorAllTicksProperty = AvaloniaProperty.Register<SmartMeterControl, bool>(nameof(ColorAllTicks));

    public bool ColorAllTicks
    {
        get => GetValue(ColorAllTicksProperty);
        set => SetValue(ColorAllTicksProperty, value);
    }

    #endregion

    public SmartMeterControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(MeterStatus):
                ChangeInner();
                ChangeOuter();
                break;
        }
    }

    protected override void ChangeOuter()
    {
        BackgroundProvider.Background = MeterStatus?.StatusCode switch
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
        InnerBackgroundProvider.Background = MeterStatus?.StatusCode switch
        {
            "STATE_ERROR" => InnerFault,
            "STATE_RUNNING" => InnerRunning,
            "STATE_WARNING" => InnerWarning,
            "STATE_STARTUP" => InnerStartup,
            _ => InnerOther,
        };
    }

    private void SetMode(IReadOnlyList<MeterDisplayMode> modeList, ref int index)
    {
        index = modeList.Contains(Mode) ? ++index % modeList.Count : 0;
        Mode = modeList[index];
    }

    private void OnPowerClick(object sender, RoutedEventArgs e) => SetMode(powerModes, ref currentPowerModeIndex);

    private void OnVoltageClick(object sender, RoutedEventArgs e) => SetMode(voltageModes, ref currentVoltageModeIndex);

    private void OnCurrentClick(object sender, RoutedEventArgs e) => SetMode(currentModes, ref currentCurrentIndex);

    private void OnMoreClick(object sender, RoutedEventArgs e) => SetMode(moreModes, ref currentMoreIndex);
}